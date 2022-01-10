﻿/**
 *   Copyright (C) 2021 okaygo
 *   
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

using System.Linq;
using MapAssist.Types;
using MapAssist.Settings;

namespace MapAssist.Helpers
{
    public static class LootFilter
    {
        public static (bool, ItemFilter) Filter(UnitAny unitAny)
        {
            //skip low quality items
            var lowQuality = (unitAny.ItemData.ItemFlags & ItemFlags.IFLAG_LOWQUALITY) == ItemFlags.IFLAG_LOWQUALITY;
            if (lowQuality)
            {
                return (false, null);
            }

            var baseName = Items.ItemName(unitAny.TxtFileNo);
            //populate a list of filter rules by combining rules from "Any" and the item base name
            //use only one list or the other depending on if "Any" exists
            var matches =
                LootLogConfiguration.Filters
                    .Where(f => f.Key == "Any" || f.Key == baseName).ToList();

            // Early breakout
            // We know that there is an item in here without any actual filters
            // So we know that simply having the name match means we can return true
            if (matches.Any(kv => kv.Value == null))
            {
                return (true, null);
            }

            //get other item stats to use for filtering
            var itemQuality = unitAny.ItemData.ItemQuality;
            var isEth = (unitAny.ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL;
            var numSockets = unitAny.Stats.TryGetValue(Stat.STAT_ITEM_NUMSOCKETS, out var socketCount) ? socketCount : 0;

            //scan the list of rules
            foreach (var rule in matches.SelectMany(kv => kv.Value))
            {
                var qualityReqMet = rule.Qualities == null || rule.Qualities.Length == 0 || rule.Qualities.Contains(itemQuality);
                if (!qualityReqMet) { continue; }

                var socketReqMet = rule.Sockets == null || rule.Sockets.Length == 0 || rule.Sockets.Contains(numSockets);
                if (!socketReqMet) { continue; }

                var defenseReqMet = rule.Defense == null || rule.Defense == 0 || Items.GetArmorDefense(unitAny) >= rule.Defense;
                if (!defenseReqMet) { continue; }

                var allResReqMet = rule.AllResist == null || rule.AllResist == 0 || Items.GetItemStatAllResist(unitAny) >= rule.AllResist;
                if (!allResReqMet) { continue; }

                var ethReqMet = (rule.Ethereal == null || rule.Ethereal == isEth);
                if (!ethReqMet) { continue; }

                var allSkillsReqMet = (rule.AllSkills == null || rule.AllSkills == 0 || Items.GetItemStatAllSkills(unitAny) >= rule.AllSkills);
                if (!allSkillsReqMet) { continue; }

                // Item class skills
                var addClassSkillsReqMet = (rule.ClassSkills.Count == 0);
                foreach (var subrule in rule.ClassSkills)
                {
                    addClassSkillsReqMet = (subrule.Value == null || subrule.Value == 0 || Items.GetItemStatAddClassSkills(unitAny, subrule.Key) >= subrule.Value);
                    if (!addClassSkillsReqMet) { continue; }
                }
                if (!addClassSkillsReqMet) { continue; }

                // Item class tab skills
                var addClassTabSkillsReqMet = (rule.ClassTabSkills.Count == 0);
                foreach (var subrule in rule.ClassTabSkills)
                {
                    addClassTabSkillsReqMet = (subrule.Value == null || subrule.Value == 0 || Items.GetItemStatAddClassTabSkills(unitAny, subrule.Key) >= subrule.Value);
                    if (!addClassTabSkillsReqMet) { continue; }
                }
                if (!addClassTabSkillsReqMet) { continue; }

                // Item single skills
                var singleSkillsReqMet = (rule.Skills.Count == 0);
                foreach (var subrule in rule.Skills)
                {
                    singleSkillsReqMet = (subrule.Value == null || subrule.Value == 0 || Items.GetItemStatSingleSkills(unitAny, subrule.Key) >= subrule.Value);
                    if (!singleSkillsReqMet) { continue; }
                }
                if (!singleSkillsReqMet) { continue; }

                // Item meets all filter requirements
                return (true, rule);
            }

            return (false, null);
        }
    }
}
