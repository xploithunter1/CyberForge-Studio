using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CyberForgeStudio.Core
{
    public class AdbManager
    {
        private readonly string _adbPath;

        public AdbManager()
        {
            _adbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Binaries", "adb.exe");
        }

        public async Task<string> ExecuteCommandAsync(string command)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = _adbPath,
                        Arguments = command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using Process process = new Process { StartInfo = psi };
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    return string.IsNullOrEmpty(output) ? error.Trim() : output.Trim();
                }
                catch (Exception ex)
                {
                    return $"Exception: {ex.Message}";
                }
            });
        }

        public async Task<bool> StartServerAsync()
        {
            string response = await ExecuteCommandAsync("start-server");
            return !response.StartsWith("Exception");
        }
    }
}