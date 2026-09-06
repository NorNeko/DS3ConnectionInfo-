using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.IO;
using Steamworks;
using System.Security.Permissions;
using System.Media;

namespace DS3ConnectionInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private DispatcherTimer gameStartTimer, updateTimer, pingFilterTimer;
        private ObservableCollection<Player> playerData;
        private OverlayWindow overlay;
        //private StreamWriter logWriter;

        private bool reoSpamming = false;
        private int reoSpamCnt = 0;
        private bool pingCheked = false;
        private bool hadInvaded = false;

        private readonly bool startServices;
        private bool fieldControlsReady, refreshingFields;
        public MainWindow() : this(true) { }

        // Allows the real compiled UI to be exercised without starting Steam or game timers.
        public MainWindow(bool startServices)
        {
            this.startServices = startServices;
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowUnhandledException((Exception)e.ExceptionObject, "CurrentDomain", e.IsTerminating);
            TaskScheduler.UnobservedTaskException += (s, e) => ShowUnhandledException(e.Exception, "TaskScheduler", false);
            Dispatcher.UnhandledException += (s, e) => { if (!Debugger.IsAttached) ShowUnhandledException(e.Exception, "Dispatcher", true); };

            ColumnSettings.EnsureCompatible();
            UiText.Current.SelectLanguage(Settings.Default.UILanguage);
            InitializeComponent();
            overlay = new OverlayWindow();
            Closed += MainWindow_Closed;

            gameStartTimer = new DispatcherTimer();
            gameStartTimer.Interval = TimeSpan.FromSeconds(1);
            gameStartTimer.Tick += GameStartTimer_Tick;
            if (startServices) gameStartTimer.Start();

            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(0.5);
            updateTimer.Tick += UpdateTimer_Tick;

            pingFilterTimer = new DispatcherTimer();
            pingFilterTimer.Tick += PingFilterTimer_Tick;

            playerData = new ObservableCollection<Player>();
            dataGridSession.DataContext = playerData;
            overlay.dataGrid.DataContext = playerData;
            UiText.Current.PropertyChanged += LanguageChanged;
            ApplyLanguage();

            swColVisible.IsOn = Settings.Default.SessColumnVisibility[0] == "Visible";
            swOColVisible.IsOn = Settings.Default.OverlayColVisibility[0] == "Visible";
            textColDesc.Text = UiText.Current["Description0"];
            UpdateColVisibility();
            overlay.UpdateColVisibility();

            RestoreColumnOrder();
            fieldControlsReady = true;
            RefreshFieldControls();
        }

        private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(cbLanguage.SelectedItem is ComboBoxItem item)) return;
            string language = (string)item.Tag;
            if (UiText.Current.Language == language) return;
            UiText.Current.SelectLanguage(language);
            if (startServices) Settings.Default.Save();
        }

        private void LanguageChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Language") ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            Title = UiText.Current["Title"] + " " + AppVersion.CurrentVersion;
            cbLanguage.SelectedIndex = UiText.Current.Language == "en" ? 1 : 0;
            labelGameState.Content = UiText.Current[DS3Interop.Attached ? "Running" : "Closed"];
            textColDesc.Text = UiText.Current["Description" + Math.Max(0, cbColName.SelectedIndex)];
            dataGridSession.Items.Refresh();
            overlay.RefreshLanguage();
        }

        private void ShowUnhandledException(Exception err, string type, bool fatal)
        {
            MetroDialogSettings diagSettings = new MetroDialogSettings()
            {
                ColorScheme = MetroDialogColorScheme.Accented,
                AffirmativeButtonText = UiText.Current["Copy"],
                NegativeButtonText = UiText.Current["Close"]
            };

            SystemSounds.Exclamation.Play();
            var result = this.ShowModalMessageExternal(string.Format(UiText.Current["Unhandled"], err.GetType().Name), $"{err.Message}\n{err.StackTrace}", MessageDialogStyle.AffirmativeAndNegative, diagSettings);
            if (result == MessageDialogResult.Affirmative)
                Clipboard.SetText($"{err.GetType().Name}: {err.Message}\n{err.StackTrace}");

            Close();
        }

        private void UpdateColVisibility()
        {
            for (int i = 0; i < Settings.Default.SessColumnVisibility.Count; i++)
            {
                string vis = Settings.Default.SessColumnVisibility[i];
                dataGridSession.Columns[i].Visibility = vis == "Visible" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            Player.UpdatePlayerList();
            Player.UpdateInGameInfo();
            playerData.Clear();

            foreach (Player p in Player.ActivePlayers().OrderBy(p => p.TeamAlliegance))
                playerData.Add(p);

            // Update session info column sizes
            foreach (var col in dataGridSession.Columns)
            {
                col.Width = new DataGridLength(1, DataGridLengthUnitType.Pixel);
                col.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }
            dataGridSession.UpdateLayout();

            // Update overlay column sizes
            foreach (var col in overlay.dataGrid.Columns)
            {
                col.Width = new DataGridLength(1, DataGridLengthUnitType.Pixel);
                col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
            }
            overlay.dataGrid.UpdateLayout();

            // Queue position update after the overlay has re-rendered
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(overlay.UpdatePosition));

            DS3Interop.NetStatus status = DS3Interop.GetNetworkState();
            if (reoSpamming && !Settings.Default.SpamRedEyeOrb)
            {
                reoSpamming = false;
                reoSpamCnt = 0;
            }
            if (status == DS3Interop.NetStatus.None)
            {
                if (hadInvaded && !reoSpamming) DS3Interop.ApplyEffect(11);
                if (reoSpamming)
                {
                    reoSpamCnt = (reoSpamCnt + 1) % 5;
                    if (DS3Interop.IsSearchingInvasion() ^ (reoSpamCnt != 0)) DS3Interop.ApplyEffect(11);
                }
                hadInvaded = false;
                pingCheked = false;
            }
            if (Settings.Default.UsePingFilter)
            {
                if (status == DS3Interop.NetStatus.Host || status == DS3Interop.NetStatus.TryCreateSession || DS3Interop.InLoadingScreen())
                {   // Someone invaded, or the local player summoned a phantom, or ping filter was too late
                    pingFilterTimer.Stop();
                    reoSpamming = false;
                    reoSpamCnt = 0;
                    pingCheked = false;
                }
                if (status == DS3Interop.NetStatus.Client && !pingFilterTimer.IsEnabled && !pingCheked)
                {   // Connection has been established
                    pingCheked = true;
                    pingFilterTimer.Interval = TimeSpan.FromSeconds(Settings.Default.SamplingDelay);
                    pingFilterTimer.Start();
                }
            }
            else pingFilterTimer.Stop();
        }

        private void PingFilterTimer_Tick(object sender, EventArgs e)
        {
            double sumPing = 0;  int n = 0;
            bool absPingRespected = true;
            foreach (Player p in Player.ActivePlayers())
            {
                if (p.Ping != -1)
                {
                    absPingRespected &= p.Ping < Settings.Default.MaxAbsPing;
                    sumPing += p.Ping;
                    n++;
                }
            }
            if (n == 0)
            {   // Wait until at least one player has a ping
                pingFilterTimer.Interval = TimeSpan.FromSeconds(0.5);
                return;
            }
            if ((!absPingRespected || sumPing / n > Settings.Default.MaxAvgPing) && !DS3Interop.InLoadingScreen())
            {
                var joinMethod = DS3Interop.GetJoinMethod();
                DS3Interop.LeaveSession();
                if (joinMethod == DS3Interop.JoinMethod.RedEyeOrb)
                    hadInvaded = true;
                else if (joinMethod == DS3Interop.JoinMethod.RedSign)
                    DS3Interop.ApplyEffect(10);
                else if (joinMethod == DS3Interop.JoinMethod.WhiteSign)
                    DS3Interop.ApplyEffect(4);
            }
            else reoSpamming = false;
            pingFilterTimer.Stop();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            UiText.Current.PropertyChanged -= LanguageChanged;
            if (startServices) Settings.Default.Save();
            gameStartTimer.Stop();
            updateTimer.Stop();
            pingFilterTimer.Stop();
            overlay.Close();
            if (startServices)
            {
                HotkeyManager.Disable();
                ETWPingMonitor.Stop();
            }
        }

        private void GameStartTimer_Tick(object sender, EventArgs e)
        {   // Wait for both process attach & main window existing
            if (DS3Interop.TryAttach() && DS3Interop.FindWindow())
            {
                DS3Interop.Process.EnableRaisingEvents = true;
                DS3Interop.Process.Exited += DarkSouls_HasExited;
                labelGameState.Content = UiText.Current["Running"];
                labelGameState.Foreground = Brushes.LawnGreen;

                File.WriteAllText("steam_appid.txt", "374320");
                if (!SteamAPI.Init())
                {
                    MessageBox.Show(UiText.Current["SteamInit"], UiText.Current["SteamError"], MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }

                if (Settings.Default.UseHotkeys && !HotkeyManager.Enable())
                    MessageBox.Show(UiText.Current["KeyboardInit"], UiText.Current["WindowsError"], MessageBoxButton.OK, MessageBoxImage.Error);

                if (!overlay.InstallMsgHook())
                    MessageBox.Show(UiText.Current["OverlayInit"], UiText.Current["WindowsError"], MessageBoxButton.OK, MessageBoxImage.Error);

                overlay.UpdateVisibility();
                if (swBorderless.IsOn ^ DS3Interop.Borderless)
                    DS3Interop.MakeBorderless(swBorderless.IsOn);

                HotkeyManager.AddHotkey(DS3Interop.WinHandle, () => Settings.Default.BorderlessHotkey, () => swBorderless.IsOn ^= true);
                HotkeyManager.AddHotkey(DS3Interop.WinHandle, () => Settings.Default.OverlayHotkey, () => swOverlay.IsOn ^= true);
                HotkeyManager.AddHotkey(DS3Interop.WinHandle, () => Settings.Default.PingFilterHotkey, () => Settings.Default.UsePingFilter ^= true);
                HotkeyManager.AddHotkey(DS3Interop.WinHandle, () => Settings.Default.REOHotkey, () => OnlineHotkey(11));
                HotkeyManager.AddHotkey(DS3Interop.WinHandle, () => Settings.Default.RSDHotkey, () => OnlineHotkey(10));
                HotkeyManager.AddHotkey(DS3Interop.WinHandle, () => Settings.Default.WSDHotkey, () => OnlineHotkey(4));
                HotkeyManager.AddHotkey(DS3Interop.WinHandle, () => Settings.Default.LeaveSessionHotkey, () => DS3Interop.LeaveSession());

                ETWPingMonitor.Start();
                updateTimer.Start();
                gameStartTimer.Stop();
            }
        }

        private void OnlineHotkey(int effect)
        {
            if (DS3Interop.InLoadingScreen()) return;
            if (effect == 11 && Settings.Default.SpamRedEyeOrb)
            {
                reoSpamming ^= true;
                reoSpamCnt = 0;
                if (!reoSpamming && DS3Interop.IsSearchingInvasion())
                    DS3Interop.ApplyEffect(11);
            }
            else DS3Interop.ApplyEffect(effect);
        }

        private void DarkSouls_HasExited(object sender, EventArgs e)
        {
            this.Invoke(() =>
            {
                updateTimer.Stop();
                SteamAPI.Shutdown();
                Close();
            });
        }

        private void btnFont_Click(object sender, RoutedEventArgs e)
        {
            var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(System.Drawing.Font));

            var fD = new System.Windows.Forms.FontDialog();
            fD.Font = (System.Drawing.Font)converter.ConvertFromInvariantString(Settings.Default.OverlayFont);

            if (fD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.Default.OverlayFont = converter.ConvertToInvariantString(fD.Font);
            }
        }

        private void RefreshFieldControls()
        {
            if (!fieldControlsReady) return;
            refreshingFields = true;
            try
            {
                int session = Math.Max(0, cbColName.SelectedIndex);
                int field = Math.Max(0, cbOColName.SelectedIndex);
                swColVisible.IsOn = Settings.Default.OverlayColVisibility[session] == "Visible";
                swOColVisible.IsOn = Settings.Default.OverlayColVisibility[field] == "Visible";
                textColDesc.Text = UiText.Current["Description" + session];
                string color = Settings.Default.OverlayColumnColors[field];
                fieldColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(
                    string.IsNullOrEmpty(color) ? Settings.Default.TextColor : color);
                resetFieldColor.IsEnabled = !string.IsNullOrEmpty(color);
            }
            finally { refreshingFields = false; }
        }

        private void SaveFieldSettings()
        {
            if (startServices) Settings.Default.Save();
        }

        private void SetFieldVisibility(int index, bool visible)
        {
            var values = ColumnSettings.Normalize(Settings.Default.OverlayColVisibility);
            values[index] = visible ? "Visible" : "Hidden";
            Settings.Default.OverlayColVisibility = values;
            Settings.Default.SessColumnVisibility = ColumnSettings.Normalize(values);
            UpdateColVisibility();
            overlay.UpdateColVisibility();
            RefreshFieldControls();
            SaveFieldSettings();
        }

        private void cbColName_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshFieldControls();
        private void cbOColName_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshFieldControls();

        private void swColVisible_Toggled(object sender, RoutedEventArgs e)
        {
            if (fieldControlsReady && !refreshingFields)
                SetFieldVisibility(cbColName.SelectedIndex, swColVisible.IsOn);
        }

        private void swOColVisible_Toggled(object sender, RoutedEventArgs e)
        {
            if (fieldControlsReady && !refreshingFields)
                SetFieldVisibility(cbOColName.SelectedIndex, swOColVisible.IsOn);
        }

        private void FieldColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (!fieldControlsReady || refreshingFields || !fieldColorPicker.SelectedColor.HasValue) return;
            var values = ColumnSettings.NormalizeColors(Settings.Default.OverlayColumnColors);
            // MahApps 2.4.5 reverses OldValue/NewValue in this event; use the actual selected value.
            values[cbOColName.SelectedIndex] = fieldColorPicker.SelectedColor.Value.ToString();
            Settings.Default.OverlayColumnColors = values;
            resetFieldColor.IsEnabled = true;
            SaveFieldSettings();
        }

        private void ResetFieldColor(object sender, RoutedEventArgs e)
        {
            var values = ColumnSettings.NormalizeColors(Settings.Default.OverlayColumnColors);
            values[cbOColName.SelectedIndex] = "";
            Settings.Default.OverlayColumnColors = values;
            RefreshFieldControls();
            SaveFieldSettings();
        }

        private void RestoreColumnOrder()
        {
            var order = ColumnSettings.NormalizeOrder(Settings.Default.SessionColumnOrder);
            for (int position = 0; position < order.Count; position++)
            {
                int field = int.Parse(order[position]);
                dataGridSession.Columns[field].DisplayIndex = position;
                overlay.dataGrid.Columns[field].DisplayIndex = position;
            }
        }

        private void SessionColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            if (!fieldControlsReady) return;
            var order = new System.Collections.Specialized.StringCollection();
            foreach (var column in dataGridSession.Columns.OrderBy(c => c.DisplayIndex))
                order.Add(dataGridSession.Columns.IndexOf(column).ToString());
            Settings.Default.SessionColumnOrder = order;
            RestoreColumnOrder();
            SaveFieldSettings();
        }

        private void swOverlay_Toggled(object sender, RoutedEventArgs e)
        {
            if (IsInitialized) overlay.UpdateVisibility();
        }

        private void swBorderless_Toggled(object sender, RoutedEventArgs e)
        {
            if (DS3Interop.WinThread != 0 && swBorderless.IsOn ^ DS3Interop.Borderless)
                DS3Interop.MakeBorderless(swBorderless.IsOn);
        }

        private void swToggleHotkeys_Toggled(object sender, RoutedEventArgs e)
        {
            if (!startServices) return;
            if (Settings.Default.UseHotkeys && !HotkeyManager.Enable())
                MessageBox.Show(UiText.Current["KeyboardInit"], UiText.Current["WindowsError"], MessageBoxButton.OK, MessageBoxImage.Error);

            if (!Settings.Default.UseHotkeys) HotkeyManager.Disable();
        }

        private void headerFmt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
        }

        private void btnResetSettings_Click(object sender, RoutedEventArgs e)
        {
            fieldControlsReady = false;
            Settings.Default.Reset();
            ColumnSettings.EnsureCompatible();
            UiText.Current.SelectLanguage(Settings.Default.UILanguage);
            ApplyLanguage();
            cbColName.SelectedIndex = 0;
            cbOColName.SelectedIndex = 0;
            swColVisible.IsOn = Settings.Default.SessColumnVisibility[0] == "Visible";
            swOColVisible.IsOn = Settings.Default.OverlayColVisibility[0] == "Visible";
            UpdateColVisibility();
            overlay.UpdateColVisibility();
            RestoreColumnOrder();
            fieldControlsReady = true;
            RefreshFieldControls();
            SaveFieldSettings();
        }

        private void webLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
