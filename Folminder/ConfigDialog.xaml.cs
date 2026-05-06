using Folminder.ViewModels;
using System.ComponentModel;
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

            // ViewModelのDialogResultプロパティの変更を監視
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // ウィンドウが表示された後に選択項目へスクロール
            this.ContentRendered += ConfigDialog_ContentRendered;
        }

        private void ConfigDialog_ContentRendered(object? sender, EventArgs e)
        {
            // 選択項目が存在する場合、その項目へスクロール
            if (_viewModel.SelectedItem != null)
            {
                KeyListBox.ScrollIntoView(_viewModel.SelectedItem);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConfigDialogViewModel.DialogResult))
            {
                if (_viewModel.DialogResult.HasValue)
                {
                    this.DialogResult = _viewModel.DialogResult;
                    this.Close();
                }
            }
        }
    }
}
