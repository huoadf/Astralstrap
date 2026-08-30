using System.Collections.ObjectModel;
using System.Windows;
using Bloxstrap.Integrations;
using Bloxstrap.UI.Elements.Base;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public class MultiLaunchAccountItem
    {
        public AltAccount Account { get; set; } = null!;
        public string DisplayName => Account.DisplayName;
        public string Username => Account.Username;
        public bool IsSelected { get; set; } = true;
    }

    public partial class MultiLaunchDialog : WpfUiWindow
    {
        public ObservableCollection<MultiLaunchAccountItem> AccountItems { get; set; } = new();

        public MultiLaunchDialog()
        {
            InitializeComponent();
            PopulateAccounts();
            AccountsList.ItemsSource = AccountItems;
        }

        private void PopulateAccounts()
        {
            AccountItems.Clear();
            foreach (var acc in AccountManager.Shared.Accounts)
            {
                AccountItems.Add(new MultiLaunchAccountItem
                {
                    Account = acc,
                    IsSelected = true
                });
            }

            if (AccountItems.Count == 0)
            {
                StatusLabel.Text = "No saved accounts found in Alt Manager.";
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool anyUnselected = AccountItems.Any(a => !a.IsSelected);
            foreach (var item in AccountItems)
            {
                item.IsSelected = anyUnselected;
            }
            AccountsList.Items.Refresh();
        }

        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = AccountItems.Where(a => a.IsSelected).ToList();
            if (selected.Count == 0)
            {
                Frontend.ShowMessageBox("Please select at least one account to launch.", MessageBoxImage.Warning);
                return;
            }

            long.TryParse(PlaceIdBox.Text.Trim(), out long placeId);
            if (!int.TryParse(DelayBox.Text.Trim(), out int delaySec) || delaySec < 1)
                delaySec = 4;

            LaunchButton.IsEnabled = false;
            LaunchProgressBar.Visibility = Visibility.Visible;
            LaunchProgressBar.IsIndeterminate = true;

            try
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    var item = selected[i];
                    StatusLabel.Text = $"Launching ({i + 1}/{selected.Count}): {item.DisplayName} (@{item.Username})...";

                    _ = Task.Run(async () =>
                    {
                        await AccountManager.Shared.LaunchAccountAsync(item.Account, placeId);
                    });

                    if (i < selected.Count - 1)
                    {
                        for (int s = delaySec; s > 0; s--)
                        {
                            StatusLabel.Text = $"Launched {item.DisplayName}. Waiting {s}s before next instance...";
                            await Task.Delay(1000);
                        }
                    }
                }

                StatusLabel.Text = $"Successfully triggered launches for {selected.Count} account(s)!";
                Frontend.ShowMessageBox($"Multi-Instance launch triggered for {selected.Count} account(s).", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Launch error: {ex.Message}", MessageBoxImage.Error);
            }
            finally
            {
                LaunchButton.IsEnabled = true;
                LaunchProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
