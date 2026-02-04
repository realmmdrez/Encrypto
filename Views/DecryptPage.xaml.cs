using Encrypto.CryptoEngine;
using Encrypto.UI;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Encrypto.Views
{
    public partial class DecryptPage : Page
    {
        public DecryptPage()
        {
            InitializeComponent();
        }

        private async Task ShowNotificationAsync(string text)
        {
            StatusText.Text = text;

            StatusTransform.BeginAnimation(
                TranslateTransform.XProperty,
                new DoubleAnimation(-320, 0, TimeSpan.FromMilliseconds(450))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

            StatusPill.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));

            await Task.Delay(1800);
        }

        private async Task HideNotificationAsync()
        {
            StatusTransform.BeginAnimation(
                TranslateTransform.XProperty,
                new DoubleAnimation(0, -320, TimeSpan.FromMilliseconds(450))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                });

            StatusPill.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)));

            await Task.Delay(500);
        }

        private void BrowseInput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "*.mmdrez|*.mmdrez" };
            if (dlg.ShowDialog() == true)
                InputFileTextBox.Text = dlg.FileName;
        }

        private async void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputFileTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Missing input.");
                return;
            }

            string input = InputFileTextBox.Text;
            string password = PasswordBox.Password;

            NotificationQueue.Enqueue(async () =>
            {
                await ShowNotificationAsync("Decrypting...");

                await Task.Run(() =>
                {
                    var pem = RsaKeyManager.DecryptPrivateKey(password);
                    HybridCrypto.DecryptFile(input, pem);
                });

                await ShowNotificationAsync("Decrypted");
                await HideNotificationAsync();
            });
        }
    }
}