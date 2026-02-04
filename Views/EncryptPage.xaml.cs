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
    public partial class EncryptPage : Page
    {
        public EncryptPage()
        {
            InitializeComponent();
        }

        // ================= STATUS =================

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

        // ================= UI =================

        private void BrowseInput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
                InputFileTextBox.Text = dlg.FileName;
        }

        private void BrowsePublicKey_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "PEM (*.pem)|*.pem" };
            if (dlg.ShowDialog() == true)
                PublicKeyTextBox.Text = dlg.FileName;
        }

        private async void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputFileTextBox.Text) ||
                string.IsNullOrWhiteSpace(PublicKeyTextBox.Text))
            {
                MessageBox.Show("Missing input.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string input = InputFileTextBox.Text;
            string pub = PublicKeyTextBox.Text;

            NotificationQueue.Enqueue(async () =>
            {
                await ShowNotificationAsync("Encrypting...");

                await Task.Run(() =>
                    HybridCrypto.EncryptFile(input, pub));

                await ShowNotificationAsync("Encrypted");
                await HideNotificationAsync();
            });
        }
    }
}