using Folminder.Models;
using Folminder.Services;
using Folminder.ViewModels;
using System.Windows;
using System.Windows.Interop;

namespace Folminder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var folderList = new FolderList();
            var hotKeyService = new HotKeyService();
            var viewModel = new MainWindowViewModel(folderList, hotKeyService);
            var mainWindow = new MainWindow(viewModel, hotKeyService);

            // EnsureHandleでmainWindowのSourceInitializedイベントを発火させる
            var helper = new WindowInteropHelper(mainWindow);
            helper.EnsureHandle();

            this.MainWindow = mainWindow;
        }
    }

}
