using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace CyberForgeStudio
{
    public partial class MainWindow : Window
    {
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVALCOMPLETE = 0x8004;
        private string _selectedBrand = "SAMSUNG";

        public MainWindow()
        {
            InitializeComponent();
            TxtSystemInfo.Text = $"Host OS: {Environment.OSVersion.Version}";
            OperationTabs.SelectedIndex = 1;
            TxtLoginStatus.Text = $"Brand: {_selectedBrand}";
            LogMessage("CyberForge Studio Initialized.");
            LogMessage("Android service console ready.");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource? source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(HwndMessageHook);

            LogMessage("ADB and Fastboot device monitor active.");
            _ = ScanUsbDevicesAsync();
        }

        private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DEVICECHANGE)
            {
                int eventType = wParam.ToInt32();
                if (eventType == DBT_DEVICEARRIVAL)
                {
                    LogMessage("USB device connected. Refreshing device lists...");
                    _ = ScanUsbDevicesAsync();
                }
                else if (eventType == DBT_DEVICEREMOVALCOMPLETE)
                {
                    LogMessage("USB device disconnected.");
                }
            }
            return IntPtr.Zero;
        }

        private async Task ScanUsbDevicesAsync()
        {
            LogMessage("Checking ADB and Fastboot connections...");
            await RunCommandAsync("adb", "devices", "-l");
            await RunCommandAsync("fastboot", "devices");
        }

        private async Task RunCommandAsync(string command, params string[] args)
        {
            LogMessage($"Executing: {command} {string.Join(" ", args)}");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ResolveToolPath(command),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                foreach (string argument in args)
                {
                    psi.ArgumentList.Add(argument);
                }

                using Process process = new Process { StartInfo = psi };
                if (!process.Start())
                {
                    LogMessage($"[Error] Could not start {command}.");
                    return;
                }

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                string output = await outputTask;
                string error = await errorTask;

                if (!string.IsNullOrWhiteSpace(output)) LogMessage($"[OUT] {output.Trim()}");
                if (!string.IsNullOrWhiteSpace(error)) LogMessage($"[ERR] {error.Trim()}");
                LogMessage($"Operation finished (exit code {process.ExitCode}).");
            }
            catch (Exception ex)
            {
                LogMessage($"[Error] Could not run {command}: {ex.Message}");
            }
        }

        private static string ResolveToolPath(string command)
        {
            string bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Binaries", $"{command}.exe");
            return File.Exists(bundledPath) ? bundledPath : $"{command}.exe";
        }

        private void LogMessage(string message)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            LstLogs.Items.Add($"[{timeStamp}] {message}");
            if (LstLogs.Items.Count > 0)
            {
                LstLogs.ScrollIntoView(LstLogs.Items[LstLogs.Items.Count - 1]);
            }
        }

        // --- BUTTON EVENT HANDLERS ---

        private void BtnBrand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button selectedButton && selectedButton.Tag is string brand)
            {
                _selectedBrand = brand;
                OperationTabs.SelectedIndex = brand switch
                {
                    "SAMSUNG" => 1,
                    "QUALCOMM" => 4,
                    "MEDIATEK" or "UNISOC" => 3,
                    _ => 2
                };

                foreach (UIElement item in BrandSelector.Children)
                {
                    if (item is Button brandButton)
                    {
                        bool isSelected = ReferenceEquals(brandButton, selectedButton);
                        brandButton.Background = isSelected
                            ? (System.Windows.Media.Brush)FindResource("AccentBlue")
                            : (System.Windows.Media.Brush)FindResource("CardBg");
                        brandButton.Foreground = isSelected
                            ? System.Windows.Media.Brushes.White
                            : (System.Windows.Media.Brush)FindResource("TextPrimary");
                    }
                }

                TxtLoginStatus.Text = $"Brand: {_selectedBrand}";
                LogMessage($"Selected {_selectedBrand}; opened its available service panel.");
            }
        }

        private async void BtnReadInfo_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Reading ADB Device Info...");
            await RunCommandAsync("adb", "devices", "-l");
            await RunCommandAsync("adb", "shell", "getprop", "ro.product.model");
            await RunCommandAsync("adb", "shell", "getprop", "ro.build.version.release");
        }

        private async void BtnScanDevices_Click(object sender, RoutedEventArgs e)
        {
            await ScanUsbDevicesAsync();
        }

        private async void BtnAdbRebootRecovery_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Rebooting device to Recovery Mode...");
            await RunCommandAsync("adb", "reboot", "recovery");
        }

        private async void BtnAdbRebootDownload_Click(object sender, RoutedEventArgs e)
        {
            string targetMode = _selectedBrand == "SAMSUNG" ? "download" : "bootloader";
            string displayMode = _selectedBrand == "SAMSUNG" ? "Download Mode" : "Bootloader/Fastboot Mode";
            LogMessage($"Requesting reboot to {displayMode}...");
            await RunCommandAsync("adb", "reboot", targetMode);
        }

        private async void BtnReboot_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Rebooting System...");
            await RunCommandAsync("adb", "reboot");
        }

        private void BtnDeviceManager_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Opening Windows Device Manager...");
            try
            {
                Process.Start(new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogMessage($"[Error] Could not open Device Manager: {ex.Message}");
            }
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LstLogs.Items.Clear();
            LogMessage("Log console cleared.");
        }

        private void BtnLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                LogMessage($"Opening URL: {url}");
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = uri.AbsoluteUri,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    LogMessage($"Failed to open URL: {ex.Message}");
                }
            }
        }
    }
}