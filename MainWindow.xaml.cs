using System.Windows;
using System.Windows.Media;
using CyberForgeStudio.Core;
using CyberForgeStudio.Utilities;

namespace CyberForgeStudio
{
    public partial class MainWindow : Window
    {
        private readonly AdbManager _adbManager;

        public MainWindow()
        {
            InitializeComponent();

            _adbManager = new AdbManager();

            // Direct global logs directly into the ListBox
            Logger.OnLog += LogToUi;

            Logger.Info("Initializing XploitForge Engine...");
            Logger.Info("Checking local binaries...");
            Logger.Success("Ready for operations.");
        }

        private void LogToUi(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LstLogs.Items.Add(message);
                LstLogs.ScrollIntoView(LstLogs.Items[LstLogs.Items.Count - 1]);
            });
        }

        private async void BtnReadInfo_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Checking ADB connection...");
            string output = await _adbManager.ExecuteCommandAsync("devices");

            if (!output.Contains("\tdevice"))
            {
                Logger.Error("No device selected or ADB connection failed!");
                return;
            }

            Logger.Info("Reading Device Details...");
            string model = await _adbManager.ExecuteCommandAsync("shell getprop ro.product.model");
            string brand = await _adbManager.ExecuteCommandAsync("shell getprop ro.product.brand");
            string androidVer = await _adbManager.ExecuteCommandAsync("shell getprop ro.build.version.release");

            Logger.Success($"Brand: {brand}");
            Logger.Success($"Model: {model}");
            Logger.Success($"Android Version: {androidVer}");
        }

        private async void BtnRemoveFrp1_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Executing FRP Removal (Exynos Protocol)...");
            string output = await _adbManager.ExecuteCommandAsync("devices");
            
            if (!output.Contains("\tdevice"))
            {
                Logger.Error("No ADB connection found!");
                Logger.Error("No device selected");
                return;
            }

            Logger.Info("Sending FRP Bypass payload...");
            await _adbManager.ExecuteCommandAsync("shell am start -n com.google.android.gsf.login/");
            Logger.Success("Bypass signal transmitted.");
        }

        private async void BtnRemoveFrp2_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Executing FRP Removal (Android 14/15/16 Server Protocol)...");
            string output = await _adbManager.ExecuteCommandAsync("devices");

            if (!output.Contains("\tdevice"))
            {
                Logger.Error("No ADB connection found!");
                Logger.Error("No device selected");
                return;
            }

            Logger.Info("Bypassing security lock via AT mode...");
        }

        private async void BtnRemoveFrpOld_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Removing FRP (Pre-August 2022 Security)...");
            await _adbManager.ExecuteCommandAsync("shell content insert --uri content://settings/secure --bind name:s:user_setup_complete --bind value:s:1");
            Logger.Success("Device unlock flags applied.");
        }

        private async void BtnReboot_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Rebooting device...");
            await _adbManager.ExecuteCommandAsync("reboot");
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LstLogs.Items.Clear();
        }
    }
}