﻿using MapAssist.Helpers;
using MapAssist.Settings;
using MapAssist.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace MapAssist
{
    public partial class ConfigEditor : Form
    {
        private PropertyInfo SelectedProperty;
        private AddAreaForm areaForm;

        public ConfigEditor()
        {
            InitializeComponent();

            var propertyList = MapAssistConfiguration.Loaded.MapConfiguration.GetType().GetProperties();
            for (var i = 0; i < propertyList.Length; i++)
            {
                var element = propertyList[i];
                if (element.Name.Length > 3)
                {
                    var lastThree = element.Name.Substring(element.Name.Length - 3, 3);
                    if (!(lastThree == "REF"))
                    {
                        cboRenderOption.Items.Add(element.Name.ToProperCase());
                        if (i + 1 > propertyList.Length)
                        {
                            Console.WriteLine("CONFIG ERROR - (MapConfiguration." + element.Name + ") missing a static REF member after it.");
                        }
                        else
                        {
                            var nextElement = propertyList[i + 1];
                            lastThree = nextElement.Name.Substring(nextElement.Name.Length - 3, 3);
                            if (!(lastThree == "REF"))
                            {
                                Console.WriteLine("CONFIG ERROR - (MapConfiguration." + element.Name + ") missing a static REF member after it.");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("CONFIG ERROR - (MapConfiguration." + element.Name + ") variable names in the MapConfiguration class should be at least 4 characters long");
                }
            }

            foreach (var element in Enum.GetNames(typeof(BuffPosition)))
            {
                cboBuffPosition.Items.Add(element.ToProperCase());
            }

            foreach (var element in Enum.GetNames(typeof(MapPosition)))
            {
                cboPosition.Items.Add(element.ToProperCase());
            }

            foreach (var element in Enum.GetNames(typeof(Shape)))
            {
                cboIconShape.Items.Add(element);
            }

            foreach (Locale element in Enum.GetValues(typeof(Locale)))
            {
                cboLanguage.Items.Add(LocaleExtensions.Name(element));
            }

            cboLanguage.SelectedIndex = (int)MapAssistConfiguration.Loaded.LanguageCode;

            opacity.Value = (int)Math.Round(MapAssistConfiguration.Loaded.RenderingConfiguration.Opacity * 100f / 5);
            lblOpacityValue.Text = (opacity.Value * 5).ToString();

            iconOpacity.Value = (int)Math.Round(MapAssistConfiguration.Loaded.RenderingConfiguration.IconOpacity * 100f / 5);
            lblIconOpacityValue.Text = (iconOpacity.Value * 5).ToString();

            mapSize.Value = (int)Math.Round(MapAssistConfiguration.Loaded.RenderingConfiguration.Size / 100f);
            lblMapSizeValue.Text = (mapSize.Value * 100).ToString();

            mapZoom.Value = zoomToTick(MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel);
            lblMapZoomValue.Text = MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel.ToString();

            chkOverlayMode.Checked = MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode;
            chkToggleViaMap.Checked = MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGameMap;
            chkToggleViaPanels.Checked = MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGamePanels;
            chkStickToLastGameWindow.Checked = MapAssistConfiguration.Loaded.RenderingConfiguration.StickToLastGameWindow;
            cboPosition.SelectedIndex = cboPosition.FindStringExact(MapAssistConfiguration.Loaded.RenderingConfiguration.Position.ToString().ToProperCase());

            buffSize.Value = (int)Math.Round(MapAssistConfiguration.Loaded.RenderingConfiguration.BuffSize * 10f);
            lblBuffSizeValue.Text = MapAssistConfiguration.Loaded.RenderingConfiguration.BuffSize.ToString();
            cboBuffPosition.SelectedIndex = cboBuffPosition.FindStringExact(MapAssistConfiguration.Loaded.RenderingConfiguration.BuffPosition.ToString().ToProperCase());

            chkGameInfo.Checked = MapAssistConfiguration.Loaded.GameInfo.ShowGameIP;
            chkShowArea.Checked = MapAssistConfiguration.Loaded.GameInfo.ShowAreaLevel;
            txtHuntIP.Text = MapAssistConfiguration.Loaded.GameInfo.HuntingIP;
            txtD2Path.Text = MapAssistConfiguration.Loaded.D2Path;

            chkShowOverlayFPS.Checked = MapAssistConfiguration.Loaded.GameInfo.ShowOverlayFPS;

            new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ToggleKey.ToString()).Monitor(txtToggleMapKey);
            new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.AreaLevelKey.ToString()).Monitor(txtAreaLevelKey);
            new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomInKey.ToString()).Monitor(txtZoomInKey);
            new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomOutKey.ToString()).Monitor(txtZoomOutKey);

            chkItemLogEnabled.Checked = MapAssistConfiguration.Loaded.ItemLog.Enabled;
            chkPlaySound.Checked = MapAssistConfiguration.Loaded.ItemLog.PlaySoundOnDrop;
            txtFilterFile.Text = MapAssistConfiguration.Loaded.ItemLog.FilterFileName;
            txtSoundFile.Text = MapAssistConfiguration.Loaded.ItemLog.SoundFile;
            itemDisplayForSeconds.Value = (int)Math.Round(MapAssistConfiguration.Loaded.ItemLog.DisplayForSeconds / 5f);
            lblItemDisplayForSecondsValue.Text = $"{itemDisplayForSeconds.Value * 5} s";

            if (MapAssistConfiguration.Loaded.MapColorConfiguration.Walkable != null)
            {
                var walkableColor = (Color)MapAssistConfiguration.Loaded.MapColorConfiguration.Walkable;
                btnWalkableColor.BackColor = walkableColor;
                chkWalkableColor.Checked = (walkableColor.A > 0);
            }
            if (MapAssistConfiguration.Loaded.MapColorConfiguration.Border != null)
            {
                var borderColor = (Color)MapAssistConfiguration.Loaded.MapColorConfiguration.Border;
                btnBorderColor.BackColor = borderColor;
            }

            foreach (var area in MapAssistConfiguration.Loaded.HiddenAreas)
            {
                lstHidden.Items.Add(AreaExtensions.NameInternal(area));
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            MapAssistConfiguration.Loaded.Save();
            base.OnFormClosing(e);
        }

        private void IgnoreMouseWheel(object sender, EventArgs e)
        {
            ((HandledMouseEventArgs)e).Handled = true;
        }

        private void updateTime_Scroll(object sender, EventArgs e)
        {
        }

        private void opacity_Scroll(object sender, EventArgs e)
        {
            if (opacity.Value > 0)
            {
                MapAssistConfiguration.Loaded.RenderingConfiguration.Opacity = (float)Math.Round(opacity.Value * 5 / 100f, 2);
            }
            else
            {
                MapAssistConfiguration.Loaded.RenderingConfiguration.Opacity = 0;
            }
            lblOpacityValue.Text = (opacity.Value * 5).ToString();
        }

        private void iconOpacity_Scroll(object sender, EventArgs e)
        {
            if (iconOpacity.Value > 0)
            {
                MapAssistConfiguration.Loaded.RenderingConfiguration.IconOpacity = (float)Math.Round(iconOpacity.Value * 5 / 100f, 2);
            }
            else
            {
                MapAssistConfiguration.Loaded.RenderingConfiguration.IconOpacity = 0;
            }
            lblIconOpacityValue.Text = (iconOpacity.Value * 5).ToString();
        }

        private void mapSize_Scroll(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.RenderingConfiguration.Size = mapSize.Value * 100;
            lblMapSizeValue.Text = (mapSize.Value * 100).ToString();
        }

        private void mapZoom_Scroll(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel = tickToZoom(mapZoom.Value);
            lblMapZoomValue.Text = MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel.ToString();
        }

        private int zoomToTick(float zoom)
        {
            if (zoom == 1) return 10;
            else if (zoom < 1) return (int)Math.Max(Math.Round((1 - zoom) * 10), 1); // Minimum zoom = 0.1 translates to tick = 1
            else return (int)Math.Min(Math.Round((zoom - 1) * 5) + 10, 25); // Maximum zoom = 4 translates to tick = 25
        }

        private float tickToZoom(int tick)
        {
            if (tick == 10) return 1;
            else if (tick < 10) return Math.Max(tick / 10f, 0.1f); // Minimum zoom = 0.1 translates to tick = 1
            else return Math.Min((tick - 10) / 5f + 1, 4); // Maximum zoom = 4 translates to tick = 25
        }

        private void buffSize_Scroll(object sender, EventArgs e)
        {
            if (buffSize.Value > 0)
            {
                MapAssistConfiguration.Loaded.RenderingConfiguration.BuffSize = (float)Math.Round(buffSize.Value / 10f, 2);
            }
            else
            {
                MapAssistConfiguration.Loaded.RenderingConfiguration.BuffSize = 0;
            }
            lblBuffSizeValue.Text = MapAssistConfiguration.Loaded.RenderingConfiguration.BuffSize.ToString();
        }

        private void chkOverlayMode_CheckedChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.RenderingConfiguration.OverlayMode = chkOverlayMode.Checked;
        }

        private void cboPosition_SelectedIndexChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.RenderingConfiguration.Position = (MapPosition)cboPosition.SelectedIndex;
        }

        private void chkToggleViaMap_CheckedChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGameMap = chkToggleViaMap.Checked;
        }

        private void chkToggleViaPanels_CheckedChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.RenderingConfiguration.ToggleViaInGamePanels = chkToggleViaPanels.Checked;
        }

        private void chkStickToLastGameWindow_CheckedChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.RenderingConfiguration.StickToLastGameWindow = chkStickToLastGameWindow.Checked;
        }

        private void cboBuffPosition_SelectedIndexChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.RenderingConfiguration.BuffPosition = (BuffPosition)cboBuffPosition.SelectedIndex;
        }

        private void chkGameInfo_CheckedChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.GameInfo.ShowGameIP = chkGameInfo.Checked;
        }

        private void chkShowArea_CheckedChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.GameInfo.ShowAreaLevel = chkShowArea.Checked;
        }

        private void txtHuntIP_TextChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.GameInfo.HuntingIP = txtHuntIP.Text;
        }

        private void txtD2Path_TextChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.D2Path = txtD2Path.Text;
        }

        private void chkShowOverlayFPS_CheckedChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.GameInfo.ShowOverlayFPS = chkShowOverlayFPS.Checked;
        }

        private void cboRenderOption_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedProperty = MapAssistConfiguration.Loaded.MapConfiguration.GetType().GetProperty(cboRenderOption.Text.ToPascalCase() + "REF");
            if (SelectedProperty != null)
            {
                tabDrawing.Visible = true;
            }
            else
            {
                return;
            }
            dynamic iconProp = SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null);
            btnIconColor.BackColor = iconProp.IconColor;
            btnIconOutlineColor.BackColor = iconProp.IconOutlineColor;
            chkIcon.Checked = (iconProp.IconColor.A > 0 || iconProp.IconOutlineColor.A > 0);
            cboIconShape.SelectedIndex = cboIconShape.FindStringExact(Enum.GetName(typeof(Shape), iconProp.IconShape));
            iconSize.Value = (int)iconProp.IconSize;
            iconThickness.Value = (int)iconProp.IconThickness;
            lblIconSizeValue.Text = iconSize.Value.ToString();
            lblIconThicknessValue.Text = iconThickness.Value.ToString();
            if (SelectedProperty.PropertyType != typeof(PointOfInterestRendering) && SelectedProperty.PropertyType != typeof(PortalRendering))
            {
                tabDrawing.TabPages.Remove(tabLabel);
                tabDrawing.TabPages.Remove(tabLine);
            }
            else
            {
                tabDrawing.TabPages.Remove(tabLabel);
                tabDrawing.TabPages.Remove(tabLine);
                tabDrawing.TabPages.Insert(1, tabLabel);
                tabDrawing.TabPages.Insert(2, tabLine);
                btnLabelColor.BackColor = iconProp.LabelColor;
                chkLabel.Checked = (iconProp.LabelColor.A > 0);

                btnLineColor.BackColor = iconProp.LineColor;
                chkLine.Checked = (iconProp.LineColor.A > 0);
                lineArrowSize.Value = iconProp.ArrowHeadSize;
                lblLineArrowSizeValue.Text = lineArrowSize.Value.ToString();
                lblLineThicknessSizeValue.Text = lineThicknessSize.Value.ToString();
            }
        }

        private void btnIconColor_Click(object sender, EventArgs e)
        {
            var colorDlg = new ColorDialog();
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                var iconProp = SelectedProperty.PropertyType.GetProperty("IconColor");
                iconProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), colorDlg.Color, null);
                btnIconColor.BackColor = colorDlg.Color;
                chkIcon.Checked = (btnIconOutlineColor.BackColor.A > 0 || colorDlg.Color.A > 0);
            }
        }

        private void btnIconOutlineColor_Click(object sender, EventArgs e)
        {
            var colorDlg = new ColorDialog();
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                var iconProp = SelectedProperty.PropertyType.GetProperty("IconOutlineColor");
                iconProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), colorDlg.Color, null);
                btnIconOutlineColor.BackColor = colorDlg.Color;
                chkIcon.Checked = (btnIconColor.BackColor.A > 0 || colorDlg.Color.A > 0);
            }
        }

        private void chkIcon_Click(object sender, EventArgs e)
        {
            var iconProp = SelectedProperty.PropertyType.GetProperty("IconColor");
            var iconOutlineProp = SelectedProperty.PropertyType.GetProperty("IconOutlineColor");

            if (chkIcon.Checked)
            {
                iconProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), btnIconColor.BackColor, null);
                iconOutlineProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), btnIconOutlineColor.BackColor, null);
            }
            else
            {
                iconProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), Color.Empty, null);
                iconOutlineProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), Color.Empty, null);

                btnIconColor.BackColor = Color.Empty;
                btnIconOutlineColor.BackColor = Color.Empty;
            }
        }

        private void cboIconShape_SelectedIndexChanged(object sender, EventArgs e)
        {
            var iconProp = SelectedProperty.PropertyType.GetProperty("IconShape");
            iconProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), (MapPosition)cboIconShape.SelectedIndex, null);
        }

        private void iconSize_Scroll(object sender, EventArgs e)
        {
            var iconProp = SelectedProperty.PropertyType.GetProperty("IconSize");
            iconProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), iconSize.Value, null);
            lblIconSizeValue.Text = iconSize.Value.ToString();
        }

        private void iconThickness_Scroll(object sender, EventArgs e)
        {
            var iconProp = SelectedProperty.PropertyType.GetProperty("IconThickness");
            iconProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), iconThickness.Value, null);
            lblIconThicknessValue.Text = iconThickness.Value.ToString();
        }

        private void tabDrawing_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabDrawing.SelectedIndex > 0 && (SelectedProperty.PropertyType != typeof(PointOfInterestRendering) && SelectedProperty.PropertyType != typeof(PortalRendering)))
            {
                tabDrawing.SelectTab(0);
                MessageBox.Show("This property type does not support Labels or Lines.");
            }
        }

        private void chkLabel_Clicked(object sender, EventArgs e)
        {
            var labelPropColor = SelectedProperty.PropertyType.GetProperty("LabelColor");
            if (chkLabel.Checked)
            {
                labelPropColor.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), btnLabelColor.BackColor, null);
            }
            else
            {
                labelPropColor.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), Color.Empty, null);
            }
        }

        private void btnLabelColor_Click(object sender, EventArgs e)
        {
            var colorDlg = new ColorDialog();
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                var labelPropColor = SelectedProperty.PropertyType.GetProperty("LabelColor");
                labelPropColor.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), colorDlg.Color, null);
                btnLabelColor.BackColor = colorDlg.Color;
                chkLabel.Checked = (colorDlg.Color.A > 0);
            }
        }

        private void btnFont_Click(object sender, EventArgs e)
        {
            dynamic labelProp = SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null);
            var labelFont = labelProp.LabelFont;
            var labelSize = labelProp.LabelFontSize;
            if (labelFont == null)
            {
                labelFont = "Helvetica";
                labelSize = 16;
            }
            var fontDlg = new FontDialog();
            fontDlg.Font = new Font(labelFont, labelSize, FontStyle.Regular);
            if (fontDlg.ShowDialog() == DialogResult.OK)
            {
                var labelPropFont = SelectedProperty.PropertyType.GetProperty("LabelFont");
                var labelPropFontSize = SelectedProperty.PropertyType.GetProperty("LabelFontSize");
                labelPropFont.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), fontDlg.Font.Name, null);
                labelPropFontSize.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), fontDlg.Font.Size, null);
            }
        }

        private void chkLine_Clicked(object sender, EventArgs e)
        {
            var lineProp = SelectedProperty.PropertyType.GetProperty("LineColor");
            if (chkLine.Checked)
            {
                lineProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), btnLineColor.BackColor, null);
            }
            else
            {
                lineProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), Color.Empty, null);
            }
        }

        private void btnLineColor_Click(object sender, EventArgs e)
        {
            var colorDlg = new ColorDialog();
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                var linePropColor = SelectedProperty.PropertyType.GetProperty("LineColor");
                linePropColor.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), colorDlg.Color, null);
                btnLineColor.BackColor = colorDlg.Color;
                chkLine.Checked = (colorDlg.Color.A > 0);
            }
        }

        private void lineArrowSize_Scroll(object sender, EventArgs e)
        {
            var iconProp = SelectedProperty.PropertyType.GetProperty("ArrowHeadSize");
            iconProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), lineArrowSize.Value, null);
            lblLineArrowSizeValue.Text = lineArrowSize.Value.ToString();
        }

        private void lineThicknessSize_Scroll(object sender, EventArgs e)
        {
            var iconProp = SelectedProperty.PropertyType.GetProperty("LineThickness");
            iconProp.SetValue(SelectedProperty.GetValue(MapAssistConfiguration.Loaded.MapConfiguration, null), lineThicknessSize.Value, null);
            lblLineThicknessSizeValue.Text = lineThicknessSize.Value.ToString();
        }

        private void cboLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.LanguageCode = (Locale)cboLanguage.SelectedIndex;
            PointOfInterestHandler.UpdateLocalizationNames();
        }

        private void chkItemLogEnabled_CheckedChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.ItemLog.Enabled = chkItemLogEnabled.Checked;
        }

        private void txtFilterFile_TextChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.ItemLog.FilterFileName = txtFilterFile.Text;
        }

        private void chkPlaySound_CheckedChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.ItemLog.PlaySoundOnDrop = chkPlaySound.Checked;
        }

        private void txtSoundFile_TextChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.ItemLog.SoundFile = txtSoundFile.Text;
        }

        private void txtSoundFile_LostFocus(object sender, EventArgs e)
        {
            AudioPlayer.LoadNewSound(true);
        }

        private void itemDisplayForSeconds_Scroll(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.ItemLog.DisplayForSeconds = (double)itemDisplayForSeconds.Value * 5;
            lblItemDisplayForSecondsValue.Text = $"{itemDisplayForSeconds.Value * 5} s";
        }

        private void btnLogFont_Click(object sender, EventArgs e)
        {
            var labelFont = MapAssistConfiguration.Loaded.ItemLog.LabelFont;
            var labelSize = MapAssistConfiguration.Loaded.ItemLog.LabelFontSize;
            if (labelFont == null)
            {
                labelFont = "Helvetica";
                labelSize = 16;
            }
            var fontDlg = new FontDialog();
            fontDlg.Font = new Font(labelFont, labelSize, FontStyle.Regular);
            if (fontDlg.ShowDialog() == DialogResult.OK)
            {
                MapAssistConfiguration.Loaded.ItemLog.LabelFont = fontDlg.Font.Name;
                MapAssistConfiguration.Loaded.ItemLog.LabelFontSize = fontDlg.Font.Size;
            }
        }

        private void txtToggleMapKey_TextChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.HotkeyConfiguration.ToggleKey = txtToggleMapKey.Text;
        }

        private void txtAreaLevelKey_TextChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.HotkeyConfiguration.AreaLevelKey = txtAreaLevelKey.Text;
        }

        private void txtZoomInKey_TextChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomInKey = txtZoomInKey.Text;
        }

        private void txtZoomOutKey_TextChanged(object sender, EventArgs e)
        {
            MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomOutKey = txtZoomOutKey.Text;
        }

        private void btnWalkableColor_Click(object sender, EventArgs e)
        {
            var colorDlg = new ColorDialog();
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                MapAssistConfiguration.Loaded.MapColorConfiguration.Walkable = colorDlg.Color;
                btnWalkableColor.BackColor = colorDlg.Color;
                chkWalkableColor.Checked = (colorDlg.Color.A > 0);
            }
        }

        private void btnBorderColor_Click(object sender, EventArgs e)
        {
            var colorDlg = new ColorDialog();
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                MapAssistConfiguration.Loaded.MapColorConfiguration.Border = colorDlg.Color;
                btnBorderColor.BackColor = colorDlg.Color;
            }
        }

        private void btnAddHidden_Click(object sender, EventArgs e)
        {
            if (areaForm == null)
            {
                areaForm = new AddAreaForm();
            }

            areaForm.listToAddTo = "lstHidden";

            if (areaForm.Visible)
            {
                areaForm.Activate();
            }
            else
            {
                areaForm.ShowDialog(this);
            }
        }

        private void btnRemoveHidden_Click(object sender, EventArgs e)
        {
            var indexToRemove = lstHidden.SelectedIndex;
            if (indexToRemove >= 0)
            {
                lstHidden.Items.RemoveAt(indexToRemove);
                var hiddenList = new List<Area>(MapAssistConfiguration.Loaded.HiddenAreas);
                hiddenList.RemoveAt(indexToRemove);
                MapAssistConfiguration.Loaded.HiddenAreas = hiddenList.ToArray();
            }
        }

        private void btnBrowseD2Location_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    txtD2Path.Text = fbd.SelectedPath;
                }
                else
                {
                    txtD2Path.Text = "";
                }
            }
        }

        private void chkWalkableColor_CheckedChanged(object sender, EventArgs e)
        {
            var newColor = chkWalkableColor.Checked
                ? btnWalkableColor.BackColor
                : Color.Empty;

            MapAssistConfiguration.Loaded.MapColorConfiguration.Walkable = newColor;
        }
    }
}
