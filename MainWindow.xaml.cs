using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Encrypto.Views;

namespace Encrypto
{
    public partial class MainWindow : Window
    {
        private readonly Page _encryptPage;
        private readonly Page _decryptPage;
        private readonly Page _keyGenPage;
        private readonly Page _secureDeletePage;

        private Page? _currentPage;
        private bool _allowClose;

        public MainWindow()
        {
            InitializeComponent();

            _encryptPage = new EncryptPage();
            _decryptPage = new DecryptPage();
            _keyGenPage = new KeyGenPage();
            _secureDeletePage = new SecureDeletePage();

            Navigate(_encryptPage);
            SetActiveButtonByIndex(0);

            Closing += OnWindowClosing;
        }

        // ================= WINDOW BEHAVIOR =================

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                return;

            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptExit();
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            if (_allowClose)
                return;

            e.Cancel = true;
            AttemptExit();
        }

        private void AttemptExit()
        {
            var dialog = new ExitConfirmDialog { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _allowClose = true;
                Close();
            }
        }

        // ================= NAVIGATION =================

        private void Navigate(Page targetPage)
        {
            if (_currentPage == targetPage)
                return;

            _currentPage = targetPage;
            MainFrame.Navigate(targetPage);
        }

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
                if (child is Button btn)
                    btn.Tag = "inactive";

            activeButton.Tag = "active";
        }

        private void SetActiveButtonByIndex(int index)
        {
            if (NavigationPanel.Children[index] is Button btn)
                btn.Tag = "active";
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptExit();
        }
    }
}