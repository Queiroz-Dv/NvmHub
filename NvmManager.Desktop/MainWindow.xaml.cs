using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Wpf;
using NvmManager.Desktop.Services;
using System.Net.Http;
using System.Windows;

namespace NvmManager.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WebHostProcessManager _hostManager;
        private const string BaseUrl = "http://127.0.0.1:5123";

        public MainWindow()
        {
            InitializeComponent();

            _hostManager = new WebHostProcessManager("NvmManager.Web.exe");

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Iniciar o processo Web
                _hostManager.StartWebHost();

                // 2. Aguardar /health
                await WaitForWebHostAsync();

                // 3. Inicializar WebView2
                await WebView.EnsureCoreWebView2Async();

                // 4. Navegar para a UI Razor
                WebView.CoreWebView2.Navigate(BaseUrl);

                WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar o host web:\n{ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async Task WaitForWebHostAsync()
        {
            using var http = new HttpClient();
            for (int i = 0; i < 40; i++) // ~10 segundos
            {
                try
                {
                    var resp = await http.GetAsync($"{BaseUrl}/health");
                    if (resp.IsSuccessStatusCode)
                        return;
                }
                catch { }

                await Task.Delay(250);
            }

            throw new Exception("O host web não respondeu ao /health.");
        }

        private void MainWindow_Closed(object? sender, System.EventArgs e)
        {
            _hostManager.StopWebHost();
        }       
    }
}