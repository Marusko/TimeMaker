using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TimeMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnAddFile_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnAddTimy_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnRaceSettings_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void StartSource(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void StopSource(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ViewRawSource(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OpenJson(object sender, RoutedEventArgs e)
        {
            const string url = "https://www.newtonsoft.com/json";
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

        private void OpenIo(object sender, RoutedEventArgs e)
        {
            const string url = "https://www.nuget.org/packages/system.io.ports/";
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}