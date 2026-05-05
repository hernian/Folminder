using Folminder.ViewModels;
using System.Windows;

namespace Folminder
{
    /// <summary>
    /// ConfigDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigDialog : Window
    {
        private readonly ConfigDialogViewModel _viewModel;

        public ConfigDialog(ConfigDialogViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
