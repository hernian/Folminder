using Folminder.ViewModels;
using Folminder.Models;
using System.Configuration;
using System.Data;
using System.Windows;
using Folminder.Platform;

namespace Folminder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow();
            var windowMetricsService = (IWindowMetricsService)mainWindow;
            var folderList = new FolderList();
            var viewModel = new MainWindowViewModel(folderList, windowMetricsService);
            mainWindow.SetViewModel(viewModel);
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.ShowInTaskbar = false;
            mainWindow.Show(); // SourceInitializedを発火させるために一度Show()を呼ぶ
            mainWindow.Hide();

            this.MainWindow = mainWindow;
        }
    }

}
