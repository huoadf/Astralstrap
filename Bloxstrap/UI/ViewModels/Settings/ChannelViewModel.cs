using Bloxstrap.RobloxInterfaces;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Net;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class ChannelViewModel : NotifyPropertyChangedViewModel
    {
        public ChannelViewModel()
        {
            Task.Run(() => LoadChannelDeployInfo(App.Settings.Prop.Channel));
        }

        public IEnumerable<UpdateCheck> UpdateCheckValues => Enum.GetValues(typeof(UpdateCheck)).Cast<UpdateCheck>();

        public UpdateCheck SelectedUpdateCheck
        {
            get => App.Settings.Prop.UpdateChecks;
            set
            {
                App.Settings.Prop.UpdateChecks = value;
                OnPropertyChanged(nameof(SelectedUpdateCheck));
            }
        }

        public bool IsRobloxInstallationMissing => !App.IsPlayerInstalled && !App.IsStudioInstalled;

        private async Task LoadChannelDeployInfo(string channel)
        {
            ShowLoadingError = false;
            OnPropertyChanged(nameof(ShowLoadingError));

            ChannelInfoLoadingText = Strings.Menu_Channel_Switcher_Fetching;
            OnPropertyChanged(nameof(ChannelInfoLoadingText));

            ChannelDeployInfo = null;
            OnPropertyChanged(nameof(ChannelDeployInfo));

            try
            {
                bool isPrivate = await Deployment.IsChannelPrivate(channel);
                if (App.Cookies.Loaded && isPrivate && string.IsNullOrEmpty(Deployment.ChannelToken))
                {
                    UserChannel? userChannel = await Deployment.GetUserChannel("WindowsPlayer");

                    if (userChannel?.Token is not null)
                        Deployment.ChannelToken = userChannel.Token;
                }

                ClientVersion info = await Deployment.GetInfo(channel, true, true);

                ShowChannelWarning = info.IsBehindDefaultChannel;
                OnPropertyChanged(nameof(ShowChannelWarning));

                ChannelDeployInfo = new DeployInfo
                {
                    Version = info.Version,
                    VersionGuid = isPrivate ? "version-private" : info.VersionGuid, // we dont want to return the hash of private channels for obvious reason
                    Timestamp = info.Timestamp?.ToLocalTime().ToString() ?? "?"
                };

                App.State.Prop.IgnoreOutdatedChannel = true;

                OnPropertyChanged(nameof(ChannelDeployInfo));
            }
            catch (InvalidChannelException ex)
            {
                ShowLoadingError = true;
                OnPropertyChanged(nameof(ShowLoadingError));

                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                    ChannelInfoLoadingText = Strings.Menu_Channel_Switcher_Unauthorized;
                else
                    ChannelInfoLoadingText = $"An http error has occured ({ex.StatusCode})";

                OnPropertyChanged(nameof(ChannelInfoLoadingText));
            }
        }

        public bool ShowLoadingError { get; set; } = false;
        public bool ShowChannelWarning { get; set; } = false;

        public DeployInfo? ChannelDeployInfo { get; private set; } = null;
        public string ChannelInfoLoadingText { get; private set; } = null!;

        public string ViewChannel
        {
            get => App.Settings.Prop.Channel;
            set
            {
                value = value.Trim();
                Task.Run(() => LoadChannelDeployInfo(value));

                if (value.ToLower() == "live" || value.ToLower() == "zlive")
                {
                    App.Settings.Prop.Channel = Deployment.DefaultChannel;
                }
                else
                {
                    App.Settings.Prop.Channel = value;
                }
            }
        }

        public bool UpdateRoblox
        {
            get => App.Settings.Prop.UpdateRoblox && !IsRobloxInstallationMissing;
            set => App.Settings.Prop.UpdateRoblox = value;
        }

        public bool StaticDirectory
        {
            get => App.Settings.Prop.StaticDirectory;
            set => App.Settings.Prop.StaticDirectory = value;
        }

        public bool SaveAndLaunchToPlayer
        {
            get => App.Settings.Prop.SaveAndLaunchToPlayer;
            set => App.Settings.Prop.SaveAndLaunchToPlayer = value;
        }

        public IReadOnlyDictionary<string, ChannelChangeMode> ChannelChangeModes => new Dictionary<string, ChannelChangeMode>
        {
            { Strings.Menu_Channel_ChangeAction_Automatic, ChannelChangeMode.Automatic },
            { Strings.Menu_Channel_ChangeAction_Prompt, ChannelChangeMode.Prompt },
            { Strings.Menu_Channel_ChangeAction_Ignore, ChannelChangeMode.Ignore },
        };

        public string SelectedChannelChangeMode
        {
            get => ChannelChangeModes.FirstOrDefault(x => x.Value == App.Settings.Prop.ChannelChangeMode).Key;
            set => App.Settings.Prop.ChannelChangeMode = ChannelChangeModes[value];
        }

        public bool ForceRobloxReinstallation
        {
            get => App.State.Prop.ForceReinstall || IsRobloxInstallationMissing;
            set => App.State.Prop.ForceReinstall = value;
        }

        public ICommand BackupSettingsCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(BackupSettings);
        public ICommand RestoreSettingsCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(RestoreSettings);
        public ICommand OpenScreenshotsCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(OpenScreenshots);
        public ICommand OpenRecordingsCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(OpenRecordings);

        private void BackupSettings()
        {
            try
            {
                string backupsDir = Path.Combine(Paths.Base, "Backups");
                if (!Directory.Exists(backupsDir))
                    Directory.CreateDirectory(backupsDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupZip = Path.Combine(backupsDir, $"Astralstrap_Backup_{timestamp}.zip");

                using var memStream = new MemoryStream();
                using var zipStream = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream(memStream);

                var files = new List<string>
                {
                    App.Settings.FileLocation,
                    App.State.FileLocation,
                    App.FastFlags.FileLocation
                };

                foreach (var file in files)
                {
                    if (!File.Exists(file)) continue;

                    var entry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(Path.GetFileName(file)) { DateTime = DateTime.Now };
                    zipStream.PutNextEntry(entry);

                    using var fs = File.OpenRead(file);
                    fs.CopyTo(zipStream);
                }

                zipStream.CloseEntry();
                zipStream.Finish();
                memStream.Position = 0;

                using var outputStream = File.OpenWrite(backupZip);
                memStream.CopyTo(outputStream);

                Frontend.ShowMessageBox($"Backup successfully saved to:\n{backupZip}", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to create backup:\n{ex.Message}", MessageBoxImage.Error);
            }
        }

        private void RestoreSettings()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Zip Archive (*.zip)|*.zip|All Files (*.*)|*.*",
                    InitialDirectory = Path.Combine(Paths.Base, "Backups")
                };

                if (dialog.ShowDialog() != true) return;

                new ICSharpCode.SharpZipLib.Zip.FastZip().ExtractZip(dialog.FileName, Paths.Base, null);

                Frontend.ShowMessageBox("Settings successfully restored from backup! Please restart Astralstrap to apply changes.", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to restore backup:\n{ex.Message}", MessageBoxImage.Error);
            }
        }

        private void OpenScreenshots()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Roblox");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to open Screenshots folder:\n{ex.Message}", MessageBoxImage.Error);
            }
        }

        private void OpenRecordings()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Roblox");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to open Recordings folder:\n{ex.Message}", MessageBoxImage.Error);
            }
        }
    }
}