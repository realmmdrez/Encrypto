using Encrypto.CryptoEngine;
using Encrypto.UI;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Encrypto.Views
{
    public partial class SecureDeletePage : Page
    {
        public SecureDeletePage()
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

        private void Pick_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
                FilePathTextBox.Text = dlg.FileName;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FilePathTextBox.Text))
            {
                MessageBox.Show("No file selected.");
                return;
            }

            string path = FilePathTextBox.Text;

            NotificationQueue.Enqueue(async () =>
            {
                await ShowNotificationAsync("Deleting...");

                await Task.Run(() =>
                    SecureDelete.WipeFile(path));

                await ShowNotificationAsync("Deleted");
                await HideNotificationAsync();
            });
        }
    }
}