using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Bloxstrap.UI.Elements.Base;
using Microsoft.Win32;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class CustomSoundsDialog : WpfUiWindow
    {
        private MediaPlayer _player = new();
        private string? _customFilePath = null;

        public CustomSoundsDialog()
        {
            InitializeComponent();
            CheckCurrentStatus();
        }

        private void CheckCurrentStatus()
        {
            string targetPath = Path.Combine(Paths.Modifications, "AstralstrapSounds", "content", "sounds", "ouch.ogg");
            if (File.Exists(targetPath))
            {
                var fi = new FileInfo(targetPath);
                CurrentModStatus.Text = $"Active Custom Sound Mod Installed ({fi.Length / 1024.0:F1} KB, modified {fi.LastWriteTime:yyyy-MM-dd HH:mm}).";
            }
            else
            {
                CurrentModStatus.Text = "No custom sound mod currently installed (using Roblox defaults).";
            }
        }

        private void DeathSoundSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomSoundPathText == null) return;

            if (DeathSoundSelector.SelectedIndex == 2)
            {
                CustomSoundPathText.Visibility = Visibility.Visible;
                if (string.IsNullOrEmpty(_customFilePath))
                    CustomSoundPathText.Text = "Click the folder icon to select a sound file...";
            }
            else
            {
                CustomSoundPathText.Visibility = Visibility.Collapsed;
            }
        }

        private void BrowseCustomSound_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio Files (*.ogg;*.mp3;*.wav)|*.ogg;*.mp3;*.wav|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _customFilePath = dialog.FileName;
                CustomSoundPathText.Text = _customFilePath;
                DeathSoundSelector.SelectedIndex = 2;
            }
        }

        private void PlaySound_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DeathSoundSelector.SelectedIndex == 2 && !string.IsNullOrEmpty(_customFilePath) && File.Exists(_customFilePath))
                {
                    _player.Open(new Uri(_customFilePath, UriKind.Absolute));
                    _player.Play();
                }
                else if (DeathSoundSelector.SelectedIndex == 1)
                {
                    // Classic OOF - if sound mod exists or play system beep
                    string targetPath = Path.Combine(Paths.Modifications, "AstralstrapSounds", "content", "sounds", "ouch.ogg");
                    if (File.Exists(targetPath))
                    {
                        _player.Open(new Uri(targetPath, UriKind.Absolute));
                        _player.Play();
                    }
                    else
                    {
                        System.Media.SystemSounds.Beep.Play();
                    }
                }
                else
                {
                    System.Media.SystemSounds.Asterisk.Play();
                }
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Could not preview audio: {ex.Message}", MessageBoxImage.Warning);
            }
        }

        private void ApplySoundMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string targetDir = Path.Combine(Paths.Modifications, "AstralstrapSounds", "content", "sounds");
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                string targetOuch = Path.Combine(targetDir, "ouch.ogg");

                if (DeathSoundSelector.SelectedIndex == 0)
                {
                    // Revert to default by removing custom sound
                    string modFolder = Path.Combine(Paths.Modifications, "AstralstrapSounds");
                    if (Directory.Exists(modFolder))
                        Directory.Delete(modFolder, true);

                    Frontend.ShowMessageBox("Reset death audio to Roblox default!", MessageBoxImage.Information);
                }
                else if (DeathSoundSelector.SelectedIndex == 2 && !string.IsNullOrEmpty(_customFilePath) && File.Exists(_customFilePath))
                {
                    File.Copy(_customFilePath, targetOuch, true);
                    Frontend.ShowMessageBox("Custom sound effect successfully installed to Astralstrap!", MessageBoxImage.Information);
                }
                else
                {
                    Frontend.ShowMessageBox("Sound pack applied successfully!", MessageBoxImage.Information);
                }

                CheckCurrentStatus();
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to apply sound mod: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try { _player.Close(); } catch { }
            Close();
        }
    }
}
