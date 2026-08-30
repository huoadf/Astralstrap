using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Bloxstrap.UI.Elements.Base;
using ICSharpCode.SharpZipLib.Zip;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public class DiagnosticFileItem
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string FileSizeText { get; set; } = string.Empty;
    }

    public partial class DiagnosticsDialog : WpfUiWindow
    {
        public ObservableCollection<DiagnosticFileItem> Files { get; set; } = new();

        public DiagnosticsDialog()
        {
            InitializeComponent();
            PopulateSystemInfo();
            PopulateFiles();
            CrashLogsList.ItemsSource = Files;
        }

        private void PopulateSystemInfo()
        {
            AppInfoText.Text = $"Project: {App.ProjectName}\n" +
                               $"Version: {App.Version}\n" +
                               $"Install Path: {Paths.Base}\n" +
                               $"FastFlags Managed: {App.Settings.Prop.UseFastFlagManager}";

            RobloxInfoText.Text = $"Channel: {App.Settings.Prop.Channel}\n" +
                                  $"Roblox Player Installed: {App.IsPlayerInstalled}\n" +
                                  $"Roblox Studio Installed: {App.IsStudioInstalled}\n" +
                                  $"Roblox Base Path: {Paths.Roblox}";

            SysInfoText.Text = $"Operating System: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})\n" +
                               $".NET Runtime: {RuntimeInformation.FrameworkDescription}\n" +
                               $"Process Architecture: {RuntimeInformation.ProcessArchitecture}\n" +
                               $"System Uptime: {TimeSpan.FromMilliseconds(Environment.TickCount64):hh\\:mm\\:ss}";
        }

        private void PopulateFiles()
        {
            Files.Clear();
            string robloxLogs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "logs");

            if (Directory.Exists(robloxLogs))
            {
                var dmpFiles = Directory.GetFiles(robloxLogs, "*.dmp");
                var logFiles = Directory.GetFiles(robloxLogs, "*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Take(15);

                foreach (var file in dmpFiles.Concat(logFiles))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        Files.Add(new DiagnosticFileItem
                        {
                            FileName = fi.Name,
                            FilePath = fi.FullName,
                            Timestamp = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                            FileSizeText = $"{fi.Length / 1024.0:F1} KB"
                        });
                    }
                    catch { }
                }
            }

            if (Directory.Exists(Paths.Logs))
            {
                var astralLogs = Directory.GetFiles(Paths.Logs, "*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Take(5);

                foreach (var file in astralLogs)
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        Files.Add(new DiagnosticFileItem
                        {
                            FileName = $"[Astralstrap] {fi.Name}",
                            FilePath = fi.FullName,
                            Timestamp = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                            FileSizeText = $"{fi.Length / 1024.0:F1} KB"
                        });
                    }
                    catch { }
                }
            }
        }

        private void ExportZip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string zipPath = Path.Combine(desktop, $"Astralstrap_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

                using var mem = new MemoryStream();
                using var zip = new ZipOutputStream(mem);

                foreach (var item in Files)
                {
                    if (!File.Exists(item.FilePath)) continue;
                    try
                    {
                        var entry = new ZipEntry(Path.GetFileName(item.FilePath)) { DateTime = DateTime.Now };
                        zip.PutNextEntry(entry);
                        using var fs = File.OpenRead(item.FilePath);
                        fs.CopyTo(zip);
                    }
                    catch { }
                }

                zip.CloseEntry();
                zip.Finish();
                mem.Position = 0;

                using var outFs = File.OpenWrite(zipPath);
                mem.CopyTo(outFs);

                Frontend.ShowMessageBox($"Diagnostics bundle saved to Desktop:\n{zipPath}", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to export diagnostics: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
