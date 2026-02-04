using Encrypto.CryptoEngine;
using Encrypto.UI;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Encrypto.Views
{
    public partial class KeyGenPage : Page
    {
        public KeyGenPage()
        {
            InitializeComponent();
        }

        private async Task ShowNotificationAsync(string text)
        {
            StatusText.Text = text;

            StatusTransform.BeginAnimation(
                TranslateTransform.XProperty,
                new DoubleAnimation(-320, 0, TimeSpan.FromMilliseconds(450)));

            StatusPill.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));

            await Task.Delay(1800);
        }

        private async Task HideNotificationAsync()
        {
            StatusTransform.BeginAnimation(
                TranslateTransform.XProperty,
                new DoubleAnimation(0, -320, TimeSpan.FromMilliseconds(450)));

            StatusPill.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)));

            await Task.Delay(500);
        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Password required.");
                return;
            }

            string password = PasswordBox.Password;

            NotificationQueue.Enqueue(async () =>
            {
                await ShowNotificationAsync("Generating keys...");

                await Task.Run(() =>
                    RsaKeyManager.GenerateKeys(password));

                await ShowNotificationAsync("Keys generated");
                await HideNotificationAsync();
            });
        }
    }
}