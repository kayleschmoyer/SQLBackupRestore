using System.Windows;
using System.Windows.Controls;
using SQLBackupRestore.ViewModels;

namespace SQLBackupRestore
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        /// <summary>
        /// Handles password changes to update the view model.
        /// </summary>
        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && DataContext is MainViewModel viewModel)
            {
                viewModel.Password = passwordBox.Password;
            }
        }
    }
}
