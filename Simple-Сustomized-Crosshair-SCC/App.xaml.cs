using System.Windows;

namespace Simple_Customized_Crosshair_SCC
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Глобальная обработка исключений
            this.DispatcherUnhandledException += (s, ex) =>
            {
                System.Windows.MessageBox.Show($"An unexpected error occurred:\n{ex.Exception.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                ex.Handled = true;
            };
        }
    }
}