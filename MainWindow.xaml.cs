using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using CyberForgeStudio.Models;

namespace CyberForgeStudio
{
    public partial class MainWindow : Window
    {
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVALCOMPLETE = 0x8004;
        private string _selectedBrand = "SAMSUNG";
        private readonly ObservableCollection<ConnectedDevice> _connectedDevices = new();
        private readonly SemaphoreSlim _deviceScanLock = new(1, 1);

        private sealed record CommandResult(int ExitCode, string Output, string Error);

        public MainWindow()
        {
            InitializeComponent();
            CmbDevices.ItemsSource = _connectedDevices;
            TxtSystemInfo.Text = $"Host OS: {Environment.OSVersion.Version}";
            OperationTabs.SelectedIndex = 0;
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
            if (!await _deviceScanLock.WaitAsync(0))
            {
                return;
            }

            try
            {
                ConnectedDevice? previousSelection = CmbDevices.SelectedItem as ConnectedDevice;
                List<ConnectedDevice> discoveredDevices = new();
                LogMessage("Scanning ADB and Fastboot devices...");

                CommandResult adbResult = await RunCommandAsync("adb", "devices", "-l");
                foreach (string line in adbResult.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length < 2 || !IsAdbState(fields[1]))
                    {
                        continue;
                    }

                    string? model = null;
                    foreach (string field in fields)
                    {
                        if (field.StartsWith("model:", StringComparison.OrdinalIgnoreCase))
                        {
                            model = field.Substring("model:".Length).Replace('_', ' ');
                            break;
                        }
                    }

                    discoveredDevices.Add(new ConnectedDevice
                    {
                        Serial = fields[0],
                        Mode = "ADB",
                        State = fields[1],
                        Model = model
                    });
                }

                CommandResult fastbootResult = await RunCommandAsync("fastboot", "devices");
                foreach (string line in fastbootResult.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length == 0 || discoveredDevices.Exists(device => device.Serial == fields[0] && device.Mode == "Fastboot"))
                    {
                        continue;
                    }

                    discoveredDevices.Add(new ConnectedDevice
                    {
                        Serial = fields[0],
                        Mode = "Fastboot",
                        State = fields.Length > 1 ? fields[1] : "fastboot"
                    });
                }

                _connectedDevices.Clear();
                foreach (ConnectedDevice device in discoveredDevices)
                {
                    _connectedDevices.Add(device);
                }

                if (previousSelection != null)
                {
                    foreach (ConnectedDevice device in _connectedDevices)
                    {
                        if (device.Serial == previousSelection.Serial && device.Mode == previousSelection.Mode)
                        {
                            CmbDevices.SelectedItem = device;
                            break;
                        }
                    }
                }

                if (CmbDevices.SelectedItem == null)
                {
                    foreach (ConnectedDevice device in _connectedDevices)
                    {
                        if (device.State == "device")
                        {
                            CmbDevices.SelectedItem = device;
                            break;
                        }
                    }
                    CmbDevices.SelectedItem ??= _connectedDevices.Count > 0 ? _connectedDevices[0] : null;
                }

