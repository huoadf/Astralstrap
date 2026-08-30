using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Bloxstrap.UI.Elements.Base;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public class ModAssetItem
    {
        public string FileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string FileSizeText { get; set; } = string.Empty;
        public BitmapImage? ImageSource { get; set; }
    }

    public partial class ModPreviewDialog : WpfUiWindow
    {
        public ObservableCollection<ModAssetItem> Assets { get; set; } = new();

        public ModPreviewDialog()
        {
            InitializeComponent();
            AssetListBox.ItemsSource = Assets;
            PopulateMods();
        }

        private void PopulateMods()
        {
            ModSelector.Items.Clear();
            if (!Directory.Exists(Paths.Modifications)) return;

            var dirs = Directory.GetDirectories(Paths.Modifications);
            foreach (var dir in dirs)
            {
                ModSelector.Items.Add(Path.GetFileName(dir));
            }

            if (ModSelector.Items.Count > 0)
                ModSelector.SelectedIndex = 0;
        }

        private void ModSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModSelector.SelectedItem is not string modName) return;

            Assets.Clear();
            string modPath = Path.Combine(Paths.Modifications, modName);
            if (!Directory.Exists(modPath)) return;

            string[] validExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".ico" };
            var files = Directory.GetFiles(modPath, "*.*", SearchOption.AllDirectories)
                .Where(f => validExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(file, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();

                    Assets.Add(new ModAssetItem
                    {
                        FileName = Path.GetFileName(file),
                        RelativePath = Path.GetRelativePath(modPath, file),
                        FullPath = file,
                        FileSizeText = $"{fileInfo.Length / 1024.0:F1} KB",
                        ImageSource = bmp
                    });
                }
                catch { }
            }

            StatusText.Text = $"Found {Assets.Count} previewable image/texture assets in '{modName}'.";
        }

        private void AssetListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssetListBox.SelectedItem is ModAssetItem item)
            {
                StatusText.Text = $"{item.RelativePath} ({item.FileSizeText})";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
