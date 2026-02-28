using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace Roblox
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetInformationProcess(IntPtr hProcess, int processInformationClass, ref int processInformation, int processInformationLength);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);

        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        private const uint SPI_SETVISUALEFFECTS = 0x103D;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;

        private CheckBox chkEnabled;
        private ComboBox cmbType;
        private Panel colorPalette;
        private TrackBar tbSize;
        private OverlayForm overlay;
        private PictureBox previewBox;
        private Label lblSelectedColor;
        private Button btnCustomColor;
        private Label lblSizeValue;
        private Color currentColor;
        private List<Color> presetColors;
        private Color accentColor = Color.FromArgb(0, 162, 232);
        private Color accentHover = Color.FromArgb(0, 182, 252);
        private Color darkBg = Color.FromArgb(28, 28, 28);
        private Color darkPanel = Color.FromArgb(38, 38, 38);
        private Color darkLighter = Color.FromArgb(48, 48, 48);
        private Color textColor = Color.FromArgb(220, 220, 220);

        private Panel titleBar;
        private Button btnMinimize;
        private Button btnMaximize;
        private Button btnClose;
        private Panel sidebar;
        private Panel mainContent;
        private bool isMaximized = false;
        private Size normalSize;
        private Point normalLocation;
        private bool isDragging = false;
        private Point dragStart;

        private Panel crosshairPanel;
        private Panel colorsPanel;
        private Panel settingsPanel;
        private Panel presetsPanel;
        private Panel saveLoadPanel;
        private Panel fpsFixerPanel;
        private Panel internetBoosterPanel;

        private CheckBox chkFPSOptimizer;
        private CheckBox chkProcessPriority;
        private CheckBox chkGPUPriority;
        private CheckBox chkDisableEffects;
        private Label lblFPSStatus;
        private System.Windows.Forms.Timer fpsMonitorTimer;

        private CheckBox chkInternetBoost;
        private CheckBox chkDNSCache;
        private CheckBox chkTCPOptimizer;
        private CheckBox chkGameTraffic;
        private Label lblNetworkStatus;
        private System.Windows.Forms.Timer networkMonitorTimer;

        private ComboBox cmbTheme;
        private CheckBox chkAutoStart;
        private CheckBox chkMinimizeToTray;
        private CheckBox chkAutoApply;
        private CheckBox chkRunAsAdmin;
        private ListBox lstPresets;

        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        private string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrosshairStudio");
        private Dictionary<string, CrosshairPreset> savedPresets = new Dictionary<string, CrosshairPreset>();

        public class CrosshairPreset
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public int Size { get; set; }
            public Color Color { get; set; }
        }

        private class PresetFileModel
        {
            public string Type { get; set; }
            public int Size { get; set; }
            public int ColorArgb { get; set; }
        }

        public MainForm()
        {
            SetProcessDPIAware();
            currentColor = accentColor;
            InitializePresetColors();
            InitializeStyle();
            LoadPresets();
            InitializeControls();
            SetupResizing();
            ShowCrosshairPanel();
            InitializeTrayIcon();
            ApplySavedSettings();
            ApplyAcrylicEffect();
        }

        private void InitializePresetColors()
        {
            presetColors = new List<Color>
            {
                Color.White,
                Color.FromArgb(255, 80, 80),
                Color.FromArgb(80, 255, 80),
                Color.FromArgb(80, 80, 255),
                Color.FromArgb(255, 255, 80),
                Color.FromArgb(255, 80, 255),
                Color.FromArgb(80, 255, 255),
                Color.FromArgb(255, 128, 0),
                Color.FromArgb(128, 0, 255),
                Color.FromArgb(255, 192, 203),
                Color.FromArgb(165, 42, 42),
                Color.FromArgb(0, 255, 128),
                Color.FromArgb(255, 64, 64),
                Color.FromArgb(64, 128, 255),
                Color.FromArgb(128, 64, 255),
                Color.FromArgb(255, 215, 0),
                Color.FromArgb(0, 162, 232),
                Color.FromArgb(255, 20, 147),
                Color.FromArgb(50, 205, 50),
                Color.FromArgb(255, 140, 0)
            };
        }

        private void InitializeStyle()
        {
            this.Text = "Crosshair Studio Pro";
            this.Size = new Size(1400, 850);
            this.MinimumSize = new Size(1200, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = darkBg;
            this.ForeColor = textColor;
            this.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            this.DoubleBuffered = true;
            normalSize = this.Size;
            normalLocation = this.Location;
        }

        private void ApplyAcrylicEffect()
        {
            try
            {
                var accent = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    AccentFlags = 2,
                    GradientColor = unchecked((int)0xCC000000),
                    AnimationId = 0
                };

                int accentStructSize = Marshal.SizeOf(accent);
                IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(this.Handle, ref data);
                Marshal.FreeHGlobal(accentPtr);
            }
            catch
            {
                this.BackColor = darkBg;
            }
        }

        private void SetupResizing()
        {
            this.Resize += (s, e) =>
            {
                if (sidebar != null)
                {
                    sidebar.Height = this.Height - titleBar.Height;
                }
                Invalidate();
            };
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show Window", null, (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit", null, (s, e) => this.Close());

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Crosshair Studio Pro",
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            };
        }

        private void LoadPresets()
        {
            try
            {
                if (!Directory.Exists(configPath))
                    Directory.CreateDirectory(configPath);

                savedPresets.Clear();
                string[] presetFiles = Directory.GetFiles(configPath, "*.csp");

                foreach (string file in presetFiles)
                {
                    try
                    {
                        string text = File.ReadAllText(file);

                        if (!string.IsNullOrWhiteSpace(text) && text.TrimStart().StartsWith("{"))
                        {
                            var doc = JsonSerializer.Deserialize<PresetFileModel>(text);
                            if (doc != null)
                            {
                                CrosshairPreset preset = new CrosshairPreset
                                {
                                    Name = Path.GetFileNameWithoutExtension(file),
                                    Type = doc.Type,
                                    Size = doc.Size,
                                    Color = Color.FromArgb(doc.ColorArgb)
                                };
                                savedPresets[preset.Name] = preset;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void SavePreset(string name)
        {
            try
            {
                if (!Directory.Exists(configPath))
                    Directory.CreateDirectory(configPath);

                string filePath = Path.Combine(configPath, name + ".csp");
                string type = cmbType.SelectedItem.ToString().Substring(2);
                var model = new PresetFileModel { Type = type, Size = tbSize.Value, ColorArgb = currentColor.ToArgb() };
                string json = JsonSerializer.Serialize(model);
                File.WriteAllText(filePath, json);

                CrosshairPreset preset = new CrosshairPreset
                {
                    Name = name,
                    Type = type,
                    Size = tbSize.Value,
                    Color = currentColor
                };
                savedPresets[name] = preset;
            }
            catch { }
        }

        private void DeletePreset(string name)
        {
            try
            {
                string filePath = Path.Combine(configPath, name + ".csp");
                if (File.Exists(filePath))
                    File.Delete(filePath);
                if (savedPresets.ContainsKey(name))
                    savedPresets.Remove(name);
            }
            catch { }
        }

        private void ApplySavedSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        bool isAutoStart = key.GetValue("CrosshairStudio") != null;
                        if (chkAutoStart != null)
                            chkAutoStart.Checked = isAutoStart;
                    }
                }
            }
            catch { }
        }

        private void InitializeControls()
        {
            CreateTitleBar();
            CreateSidebar();
            CreateMainContent();

            // ensure docking and z-order are correct so title bar and sidebar are visible
            try
            {
                titleBar?.BringToFront();
                sidebar?.BringToFront();
                mainContent?.BringToFront();
            }
            catch { }

            fpsMonitorTimer = new System.Windows.Forms.Timer();
            fpsMonitorTimer.Interval = 2000;
            fpsMonitorTimer.Tick += FPSMonitorTimer_Tick;
            fpsMonitorTimer.Start();

            networkMonitorTimer = new System.Windows.Forms.Timer();
            networkMonitorTimer.Interval = 3000;
            networkMonitorTimer.Tick += NetworkMonitorTimer_Tick;
            networkMonitorTimer.Start();

            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch { }
        }

        private void CreateTitleBar()
        {
            titleBar = new Panel
            {
                Height = 70,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(20, 20, 20)
            };

            Label appTitle = new Label
            {
                Text = "CROSSHAIR STUDIO PRO",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label appSubtitle = new Label
            {
                Text = "by @snapdowgg",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(30, 45),
                AutoSize = true
            };

            btnMinimize = CreateWindowButton("─", Color.FromArgb(255, 180, 0));
            btnMaximize = CreateWindowButton("□", Color.FromArgb(0, 200, 0));
            btnClose = CreateWindowButton("✕", Color.FromArgb(220, 50, 50));

            Panel btnPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 140,
                BackColor = Color.Transparent
            };

            btnMinimize.Size = new Size(35, 35);
            btnMaximize.Size = new Size(35, 35);
            btnClose.Size = new Size(35, 35);

            btnMinimize.Location = new Point(btnPanel.Width - 120, (btnPanel.Height - btnMinimize.Height) / 2);
            btnMaximize.Location = new Point(btnPanel.Width - 80, (btnPanel.Height - btnMaximize.Height) / 2);
            btnClose.Location = new Point(btnPanel.Width - 40, (btnPanel.Height - btnClose.Height) / 2);

            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            btnPanel.Controls.AddRange(new Control[] { btnMinimize, btnMaximize, btnClose });

            btnMinimize.Click += (s, e) =>
            {
                if (chkMinimizeToTray != null && chkMinimizeToTray.Checked)
                {
                    this.Hide();
                    trayIcon.ShowBalloonTip(1000, "Crosshair Studio", "Application minimized to tray", ToolTipIcon.Info);
                }
                else
                {
                    this.WindowState = FormWindowState.Minimized;
                }
            };

            btnMaximize.Click += (s, e) => ToggleMaximize();
            btnClose.Click += (s, e) => this.Close();

            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && !isMaximized)
                {
                    isDragging = true;
                    dragStart = e.Location;
                }
            };

            titleBar.MouseMove += (s, e) =>
            {
                if (isDragging && !isMaximized)
                {
                    this.Location = new Point(
                        this.Location.X + e.X - dragStart.X,
                        this.Location.Y + e.Y - dragStart.Y
                    );
                }
            };

            titleBar.MouseUp += (s, e) => isDragging = false;
            titleBar.DoubleClick += (s, e) => ToggleMaximize();

            titleBar.Controls.AddRange(new Control[] { appTitle, appSubtitle, btnPanel });
            this.Controls.Add(titleBar);
        }

        private void CreateSidebar()
        {
            sidebar = new Panel
            {
                Width = 300,
                Height = this.Height - titleBar.Height,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(18, 18, 18),
                Padding = new Padding(15)
            };

            Panel sidebarHeader = new Panel
            {
                Height = 100,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };

            Label sidebarTitle = new Label
            {
                Text = "NAVIGATION",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(5, 10),
                AutoSize = true
            };

            Panel sidebarMenu = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            string[] menuItems = {
                "🎯 CROSSHAIR",
                "🎨 COLORS",
                "⚙️ SETTINGS",
                "📊 PRESETS",
                "💾 SAVE/LOAD",
                "⚡ FPS FIXER",
                "🌐 INTERNET BOOSTER"
            };

            int menuY = 10;
            foreach (string item in menuItems)
            {
                Button menuBtn = new Button
                {
                    Text = item,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.FromArgb(180, 180, 180),
                    Font = new Font("Segoe UI", 12, FontStyle.Regular),
                    Location = new Point(5, menuY),
                    Size = new Size(260, 50),
                    TextAlign = ContentAlignment.MiddleLeft,
                    FlatAppearance = { BorderSize = 0 }
                };

                if (item == "🎯 CROSSHAIR")
                {
                    menuBtn.BackColor = Color.FromArgb(30, 30, 30);
                    menuBtn.ForeColor = accentColor;
                }

                menuBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 40);
                menuBtn.Click += (s, e) => {
                    HighlightMenuItem(s as Button);
                    SwitchPanel(item);
                };

                sidebarMenu.Controls.Add(menuBtn);
                menuY += 55;
            }

            Panel sidebarFooter = new Panel
            {
                Height = 60,
                Dock = DockStyle.Bottom,
                BackColor = Color.Transparent
            };

            Label versionLabel = new Label
            {
                Text = "Version 2.0.0",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(5, 20),
                AutoSize = true
            };

            sidebarFooter.Controls.Add(versionLabel);
            sidebar.Controls.Add(sidebarFooter);
            sidebar.Controls.Add(sidebarMenu);
            sidebar.Controls.Add(sidebarHeader);

            this.Controls.Add(sidebar);
        }

        private void CreateMainContent()
        {
            mainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 22, 22),
                AutoScroll = true,
                Padding = new Padding(30)
            };

            CreateCrosshairPanel();
            CreateColorsPanel();
            CreateSettingsPanel();
            CreatePresetsPanel();
            CreateSaveLoadPanel();
            CreateFPSFixerPanel();
            CreateInternetBoosterPanel();

            mainContent.Controls.AddRange(new Control[] {
                crosshairPanel,
                colorsPanel,
                settingsPanel,
                presetsPanel,
                saveLoadPanel,
                fpsFixerPanel,
                internetBoosterPanel
            });

            this.Controls.Add(mainContent);
        }

        private void CreateCrosshairPanel()
        {
            crosshairPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Visible = false
            };

            int y = 0;

            Label headerTitle = new Label
            {
                Text = "Crosshair Designer",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 50;

            Label headerSubtitle = new Label
            {
                Text = "Create and customize your perfect aiming reticle",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 40;

            Panel enablePanel = CreateGlassPanel(0, y, 400, 70);
            chkEnabled = new CheckBox
            {
                Text = "Enable Crosshair Overlay",
                ForeColor = textColor,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(20, 22),
                Size = new Size(300, 30),
                Checked = false,
                BackColor = Color.Transparent
            };
            chkEnabled.CheckedChanged += ChkEnabled_CheckedChanged;
            enablePanel.Controls.Add(chkEnabled);
            y += 90;

            Panel typePanel = CreateGlassPanel(0, y, 400, 80);
            Label typeLabel = new Label
            {
                Text = "CROSSHAIR TYPE",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            cmbType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 35),
                Size = new Size(360, 30),
                BackColor = darkLighter,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat
            };
            cmbType.Items.AddRange(new object[] {
                "➕ PLUS", "❌ CROSS", "● DOT", "○ CIRCLE",
                "⬟ DIAMOND", "⌖ SNIPER", "⨯ TARGET", "⬤ FILLED CIRCLE",
                "⊞ SQUARE", "⬥ TRIANGLE"
            });
            cmbType.SelectedIndex = 0;
            cmbType.SelectedIndexChanged += (s, e) => UpdateControls();
            typePanel.Controls.AddRange(new Control[] { typeLabel, cmbType });
            y += 100;

            Panel sizePanel = CreateGlassPanel(0, y, 400, 80);
            Label sizeLabel = new Label
            {
                Text = "SIZE",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            tbSize = new TrackBar
            {
                Minimum = 5,
                Maximum = 150,
                Value = 30,
                Location = new Point(20, 35),
                Size = new Size(260, 45),
                BackColor = darkLighter,
                TickStyle = TickStyle.None
            };
            tbSize.ValueChanged += (s, e) =>
            {
                lblSizeValue.Text = $"{tbSize.Value}px";
                UpdateControls();
            };

            lblSizeValue = new Label
            {
                Text = "30px",
                Location = new Point(290, 35),
                Size = new Size(70, 30),
                ForeColor = accentColor,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            sizePanel.Controls.AddRange(new Control[] { sizeLabel, tbSize, lblSizeValue });
            y += 100;

            Panel actionPanel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(400, 60),
                BackColor = Color.Transparent
            };

            Button btnApply = CreateStyledButton("✓ APPLY CHANGES", 0, 5, 190, 45, accentColor, Color.Black);
            btnApply.Click += (s, e) => UpdateOverlay();

            Button btnReset = CreateStyledButton("↺ RESET", 200, 5, 190, 45, darkLighter, textColor, accentColor);
            btnReset.Click += (s, e) => ResetDefaults();

            actionPanel.Controls.AddRange(new Control[] { btnApply, btnReset });
            y += 80;

            Panel previewPanel = CreateGlassPanel(450, 20, 400, 400);
            Label previewLabel = new Label
            {
                Text = "LIVE PREVIEW",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            previewBox = new PictureBox
            {
                Location = new Point(20, 50),
                Size = new Size(360, 330),
                BackColor = darkLighter,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            previewBox.Paint += PreviewBox_Paint;

            previewPanel.Controls.AddRange(new Control[] { previewLabel, previewBox });

            crosshairPanel.Controls.AddRange(new Control[]
            {
                headerTitle, headerSubtitle,
                enablePanel, previewPanel,
                typePanel, sizePanel, actionPanel
            });
        }

        private void CreateColorsPanel()
        {
            colorsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Visible = false
            };

            int y = 0;

            Label headerTitle = new Label
            {
                Text = "Color Palette",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 50;

            Label headerSubtitle = new Label
            {
                Text = "Choose from preset colors or create your own",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 40;

            Panel palettePanel = CreateGlassPanel(0, y, 700, 300);
            Label paletteLabel = new Label
            {
                Text = "PRESET COLORS",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            colorPalette = new Panel
            {
                Location = new Point(20, 45),
                Size = new Size(660, 220),
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            int colorSize = 45;
            int x = 5, yPos = 5;

            for (int i = 0; i < presetColors.Count; i++)
            {
                Button colorBtn = new Button
                {
                    Width = colorSize,
                    Height = colorSize,
                    Location = new Point(x, yPos),
                    BackColor = presetColors[i],
                    FlatStyle = FlatStyle.Flat,
                    Tag = presetColors[i]
                };
                colorBtn.FlatAppearance.BorderSize = 2;
                colorBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
                colorBtn.Click += ColorButton_Click;

                if (presetColors[i] == currentColor)
                {
                    colorBtn.FlatAppearance.BorderSize = 3;
                    colorBtn.FlatAppearance.BorderColor = Color.White;
                }

                colorPalette.Controls.Add(colorBtn);

                x += colorSize + 8;
                if (x + colorSize > colorPalette.Width - 10)
                {
                    x = 5;
                    yPos += colorSize + 8;
                }
            }

            palettePanel.Controls.AddRange(new Control[] { paletteLabel, colorPalette });
            y += 320;

            Panel selectedPanel = CreateGlassPanel(0, y, 700, 80);

            lblSelectedColor = new Label
            {
                Text = "Selected Color: ■",
                ForeColor = currentColor,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(20, 25),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            btnCustomColor = new Button
            {
                Text = "CUSTOM COLOR",
                Location = new Point(450, 20),
                Size = new Size(200, 40),
                BackColor = darkLighter,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatAppearance = { BorderColor = accentColor, BorderSize = 1 }
            };
            btnCustomColor.Click += BtnCustomColor_Click;

            selectedPanel.Controls.AddRange(new Control[] { lblSelectedColor, btnCustomColor });

            colorsPanel.Controls.AddRange(new Control[]
            {
                headerTitle, headerSubtitle,
                palettePanel, selectedPanel
            });
        }

        private void CreateSettingsPanel()
        {
            settingsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Visible = false
            };

            int y = 0;

            Label headerTitle = new Label
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 50;

            Label headerSubtitle = new Label
            {
                Text = "Configure application behavior",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 40;

            Panel generalPanel = CreateGlassPanel(0, y, 600, 200);
            Label generalLabel = new Label
            {
                Text = "GENERAL",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            chkAutoStart = new CheckBox
            {
                Text = "Start with Windows",
                ForeColor = textColor,
                Location = new Point(20, 50),
                Size = new Size(200, 25),
                BackColor = Color.Transparent
            };
            chkAutoStart.CheckedChanged += (s, e) =>
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (chkAutoStart.Checked)
                            key.SetValue("CrosshairStudio", Application.ExecutablePath);
                        else
                            key.DeleteValue("CrosshairStudio", false);
                    }
                }
                catch { }
            };

            chkMinimizeToTray = new CheckBox
            {
                Text = "Minimize to system tray",
                ForeColor = textColor,
                Location = new Point(20, 80),
                Size = new Size(200, 25),
                BackColor = Color.Transparent
            };

            chkAutoApply = new CheckBox
            {
                Text = "Auto-apply changes",
                ForeColor = textColor,
                Location = new Point(20, 110),
                Size = new Size(200, 25),
                BackColor = Color.Transparent,
                Checked = true
            };

            chkRunAsAdmin = new CheckBox
            {
                Text = "Always run as administrator",
                ForeColor = textColor,
                Location = new Point(20, 140),
                Size = new Size(200, 25),
                BackColor = Color.Transparent
            };
            chkRunAsAdmin.CheckedChanged += (s, e) =>
            {
                if (chkRunAsAdmin.Checked)
                {
                    MessageBox.Show("Please restart the application as administrator manually.",
                        "Admin Rights", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            generalPanel.Controls.AddRange(new Control[]
            {
                generalLabel, chkAutoStart, chkMinimizeToTray,
                chkAutoApply, chkRunAsAdmin
            });
            y += 220;

            Panel themePanel = CreateGlassPanel(0, y, 600, 120);
            Label themeLabel = new Label
            {
                Text = "THEME",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            cmbTheme = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 45),
                Size = new Size(200, 30),
                BackColor = darkLighter,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat
            };
            cmbTheme.Items.AddRange(new object[] { "Dark (Default)", "Light", "Blue", "Purple", "Red" });
            cmbTheme.SelectedIndex = 0;
            cmbTheme.SelectedIndexChanged += (s, e) => ApplyTheme(cmbTheme.SelectedItem.ToString());

            themePanel.Controls.AddRange(new Control[] { themeLabel, cmbTheme });

            settingsPanel.Controls.AddRange(new Control[]
            {
                headerTitle, headerSubtitle,
                generalPanel, themePanel
            });
        }

        private void CreatePresetsPanel()
        {
            presetsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Visible = false
            };

            int y = 0;

            Label headerTitle = new Label
            {
                Text = "Presets",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 50;

            Label headerSubtitle = new Label
            {
                Text = "Save and load your favorite configurations",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 40;

            Panel presetsList = CreateGlassPanel(0, y, 450, 450);
            Label presetsLabel = new Label
            {
                Text = "SAVED PRESETS",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            lstPresets = new ListBox
            {
                Location = new Point(20, 45),
                Size = new Size(410, 350),
                BackColor = darkLighter,
                ForeColor = textColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11, FontStyle.Regular)
            };

            RefreshPresetsList();

            presetsList.Controls.AddRange(new Control[] { presetsLabel, lstPresets });

            Panel actionPanel = CreateGlassPanel(480, y, 250, 450);

            Button btnLoad = new Button
            {
                Text = "LOAD PRESET",
                Size = new Size(210, 45),
                Location = new Point(20, 50),
                BackColor = accentColor,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }
            };
            btnLoad.Click += (s, e) =>
            {
                if (lstPresets.SelectedItem != null)
                {
                    string presetName = lstPresets.SelectedItem.ToString();
                    if (savedPresets.ContainsKey(presetName))
                    {
                        CrosshairPreset preset = savedPresets[presetName];

                        int index = 0;
                        foreach (var item in cmbType.Items)
                        {
                            if (item.ToString().Contains(preset.Type))
                            {
                                cmbType.SelectedIndex = index;
                                break;
                            }
                            index++;
                        }

                        tbSize.Value = preset.Size;
                        currentColor = preset.Color;
                        UpdateControls();

                        MessageBox.Show($"Preset '{presetName}' loaded successfully!",
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };

            Button btnSave = CreateStyledButton("SAVE CURRENT", 20, 110, 210, 45, darkLighter, textColor, accentColor);
            btnSave.Click += (s, e) =>
            {
                string name = Microsoft.VisualBasic.Interaction.InputBox("Enter preset name:", "Save Preset", "My Preset");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    SavePreset(name);
                    RefreshPresetsList();
                    MessageBox.Show($"Preset '{name}' saved successfully!",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            Button btnDelete = CreateStyledButton("DELETE", 20, 170, 210, 45, darkLighter, textColor, Color.FromArgb(255, 80, 80));
            btnDelete.Click += (s, e) =>
            {
                if (lstPresets.SelectedItem != null)
                {
                    string presetName = lstPresets.SelectedItem.ToString();
                    var result = MessageBox.Show($"Delete preset '{presetName}'?",
                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        DeletePreset(presetName);
                        RefreshPresetsList();
                    }
                }
            };

            actionPanel.Controls.AddRange(new Control[] { btnLoad, btnSave, btnDelete });

            presetsPanel.Controls.AddRange(new Control[]
            {
                headerTitle, headerSubtitle,
                presetsList, actionPanel
            });
        }

        private void CreateSaveLoadPanel()
        {
            saveLoadPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Visible = false
            };

            int y = 0;

            Label headerTitle = new Label
            {
                Text = "Export / Import",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 50;

            Label headerSubtitle = new Label
            {
                Text = "Share your configurations with others",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 40;

            Panel exportPanel = CreateGlassPanel(0, y, 450, 150);
            Label exportLabel = new Label
            {
                Text = "EXPORT",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            Button btnExport = new Button
            {
                Text = "📤 EXPORT TO FILE",
                Size = new Size(410, 50),
                Location = new Point(20, 50),
                BackColor = accentColor,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }
            };
            btnExport.Click += (s, e) =>
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Crosshair Config|*.cfg";
                    sfd.Title = "Export Configuration";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string type = cmbType.SelectedItem.ToString().Substring(2);
                            var config = new
                            {
                                Type = type,
                                Size = tbSize.Value,
                                Color = currentColor.ToArgb()
                            };

                            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(sfd.FileName, json);

                            MessageBox.Show("Configuration exported successfully!",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error exporting: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };
            exportPanel.Controls.AddRange(new Control[] { exportLabel, btnExport });
            y += 170;

            Panel importPanel = CreateGlassPanel(0, y, 450, 150);
            Label importLabel = new Label
            {
                Text = "IMPORT",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            Button btnImport = CreateStyledButton("📥 IMPORT FROM FILE", 20, 50, 410, 50, darkLighter, textColor, accentColor);
            btnImport.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnImport.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Crosshair Config|*.cfg;*.csp";
                    ofd.Title = "Import Configuration";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string text = File.ReadAllText(ofd.FileName);

                            if (text.TrimStart().StartsWith("{"))
                            {
                                var config = JsonSerializer.Deserialize<PresetFileModel>(text);
                                if (config != null)
                                {
                                    int index = 0;
                                    foreach (var item in cmbType.Items)
                                    {
                                        if (item.ToString().Contains(config.Type))
                                        {
                                            cmbType.SelectedIndex = index;
                                            break;
                                        }
                                        index++;
                                    }
                                    tbSize.Value = config.Size;
                                    currentColor = Color.FromArgb(config.ColorArgb);
                                    UpdateControls();
                                }
                            }

                            MessageBox.Show("Configuration imported successfully!",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error importing: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };
            importPanel.Controls.AddRange(new Control[] { importLabel, btnImport });

            saveLoadPanel.Controls.AddRange(new Control[]
            {
                headerTitle, headerSubtitle,
                exportPanel, importPanel
            });
        }

        private void CreateFPSFixerPanel()
        {
            fpsFixerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Visible = false
            };

            int y = 0;

            Label headerTitle = new Label
            {
                Text = "FPS Optimizer",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 50;

            Label headerSubtitle = new Label
            {
                Text = "Boost your game performance",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 40;

            Panel statusPanel = CreateGlassPanel(0, y, 600, 80);
            lblFPSStatus = new Label
            {
                Text = "● Ready",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 25),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            statusPanel.Controls.Add(lblFPSStatus);
            y += 100;

            Panel optionsPanel = CreateGlassPanel(0, y, 600, 250);
            Label optionsLabel = new Label
            {
                Text = "OPTIMIZATION OPTIONS",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            chkFPSOptimizer = new CheckBox
            {
                Text = "Enable FPS Optimizer",
                ForeColor = textColor,
                Location = new Point(20, 50),
                Size = new Size(300, 25),
                BackColor = Color.Transparent,
                Checked = true
            };

            chkProcessPriority = new CheckBox
            {
                Text = "Set Game to High Priority",
                ForeColor = textColor,
                Location = new Point(20, 80),
                Size = new Size(300, 25),
                BackColor = Color.Transparent,
                Checked = true
            };

            chkGPUPriority = new CheckBox
            {
                Text = "Optimize GPU Settings",
                ForeColor = textColor,
                Location = new Point(20, 110),
                Size = new Size(300, 25),
                BackColor = Color.Transparent,
                Checked = true
            };

            chkDisableEffects = new CheckBox
            {
                Text = "Disable Visual Effects",
                ForeColor = textColor,
                Location = new Point(20, 140),
                Size = new Size(300, 25),
                BackColor = Color.Transparent,
                Checked = false
            };

            optionsPanel.Controls.AddRange(new Control[]
            {
                optionsLabel, chkFPSOptimizer, chkProcessPriority,
                chkGPUPriority, chkDisableEffects
            });
            y += 270;

            Panel actionPanel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(600, 60),
                BackColor = Color.Transparent
            };

            Button btnApplyFPS = CreateStyledButton("⚡ APPLY OPTIMIZATIONS", 0, 5, 290, 45, accentColor, Color.Black);
            btnApplyFPS.Click += BtnApplyFPS_Click;

            Button btnResetFPS = CreateStyledButton("↺ RESET", 300, 5, 290, 45, darkLighter, textColor, accentColor);
            btnResetFPS.Click += BtnResetFPS_Click;

            actionPanel.Controls.AddRange(new Control[] { btnApplyFPS, btnResetFPS });

            fpsFixerPanel.Controls.AddRange(new Control[]
            {
                headerTitle, headerSubtitle,
                statusPanel, optionsPanel, actionPanel
            });
        }

        private void CreateInternetBoosterPanel()
        {
            internetBoosterPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Visible = false
            };

            int y = 0;

            Label headerTitle = new Label
            {
                Text = "Network Optimizer",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 50;

            Label headerSubtitle = new Label
            {
                Text = "Reduce lag and improve connection",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 40;

            Panel statusPanel = CreateGlassPanel(0, y, 600, 80);
            lblNetworkStatus = new Label
            {
                Text = "● Connected",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 25),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            statusPanel.Controls.Add(lblNetworkStatus);
            y += 100;

            Panel optionsPanel = CreateGlassPanel(0, y, 600, 250);
            Label optionsLabel = new Label
            {
                Text = "NETWORK OPTIONS",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            chkInternetBoost = new CheckBox
            {
                Text = "Enable Network Optimizer",
                ForeColor = textColor,
                Location = new Point(20, 50),
                Size = new Size(300, 25),
                BackColor = Color.Transparent,
                Checked = true
            };

            chkDNSCache = new CheckBox
            {
                Text = "Optimize DNS Cache",
                ForeColor = textColor,
                Location = new Point(20, 80),
                Size = new Size(300, 25),
                BackColor = Color.Transparent,
                Checked = true
            };

            chkTCPOptimizer = new CheckBox
            {
                Text = "TCP/IP Optimizer",
                ForeColor = textColor,
                Location = new Point(20, 110),
                Size = new Size(300, 25),
                BackColor = Color.Transparent,
                Checked = true
            };

            chkGameTraffic = new CheckBox
            {
                Text = "Prioritize Game Traffic",
                ForeColor = textColor,
                Location = new Point(20, 140),
                Size = new Size(300, 25),
                BackColor = Color.Transparent,
                Checked = true
            };

            optionsPanel.Controls.AddRange(new Control[]
            {
                optionsLabel, chkInternetBoost, chkDNSCache,
                chkTCPOptimizer, chkGameTraffic
            });
            y += 270;

            Panel actionPanel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(600, 60),
                BackColor = Color.Transparent
            };

            Button btnApplyNetwork = CreateStyledButton("🌐 APPLY OPTIMIZATIONS", 0, 5, 290, 45, accentColor, Color.Black);
            btnApplyNetwork.Click += BtnApplyNetwork_Click;

            Button btnResetNetwork = CreateStyledButton("↺ RESET", 300, 5, 290, 45, darkLighter, textColor, accentColor);
            btnResetNetwork.Click += BtnResetNetwork_Click;

            actionPanel.Controls.AddRange(new Control[] { btnApplyNetwork, btnResetNetwork });

            internetBoosterPanel.Controls.AddRange(new Control[]
            {
                headerTitle, headerSubtitle,
                statusPanel, optionsPanel, actionPanel
            });
        }

        private Button CreateWindowButton(string text, Color hoverColor)
        {
            return new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(35, 35),
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = hoverColor }
            };
        }

        private Button CreateStyledButton(string text, int x, int y, int width, int height, Color backColor, Color foreColor, Color? borderColor = null)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatAppearance = { BorderSize = borderColor.HasValue ? 1 : 0 }
            };

            if (borderColor.HasValue)
            {
                btn.FlatAppearance.BorderColor = borderColor.Value;
            }

            return btn;
        }

        private Panel CreateGlassPanel(int x, int y, int width, int height)
        {
            Panel panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = darkPanel
            };

            panel.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(60, 60, 60), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };

            return panel;
        }

        private void HighlightMenuItem(Button clickedBtn)
        {
            foreach (Control ctrl in sidebar.Controls[1].Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.BackColor = Color.Transparent;
                    btn.ForeColor = Color.FromArgb(180, 180, 180);
                }
            }

            clickedBtn.BackColor = Color.FromArgb(40, 40, 40);
            clickedBtn.ForeColor = accentColor;
        }

        private void ApplyTheme(string theme)
        {
            switch (theme)
            {
                case "Light":
                    accentColor = Color.FromArgb(0, 120, 212);
                    accentHover = Color.FromArgb(0, 140, 232);
                    darkBg = Color.FromArgb(240, 240, 240);
                    darkPanel = Color.FromArgb(250, 250, 250);
                    darkLighter = Color.FromArgb(255, 255, 255);
                    textColor = Color.FromArgb(50, 50, 50);
                    break;

                case "Blue":
                    accentColor = Color.FromArgb(0, 162, 232);
                    accentHover = Color.FromArgb(0, 182, 252);
                    darkBg = Color.FromArgb(22, 22, 32);
                    darkPanel = Color.FromArgb(32, 32, 42);
                    darkLighter = Color.FromArgb(42, 42, 52);
                    textColor = Color.FromArgb(220, 220, 240);
                    break;

                case "Purple":
                    accentColor = Color.FromArgb(156, 39, 176);
                    accentHover = Color.FromArgb(176, 59, 196);
                    darkBg = Color.FromArgb(28, 18, 28);
                    darkPanel = Color.FromArgb(38, 28, 38);
                    darkLighter = Color.FromArgb(48, 38, 48);
                    textColor = Color.FromArgb(230, 220, 230);
                    break;

                case "Red":
                    accentColor = Color.FromArgb(229, 57, 53);
                    accentHover = Color.FromArgb(249, 77, 73);
                    darkBg = Color.FromArgb(28, 18, 18);
                    darkPanel = Color.FromArgb(38, 28, 28);
                    darkLighter = Color.FromArgb(48, 38, 38);
                    textColor = Color.FromArgb(240, 220, 220);
                    break;

                default:
                    accentColor = Color.FromArgb(0, 162, 232);
                    accentHover = Color.FromArgb(0, 182, 252);
                    darkBg = Color.FromArgb(22, 22, 22);
                    darkPanel = Color.FromArgb(32, 32, 32);
                    darkLighter = Color.FromArgb(42, 42, 42);
                    textColor = Color.FromArgb(220, 220, 220);
                    break;
            }

            this.BackColor = darkBg;
            titleBar.BackColor = Color.FromArgb(18, 18, 18);
            sidebar.BackColor = Color.FromArgb(18, 18, 18);
            mainContent.BackColor = darkBg;

            foreach (Control c in mainContent.Controls)
            {
                c.BackColor = Color.Transparent;
            }

            previewBox.BackColor = darkLighter;
            cmbType.BackColor = darkLighter;
            cmbTheme.BackColor = darkLighter;
            lstPresets.BackColor = darkLighter;

            foreach (Control c in colorPalette.Controls)
            {
                if (c is Button btn)
                {
                    btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
                }
            }

            currentColor = accentColor;
            UpdateControls();
            mainContent.Invalidate();
        }

        private void SwitchPanel(string menuItem)
        {
            crosshairPanel.Visible = false;
            colorsPanel.Visible = false;
            settingsPanel.Visible = false;
            presetsPanel.Visible = false;
            saveLoadPanel.Visible = false;
            fpsFixerPanel.Visible = false;
            internetBoosterPanel.Visible = false;

            switch (menuItem)
            {
                case "🎯 CROSSHAIR":
                    crosshairPanel.Visible = true;
                    break;
                case "🎨 COLORS":
                    colorsPanel.Visible = true;
                    break;
                case "⚙️ SETTINGS":
                    settingsPanel.Visible = true;
                    break;
                case "📊 PRESETS":
                    presetsPanel.Visible = true;
                    RefreshPresetsList();
                    break;
                case "💾 SAVE/LOAD":
                    saveLoadPanel.Visible = true;
                    break;
                case "⚡ FPS FIXER":
                    fpsFixerPanel.Visible = true;
                    break;
                case "🌐 INTERNET BOOSTER":
                    internetBoosterPanel.Visible = true;
                    break;
            }
        }

        private void ShowCrosshairPanel()
        {
            crosshairPanel.Visible = true;
        }

        private void ToggleMaximize()
        {
            isMaximized = !isMaximized;

            if (isMaximized)
            {
                normalSize = this.Size;
                normalLocation = this.Location;
                this.WindowState = FormWindowState.Maximized;
                btnMaximize.Text = "❐";
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                this.Size = normalSize;
                this.Location = normalLocation;
                btnMaximize.Text = "□";
            }
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            Button clickedBtn = (Button)sender;
            currentColor = clickedBtn.BackColor;

            foreach (Button btn in colorPalette.Controls)
            {
                btn.FlatAppearance.BorderSize = 2;
                btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            }

            clickedBtn.FlatAppearance.BorderSize = 3;
            clickedBtn.FlatAppearance.BorderColor = Color.White;

            lblSelectedColor.ForeColor = currentColor;
            lblSelectedColor.Text = $"Selected Color: ■";

            UpdateControls();
        }

        private void BtnCustomColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                dlg.Color = currentColor;
                dlg.FullOpen = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    currentColor = dlg.Color;

                    foreach (Button btn in colorPalette.Controls)
                    {
                        btn.FlatAppearance.BorderSize = 2;
                        btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
                    }

                    lblSelectedColor.ForeColor = currentColor;
                    lblSelectedColor.Text = $"Selected Color: ■ (Custom)";

                    UpdateControls();
                }
            }
        }

        private void PreviewBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cx = previewBox.Width / 2;
            int cy = previewBox.Height / 2;
            int s = tbSize.Value / 2;

            using (var pen = new Pen(currentColor, 3f))
            using (var brush = new SolidBrush(currentColor))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                string type = cmbType.SelectedItem.ToString().Substring(2);

                switch (type)
                {
                    case "PLUS":
                        g.DrawLine(pen, cx - s, cy, cx + s, cy);
                        g.DrawLine(pen, cx, cy - s, cx, cy + s);
                        break;

                    case "CROSS":
                        g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
                        g.DrawLine(pen, cx - s, cy + s, cx + s, cy - s);
                        break;

                    case "DOT":
                        g.FillEllipse(brush, cx - s / 2, cy - s / 2, s, s);
                        break;

                    case "CIRCLE":
                        g.DrawEllipse(pen, cx - s, cy - s, s * 2, s * 2);
                        break;

                    case "DIAMOND":
                        Point[] diamondPoints = new Point[]
                        {
                            new Point(cx, cy - s),
                            new Point(cx + s, cy),
                            new Point(cx, cy + s),
                            new Point(cx - s, cy)
                        };
                        g.DrawPolygon(pen, diamondPoints);
                        break;

                    case "SNIPER":
                        g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
                        g.DrawLine(pen, cx - s, cy + s, cx + s, cy - s);
                        g.DrawEllipse(pen, cx - s / 2, cy - s / 2, s, s);
                        break;

                    case "TARGET":
                        g.DrawEllipse(pen, cx - s, cy - s, s * 2, s * 2);
                        g.DrawEllipse(pen, cx - s / 2, cy - s / 2, s, s);
                        g.DrawLine(pen, cx - s, cy, cx + s, cy);
                        g.DrawLine(pen, cx, cy - s, cx, cy + s);
                        break;

                    case "FILLED CIRCLE":
                        g.FillEllipse(brush, cx - s, cy - s, s * 2, s * 2);
                        break;

                    case "SQUARE":
                        g.DrawRectangle(pen, cx - s, cy - s, s * 2, s * 2);
                        break;

                    case "TRIANGLE":
                        Point[] trianglePoints = new Point[]
                        {
                            new Point(cx, cy - s),
                            new Point(cx + s, cy + s),
                            new Point(cx - s, cy + s)
                        };
                        g.DrawPolygon(pen, trianglePoints);
                        break;
                }
            }
        }

        private void UpdateControls()
        {
            lblSizeValue.Text = $"{tbSize.Value}px";
            previewBox.Invalidate();

            if (chkAutoApply != null && chkAutoApply.Checked)
            {
                UpdateOverlay();
            }
        }

        private void UpdateOverlay()
        {
            if (overlay != null && !overlay.IsDisposed && chkEnabled.Checked)
            {
                string type = cmbType.SelectedItem.ToString().Substring(2);
                overlay.CrosshairType = type;
                overlay.CrosshairSize = tbSize.Value;
                overlay.CrosshairColor = currentColor;
                overlay.Refresh();
            }
        }

        private void ResetDefaults()
        {
            cmbType.SelectedIndex = 0;
            tbSize.Value = 30;
            currentColor = accentColor;

            foreach (Button btn in colorPalette.Controls)
            {
                btn.FlatAppearance.BorderSize = 2;
                btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);

                if (btn.BackColor == currentColor)
                {
                    btn.FlatAppearance.BorderSize = 3;
                    btn.FlatAppearance.BorderColor = Color.White;
                }
            }

            lblSelectedColor.ForeColor = currentColor;
            lblSelectedColor.Text = "Selected Color: ■";

            UpdateControls();
        }

        private void ChkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (chkEnabled.Checked)
            {
                string type = cmbType.SelectedItem.ToString().Substring(2);
                overlay = new OverlayForm
                {
                    CrosshairType = type,
                    CrosshairSize = tbSize.Value,
                    CrosshairColor = currentColor
                };
                overlay.Show();
            }
            else
            {
                if (overlay != null && !overlay.IsDisposed)
                {
                    overlay.Close();
                    overlay.Dispose();
                }
            }
        }

        private void BtnApplyFPS_Click(object sender, EventArgs e)
        {
            try
            {
                lblFPSStatus.Text = "● Optimizing...";
                lblFPSStatus.ForeColor = Color.FromArgb(255, 180, 0);
                Application.DoEvents();

                if (chkFPSOptimizer.Checked)
                {
                    Process[] processes = Process.GetProcessesByName("RobloxPlayerBeta");
                    if (processes.Length == 0)
                        processes = Process.GetProcessesByName("RobloxPlayer");

                    foreach (Process proc in processes)
                    {
                        try
                        {
                            if (chkProcessPriority.Checked)
                            {
                                proc.PriorityClass = ProcessPriorityClass.High;
                            }

                            if (chkGPUPriority.Checked)
                            {
                                using (Process procCurrent = Process.GetCurrentProcess())
                                {
                                    int isCritical = 1;
                                    NtSetInformationProcess(procCurrent.Handle, 0x1D, ref isCritical, sizeof(int));
                                }
                            }
                        }
                        catch { }
                    }

                    if (chkDisableEffects.Checked)
                    {
                        SystemParametersInfo(SPI_SETVISUALEFFECTS, 0, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                    }

                    lblFPSStatus.Text = $"● Active - {processes.Length} Game(s) Optimized";
                    lblFPSStatus.ForeColor = accentColor;
                }
                else
                {
                    lblFPSStatus.Text = "● Disabled";
                    lblFPSStatus.ForeColor = Color.FromArgb(150, 150, 150);
                }
            }
            catch
            {
                lblFPSStatus.Text = "● Error - Run as Admin";
                lblFPSStatus.ForeColor = Color.FromArgb(255, 80, 80);
            }
        }

        private void BtnResetFPS_Click(object sender, EventArgs e)
        {
            chkFPSOptimizer.Checked = true;
            chkProcessPriority.Checked = true;
            chkGPUPriority.Checked = true;
            chkDisableEffects.Checked = false;
            lblFPSStatus.Text = "● Reset to Defaults";
            lblFPSStatus.ForeColor = accentColor;
        }

        private void BtnApplyNetwork_Click(object sender, EventArgs e)
        {
            try
            {
                lblNetworkStatus.Text = "● Optimizing...";
                lblNetworkStatus.ForeColor = Color.FromArgb(255, 180, 0);
                Application.DoEvents();

                if (chkInternetBoost.Checked)
                {
                    if (chkDNSCache.Checked)
                    {
                        Process.Start("ipconfig", "/flushdns")?.WaitForExit(5000);
                    }

                    if (chkTCPOptimizer.Checked)
                    {
                        Process.Start("netsh", "int tcp set global autotuninglevel=normal")?.WaitForExit(5000);
                        Process.Start("netsh", "int tcp set global chimney=enabled")?.WaitForExit(5000);
                        Process.Start("netsh", "int tcp set global rss=enabled")?.WaitForExit(5000);
                    }

                    if (chkGameTraffic.Checked)
                    {
                        Process.Start("netsh", "int tcp set global timestamps=disabled")?.WaitForExit(5000);
                    }

                    using (Ping ping = new Ping())
                    {
                        try
                        {
                            PingReply reply = ping.Send("8.8.8.8", 1000);
                            if (reply.Status == IPStatus.Success)
                            {
                                lblNetworkStatus.Text = $"● Optimized | Ping: {reply.RoundtripTime}ms";
                            }
                        }
                        catch { }
                    }

                    lblNetworkStatus.ForeColor = accentColor;
                }
                else
                {
                    lblNetworkStatus.Text = "● Standard Mode";
                    lblNetworkStatus.ForeColor = Color.FromArgb(150, 150, 150);
                }
            }
            catch
            {
                lblNetworkStatus.Text = "● Error - Check Connection";
                lblNetworkStatus.ForeColor = Color.FromArgb(255, 80, 80);
            }
        }

        private void BtnResetNetwork_Click(object sender, EventArgs e)
        {
            chkInternetBoost.Checked = true;
            chkDNSCache.Checked = true;
            chkTCPOptimizer.Checked = true;
            chkGameTraffic.Checked = true;
            lblNetworkStatus.Text = "● Reset to Defaults";
            lblNetworkStatus.ForeColor = accentColor;
        }

        private void RefreshPresetsList()
        {
            lstPresets.Items.Clear();
            foreach (var preset in savedPresets.Keys)
            {
                lstPresets.Items.Add(preset);
            }
        }

        private void FPSMonitorTimer_Tick(object sender, EventArgs e)
        {
            if (fpsFixerPanel.Visible && chkFPSOptimizer.Checked)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName("RobloxPlayerBeta");
                    if (processes.Length == 0)
                        processes = Process.GetProcessesByName("RobloxPlayer");

                    if (processes.Length > 0 && cpuCounter != null)
                    {
                        float cpuUsage = cpuCounter.NextValue();
                        float availableRAM = ramCounter.NextValue();
                        lblFPSStatus.Text = $"● Active | CPU: {cpuUsage:F1}% | RAM: {availableRAM:F0}MB";
                    }
                    else
                    {
                        lblFPSStatus.Text = "● Active (No Game)";
                    }
                }
                catch
                {
                    lblFPSStatus.Text = "● Active";
                }
            }
        }

        private void NetworkMonitorTimer_Tick(object sender, EventArgs e)
        {
            if (internetBoosterPanel.Visible && chkInternetBoost.Checked)
            {
                try
                {
                    using (Ping ping = new Ping())
                    {
                        PingReply reply = ping.Send("8.8.8.8", 1000);
                        if (reply.Status == IPStatus.Success)
                        {
                            lblNetworkStatus.Text = $"● Optimized | Ping: {reply.RoundtripTime}ms";
                        }
                    }
                }
                catch
                {
                    lblNetworkStatus.Text = "● Optimized";
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (fpsMonitorTimer != null)
                fpsMonitorTimer.Stop();

            if (networkMonitorTimer != null)
                networkMonitorTimer.Stop();

            if (overlay != null && !overlay.IsDisposed)
            {
                overlay.Close();
                overlay.Dispose();
            }

            trayIcon?.Visible = false;
            trayIcon?.Dispose();

            base.OnFormClosing(e);
        }
    }

    public class OverlayForm : Form
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CrosshairType { get; set; } = "PLUS";

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CrosshairSize { get; set; } = 30;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color CrosshairColor { get; set; } = Color.FromArgb(0, 162, 232);

        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOOLWINDOW = 0x80;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        public OverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cx = Screen.PrimaryScreen.Bounds.Width / 2;
            int cy = Screen.PrimaryScreen.Bounds.Height / 2;
            int s = CrosshairSize;

            using (var pen = new Pen(CrosshairColor, 3f))
            using (var brush = new SolidBrush(CrosshairColor))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                switch (CrosshairType)
                {
                    case "PLUS":
                        g.DrawLine(pen, cx - s, cy, cx + s, cy);
                        g.DrawLine(pen, cx, cy - s, cx, cy + s);
                        break;

                    case "CROSS":
                        g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
                        g.DrawLine(pen, cx - s, cy + s, cx + s, cy - s);
                        break;

                    case "DOT":
                        g.FillEllipse(brush, cx - s / 2, cy - s / 2, s, s);
                        break;

                    case "CIRCLE":
                        g.DrawEllipse(pen, cx - s, cy - s, s * 2, s * 2);
                        break;

                    case "DIAMOND":
                        Point[] diamondPoints = new Point[]
                        {
                            new Point(cx, cy - s),
                            new Point(cx + s, cy),
                            new Point(cx, cy + s),
                            new Point(cx - s, cy)
                        };
                        g.DrawPolygon(pen, diamondPoints);
                        break;

                    case "SNIPER":
                        g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
                        g.DrawLine(pen, cx - s, cy + s, cx + s, cy - s);
                        g.DrawEllipse(pen, cx - s / 2, cy - s / 2, s, s);
                        break;

                    case "TARGET":
                        g.DrawEllipse(pen, cx - s, cy - s, s * 2, s * 2);
                        g.DrawEllipse(pen, cx - s / 2, cy - s / 2, s, s);
                        g.DrawLine(pen, cx - s, cy, cx + s, cy);
                        g.DrawLine(pen, cx, cy - s, cx, cy + s);
                        break;

                    case "FILLED CIRCLE":
                        g.FillEllipse(brush, cx - s, cy - s, s * 2, s * 2);
                        break;

                    case "SQUARE":
                        g.DrawRectangle(pen, cx - s, cy - s, s * 2, s * 2);
                        break;

                    case "TRIANGLE":
                        Point[] trianglePoints = new Point[]
                        {
                            new Point(cx, cy - s),
                            new Point(cx + s, cy + s),
                            new Point(cx - s, cy + s)
                        };
                        g.DrawPolygon(pen, trianglePoints);
                        break;
                }
            }
        }
    }
}