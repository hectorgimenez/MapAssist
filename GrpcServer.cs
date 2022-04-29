using Grpc.Core;
using MapAssist.Helpers;
using MapAssist.Types;
using koolo.mapassist.api;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameOverlay.Drawing;
using System;

namespace MapAssist
{
    class GrpcServer: koolo.mapassist.api.MapAssistApi.MapAssistApiBase
    {
        private GameDataReader _gameDataReader;
        private GameData _gameData;
        private AreaData _areaData;
        private List<Types.PointOfInterest> _pointsOfInterest;

        public GrpcServer()
        {
            _gameDataReader = new GameDataReader();
            GameOverlay.TimerService.EnableHighPrecisionTimers();
        }

        public override Task<Data> GetData(R request, ServerCallContext context)
        {
            try
            {
                (_gameData, _areaData, _pointsOfInterest, _) = _gameDataReader.Get();
            } catch (Exception e) {
                return Task.FromResult(new Data());
            }
            
            if (_areaData == null || _gameData == null)
            {
                return Task.FromResult(new Data());
            }

            var data = new Data();

            data.Status = Status();
            data.PlayerUnit = PlayerUnit();
            data.AreaOrigin = Position(_areaData.Origin.X, _areaData.Origin.Y);
            data.Area = _areaData.Area.ToString();
            data.MenuOpen = new MenuOpen();
            SetCorpses(data);
            SetMonsters(data);
            SetObjects(data);
            SetItems(data);
            SetPointsOfInterest(data);
            SetAdjacentLevels(data);
            SetNPCs(data);

            data.MenuOpen = new MenuOpen();
            data.MenuOpen.Inventory = _gameData.MenuOpen.Inventory;
            data.MenuOpen.NpcInteract = _gameData.MenuOpen.NpcInteract;
            data.MenuOpen.NpcShop = _gameData.MenuOpen.NpcShop;
            data.MenuOpen.Stash = _gameData.MenuOpen.Stash;
            data.MenuOpen.Waypoint = _gameData.MenuOpen.Waypoint;

            SetCollisionGrid(data);

            return Task.FromResult(data);
        }

