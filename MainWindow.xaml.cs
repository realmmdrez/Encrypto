using System.Windows;
using System.Windows.Controls;
using Encrypto.Views;

namespace Encrypto
{
    public partial class MainWindow : Window
    {
        // Pages (created once, reused)
        private readonly Page _encryptPage;
        private readonly Page _decryptPage;
        private readonly Page _keyGenPage;
        private readonly Page _secureDeletePage;

        private Page? _currentPage;

        public MainWindow()
        {
            InitializeComponent();

            _encryptPage = new EncryptPage();
            _decryptPage = new DecryptPage();
            _keyGenPage = new KeyGenPage();
            _secureDeletePage = new SecureDeletePage();

            Navigate(_encryptPage);

            SetActiveButtonByIndex(0);
            Loaded += (_, _) => ForceNavVisualRefresh();
        }

        // ================= CORE NAVIGATION =================

        private void Navigate(Page targetPage)
        {
            if (_currentPage == targetPage)
                return;

            _currentPage = targetPage;
            MainFrame.Navigate(targetPage);
        }

        // ================= BUTTON HANDLERS =================

        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            Navigate(_encryptPage);
            SetActive((Button)sender);
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            Navigate(_decryptPage);
            SetActive((Button)sender);
        }

        private void KeyGenButton_Click(object sender, RoutedEventArgs e)
        {
            Navigate(_keyGenPage);
            SetActive((Button)sender);
        }

        private void SecureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Navigate(_secureDeletePage);
            SetActive((Button)sender);
        }

        // ================= ACTIVE STATE =================

        private void SetActive(Button activeButton)
        {
            foreach (UIElement child in NavigationPanel.Children)
            {
                if (child is Button btn)
                    btn.Tag = "inactive";
            }

            activeButton.Tag = "active";
        }

        private void SetActiveButtonByIndex(int index)
        {
            if (NavigationPanel.Children[index] is Button btn)
                btn.Tag = "active";
        }

        // ================= EXIT =================

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Encrypto.Views.ExitConfirmDialog
            {
                Owner = this
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                Application.Current.Shutdown();
            }
        }

        // ================= VISUAL FIX =================

        private void ForceNavVisualRefresh()
        {
            foreach (UIElement child in NavigationPanel.Children)
            {
                if (child is Button btn)
                    btn.InvalidateVisual();
            }
        }
    }
}