                TxtLoginStatus.Text = _connectedDevices.Count == 0
                    ? "No devices detected"
                    : $"{_connectedDevices.Count} device(s) detected";
                LogMessage($"Device scan complete: {_connectedDevices.Count} device(s) found.");
            }
            finally
            {
                _deviceScanLock.Release();
            }
        }

        private static bool IsAdbState(string state)
        {
            return state is "device" or "offline" or "unauthorized" or "recovery" or "sideload" or "no";
        }

        private async Task<CommandResult> RunCommandAsync(string command, params string[] args)
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
                    return new CommandResult(-1, string.Empty, "Could not start process.");
                }

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                string output = await outputTask;
                string error = await errorTask;

                if (!string.IsNullOrWhiteSpace(output)) LogMessage($"[OUT] {output.Trim()}");
                if (!string.IsNullOrWhiteSpace(error)) LogMessage($"[ERR] {error.Trim()}");
                LogMessage($"Operation finished (exit code {process.ExitCode}).");
                return new CommandResult(process.ExitCode, output, error);
            }
            catch (Exception ex)
            {
                LogMessage($"[Error] Could not run {command}: {ex.Message}");
                return new CommandResult(-1, string.Empty, ex.Message);
            }
        }

        private async Task RunSelectedDeviceCommandAsync(string requiredMode, params string[] args)
        {
            if (CmbDevices.SelectedItem is not ConnectedDevice device)
            {
                LogMessage("Select a connected device first, then scan again if it is missing.");
                return;
            }

            if (device.Mode != requiredMode)
            {
                LogMessage($"Selected device is in {device.Mode} mode; this action requires {requiredMode} mode.");
                return;
            }

            if (device.Mode == "ADB" && device.State != "device")
            {
                LogMessage($"ADB device {device.Serial} is {device.State}; authorize it and scan again.");
                return;
            }

            string[] targetedArguments = new string[args.Length + 2];
            targetedArguments[0] = "-s";
            targetedArguments[1] = device.Serial;
            Array.Copy(args, 0, targetedArguments, 2, args.Length);
            await RunCommandAsync(device.Mode == "ADB" ? "adb" : "fastboot", targetedArguments);
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
                    "SAMSUNG" => 0,
                    "QUALCOMM" => 4,
                    "MEDIATEK" or "UNISOC" => 3,
                    _ => 1
                };

                foreach (UIElement item in BrandSelector.Children)
                {
                    if (item is Button brandButton)
                    {
                        bool isSelected = ReferenceEquals(brandButton, selectedButton);
                        brandButton.Background = isSelected
                            ? (System.Windows.Media.Brush)FindResource("AccentBlue")
                            : (System.Windows.Media.Brush)FindResource("CardBg");
                        brandButton.BorderBrush = isSelected
                            ? (System.Windows.Media.Brush)FindResource("AccentBlue")
                            : (System.Windows.Media.Brush)FindResource("BorderBrush");
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
            if (CmbDevices.SelectedItem is not ConnectedDevice device)
            {
                await ScanUsbDevicesAsync();
                if (CmbDevices.SelectedItem is not ConnectedDevice)
                {
                    LogMessage("No selectable device found. Check the USB cable, drivers, and device mode.");
                    return;
                }
                device = (ConnectedDevice)CmbDevices.SelectedItem;
            }

            LogMessage($"Reading information for {device.DisplayName}...");
            if (device.Mode == "ADB")
            {
                await RunSelectedDeviceCommandAsync("ADB", "shell", "getprop", "ro.product.model");
                await RunSelectedDeviceCommandAsync("ADB", "shell", "getprop", "ro.build.version.release");
                await RunSelectedDeviceCommandAsync("ADB", "shell", "getprop", "ro.product.manufacturer");
            }
            else
            {
                await RunSelectedDeviceCommandAsync("Fastboot", "getvar", "product");
                await RunSelectedDeviceCommandAsync("Fastboot", "getvar", "version");
            }
        }

        private async void BtnScanDevices_Click(object sender, RoutedEventArgs e)
        {
            await ScanUsbDevicesAsync();
        }

        private async void BtnAdbRebootRecovery_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Rebooting device to Recovery Mode...");
            await RunSelectedDeviceCommandAsync("ADB", "reboot", "recovery");
        }

        private async void BtnAdbRebootDownload_Click(object sender, RoutedEventArgs e)
        {
            string targetMode = _selectedBrand == "SAMSUNG" ? "download" : "bootloader";
            string displayMode = _selectedBrand == "SAMSUNG" ? "Download Mode" : "Bootloader/Fastboot Mode";
            LogMessage($"Requesting reboot to {displayMode}...");
            await RunSelectedDeviceCommandAsync("ADB", "reboot", targetMode);
        }

        private async void BtnReboot_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Rebooting System...");
            if (CmbDevices.SelectedItem is ConnectedDevice device)
            {
                await RunSelectedDeviceCommandAsync(device.Mode, "reboot");
            }
            else
            {
                LogMessage("Select a connected device before rebooting.");
            }
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