        private koolo.mapassist.api.Status Status()
        {
            var status = new koolo.mapassist.api.Status();

            // Mercenary Health management
            if (_gameData.Mercs.Length > 0)
            {
                if (_gameData.Mercs[0].HealthPercentage != 0)
                {
                    status.MercAlive = true;
                    // This fixes a bug where sometimes life is not retrieved correctly
                    if (_gameData.Mercs[0].Stats[Stats.Stat.Life] > 32768)
                    {
                        status.MercLife = (uint)(_gameData.Mercs[0].Stats[Stats.Stat.Life] >> 8);
                    }
                    else
                    {
                        status.MercLife = (uint)(((double)_gameData.Mercs[0].Stats[Stats.Stat.Life] / 32768) * (_gameData.Mercs[0].Stats[Stats.Stat.MaxLife] >> 8));
                    }
                    status.MercMaxLife = (uint)(status.MercAlive ? _gameData.Mercs[0].Stats[Stats.Stat.MaxLife] >> 8 : 0);
                }
            }

            // Player unit
            status.Life = (uint)(_gameData.PlayerUnit.Stats.ContainsKey(Stats.Stat.Life) ? _gameData.PlayerUnit.Stats[Stats.Stat.Life] >> 8 : 0);
            status.MaxLife = (uint)(_gameData.PlayerUnit.Stats[Stats.Stat.MaxLife] >> 8);
            status.Mana = (uint)(_gameData.PlayerUnit.Stats[Stats.Stat.Mana] >> 8);
            status.MaxMana = (uint)(_gameData.PlayerUnit.Stats[Stats.Stat.MaxMana] >> 8);

            return status;
        }
        private PlayerUnit PlayerUnit()
        {
            var pu = new PlayerUnit();
            pu.Position = Position(_gameData.PlayerUnit.Position.X, _gameData.PlayerUnit.Position.Y);
            pu.Name = _gameData.PlayerUnit.Name;
            pu.Class = _gameData.PlayerUnit.RosterEntry.PlayerClass.ToString();

            foreach (KeyValuePair<Stats.Stat, int> s in _gameData.PlayerUnit.Stats)
            {
                var stat = new Stat();
                stat.Name = s.Key.ToString();
                stat.Value = (uint)s.Value;
                
                pu.Stats.Add(stat);
            }

            foreach (SkillPoints s in _gameData.PlayerUnit.Skills.AllSkills)
            {
                var skill = new koolo.mapassist.api.Skill();
                skill.Name = s.Skill.ToString();
                skill.Points = s.HardPoints;

                pu.Skills.Add(skill);
            }

            return pu;
        }
        private void SetCorpses(Data data)
        {
            foreach (UnitPlayer m in _gameData.Corpses)
            {
                var corpse = new Corpse();
                corpse.Name = m.Name;
                corpse.Position = Position(m.Position.X, m.Position.Y);

                data.Corpses.Add(corpse);
            }
        }
        private void SetMonsters(Data data)
        {
            foreach (UnitAny m in _gameData.Monsters)
            {
                if (m is UnitMonster monster)
                {
                    var protoMonster = new Monster();
                    protoMonster.Id = (int)monster.UnitId;
                    protoMonster.Name = ((Npc)monster.TxtFileNo).Name();
                    protoMonster.Type = monster.MonsterType.ToString();

                    if (monster.MonsterType == Structs.MonsterTypeFlags.SuperUnique)
                    {
                        foreach (KeyValuePair<string, string> unique in Types.NPC.SuperUniques)
                        {
                            if (unique.Value == monster.MonsterStats.Name)
                            {
                                protoMonster.Name = unique.Key;
                            }
                        }
                    }
                    protoMonster.Position = Position(monster.Position.X, monster.Position.Y);
                    protoMonster.Hovered = monster.IsHovered;

                    foreach (Resist r in monster.Immunities)
                    {
                        protoMonster.Immunities.Add(r.ToString());
                    }

                    data.Monsters.Add(protoMonster);
                }
            }
        }
        private void SetObjects(Data data)
        {

            foreach (UnitAny o in _gameData.Objects)
            {
                if (o is UnitObject ob)
                {
                    var protoObject = new koolo.mapassist.api.Object();
                    protoObject.Name = ((GameObject)ob.TxtFileNo).ToString();
                    protoObject.Position = Position(ob.Position.X, ob.Position.Y);
                    protoObject.Hovered = ob.IsHovered;
                    protoObject.Selectable = ob.ObjectData.InteractType != 0x00;
                    protoObject.Chest = ob.IsChest;
                    data.Objects.Add(protoObject);
                }
            }

            foreach (KeyValuePair<GameObject, Point[]> o in _areaData.Objects)
            {
                var found = false;
                foreach (koolo.mapassist.api.Object ob in data.Objects)
                {
                    if (ob.Name == o.Key.ToString())
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var protoObject = new koolo.mapassist.api.Object();
                    protoObject.Position = Position(o.Value[0].X, o.Value[0].Y);
                    protoObject.Name = o.Key.ToString();

                    data.Objects.Add(protoObject);
                }
            }
        }
        private void SetItems(Data data)
        {
            foreach (UnitAny i in _gameData.AllItems)
            {
                if (i is UnitItem item)
                {
                    var protoItem = new koolo.mapassist.api.Item();
                    protoItem.Id = (int)item.UnitId;
                    protoItem.Position = Position(item.Position.X, item.Position.Y);
                    protoItem.Name = item.Item.ToString();
                    protoItem.Hovered = i.IsHovered;
                    protoItem.Place = item.ItemModeMapped.ToString();
                    protoItem.Quality = item.ItemData.ItemQuality.ToString();
                    protoItem.Ethereal = (item.ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL;
                    protoItem.Identified = item.IsIdentified;
                    protoItem.VendorName = item.VendorOwner.ToString();

                    if (item.Stats != null)
                    {
                        foreach (KeyValuePair<Stats.Stat, int> stat in item.Stats)
                        {
                            var pStat = new Stat();
                            pStat.Name = stat.Key.ToString();
                            pStat.Value = (uint)stat.Value;

                            protoItem.Stats.Add(pStat);
                        }
                    }

                    data.Items.Add(protoItem);
                }
            }
        }
        private void SetPointsOfInterest(Data data)
        {
            foreach (Types.PointOfInterest p in _pointsOfInterest)
            {
                var poi = new koolo.mapassist.api.PointOfInterest();
                poi.Name = p.Label;
                poi.Position = Position(p.Position.X, p.Position.Y);

                data.PointsOfInterest.Add(poi);
            }
        }
        private void SetAdjacentLevels(Data data)
        {
            foreach (KeyValuePair<Area, Types.AdjacentLevel> a in _areaData.AdjacentLevels)
            {
                var pal = new koolo.mapassist.api.AdjacentLevel();
                pal.Area = a.Key.ToString();
                foreach (Point p in a.Value.Exits)
                {
                    pal.Positions.Add(Position(p.X, p.Y));
                }
                
                data.AdjacentLevels.Add(pal);
            }
        }
        private void SetNPCs(Data data)
        {
            foreach (KeyValuePair<Npc, Point[]> npc in _areaData.NPCs)
            {
                var pNpc = new koolo.mapassist.api.NPC();
                pNpc.Name = NpcExtensions.Name(npc.Key);

                foreach (Point p in npc.Value)
                {
                    pNpc.Positions.Add(Position(p.X, p.Y));
                }

                data.Npcs.Add(pNpc);
            }
        }

        private void SetCollisionGrid(Data data)
        {
            foreach (var c in _areaData.CollisionGrid) {
                var cg = new CollisionGrid();
                foreach (var r in c)
                {
                    cg.Walkable.Add(r == 0);
                }
                data.CollisionGrid.Add(cg);
            }
        }

        private koolo.mapassist.api.Position Position(float x, float y)
        {
            var position = new koolo.mapassist.api.Position();
            position.X = x;
            position.Y = y;

            return position;
        }
    }
}
