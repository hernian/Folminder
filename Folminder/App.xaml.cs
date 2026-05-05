using Folminder.ViewModels;
using Folminder.Models;
using System.Configuration;
using System.Data;
using System.Windows;
using Folminder.Platform;
using System.Windows.Interop;
using System.Diagnostics;

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
            var viewModel = new MainWindowViewModel(folderList);
            var mainWindow = new MainWindow(viewModel);
            var helper = new WindowInteropHelper(mainWindow);
            Debug.WriteLine("before EnsureHandle");
            helper.EnsureHandle();
            Debug.WriteLine("after EnsureHandle");

            this.MainWindow = mainWindow;
        }
    }

}
