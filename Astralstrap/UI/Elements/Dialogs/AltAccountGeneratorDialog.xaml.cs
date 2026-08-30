using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Bloxstrap.Integrations;
using Bloxstrap.UI.Elements.Base;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class AltAccountGeneratorDialog : WpfUiWindow
    {
        private static readonly string[] Prefixes = { "Astral", "Nova", "Cosmic", "Echo", "Cyber", "Void", "Star", "Shadow", "Frost", "Zenith", "Vortex", "Pulse", "Quantum", "Apex" };
        private static readonly string[] Suffixes = { "Runner", "Rider", "Striker", "Knight", "Vortex", "Walker", "Blaze", "Pilot", "Drifter", "Hunter", "Ghost", "Specter" };

        public AltAccountGeneratorDialog()
        {
            InitializeComponent();
            GenerateAll();
        }

        private void GenerateAll()
        {
            GeneratedUsernameBox.Text = GenerateUsername();
            GeneratedPasswordBox.Text = GeneratePassword(16);
            GeneratedBirthdayBox.Text = GenerateBirthday();
        }

        private string GenerateUsername()
        {
            var rng = new Random();
            string p = Prefixes[rng.Next(Prefixes.Length)];
            string s = Suffixes[rng.Next(Suffixes.Length)];
            int num = rng.Next(10, 999);
            return $"{p}_{s}{num}";
        }

        private string GeneratePassword(int length)
        {
            const string validChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%^&*";
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append(validChars[b % validChars.Length]);
            }
            return sb.ToString();
        }

        private string GenerateBirthday()
        {
            var rng = new Random();
            int year = rng.Next(2000, 2006);
            int month = rng.Next(1, 13);
            int day = rng.Next(1, 29);
            var dt = new DateTime(year, month, day);
            return dt.ToString("MMMM dd, yyyy");
        }

        private void RegenerateUsername_Click(object sender, RoutedEventArgs e)
        {
            GeneratedUsernameBox.Text = GenerateUsername();
        }

        private void CopyUsername_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(GeneratedUsernameBox.Text);
            StatusLabel.Text = "Username copied to clipboard!";
        }

        private void RegeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            GeneratedPasswordBox.Text = GeneratePassword(16);
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(GeneratedPasswordBox.Text);
            StatusLabel.Text = "Password copied to clipboard!";
        }

        private void CopyBirthday_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(GeneratedBirthdayBox.Text);
            StatusLabel.Text = "Birthday copied to clipboard!";
        }

        private void OpenRobloxSignup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.roblox.com",
                    UseShellExecute = true
                });
                StatusLabel.Text = "Opened Roblox Sign-Up page in browser.";
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Could not open browser: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void CopyAllDetails_Click(object sender, RoutedEventArgs e)
        {
            string summary = $"Username: {GeneratedUsernameBox.Text}\nPassword: {GeneratedPasswordBox.Text}\nBirthday: {GeneratedBirthdayBox.Text}";
            Clipboard.SetText(summary);
            StatusLabel.Text = "All generated alt credentials copied to clipboard!";
        }

        private async void SaveAccount_Click(object sender, RoutedEventArgs e)
        {
            string cookie = SecurityCookieBox.Text.Trim();
            if (string.IsNullOrEmpty(cookie))
            {
                Frontend.ShowMessageBox("Please paste a valid .ROBLOSECURITY cookie.", MessageBoxImage.Warning);
                return;
            }

            if (cookie.StartsWith("_|WARNING:-"))
            {
                // valid format
            }

            try
            {
                StatusLabel.Text = "Validating token and fetching profile...";
                var account = await AccountManager.Shared.ValidateAndAddCookieAsync(cookie);
                if (account != null)
                {
                    Frontend.ShowMessageBox($"Successfully saved alt account '{account.DisplayName}' (@{account.Username}) to Astralstrap!", MessageBoxImage.Information);
                    Close();
                }
                else
                {
                    Frontend.ShowMessageBox("Failed to validate cookie. Please ensure the cookie is valid and active.", MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Error saving account: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
