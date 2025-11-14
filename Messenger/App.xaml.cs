using System.Windows;

namespace messenger
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Создаём и показываем сразу главное окно
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.MainWindow = mainWindow;
        }
    }
}
