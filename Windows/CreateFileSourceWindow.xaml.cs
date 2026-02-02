using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TimeMaker.Models;
using TimeMaker.Services;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for CreateFileSourceWindow.xaml
    /// </summary>
    public partial class CreateFileSourceWindow
    {
        private char _delimiter = ';';
        private string _path = string.Empty;
        public CreateFileSourceWindow()
        {
            InitializeComponent();
            TemplateCheckBox.IsEnabled = App.RaceResult.TemplateEnabled;
            SetPoints();
        }

        private void DelimiterChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button)
            {
                _delimiter = button.Content.ToString()?[0] ?? ';';
            }
        }

        private void SetPoints()
        {
            TimingPointsCombo.Items.Clear();
            foreach (var p in App.RaceResult.Points)
            {
                TimingPointsCombo.Items.Add(p.Name);
            }
        }

        private void LoadFile(object sender, RoutedEventArgs e)
        {
            try
            {
                var op = new OpenFileDialog();
                op.Title = "Vyberte súbor";
                op.Filter = "CSV súbor|*.csv";
                var res = op.ShowDialog();
                if (res == null)
                {
                    MessageBox.Show("CSV súbor je potrebný", "CSV súbor", MessageBoxButton.OK, MessageBoxImage.Warning);
                    FileNameLabel.Content = "Nie je vybraný žiadny súbor";
                    return;
                }
                _path = op.FileName;
                var index = _path.LastIndexOf(Path.DirectorySeparatorChar) + 1;
                var name = _path[index..];
                FileNameLabel.Content = name;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nastala chyba pri načítavaní súboru: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                FileNameLabel.Content = "Nie je vybraný žiadny súbor";
                _path = string.Empty;
            }
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_path) || string.IsNullOrEmpty(TimingPointsCombo.Text) || string.IsNullOrEmpty(NameText.Text))
            {
                MessageBox.Show("Prosím zadajte všetky potrebné dáta", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                var init = new FileSourceInitModel()
                {
                    Name = NameText.Text,
                    Source = _path,
                    FirstTarget = new ApiTimingPoint(){ Name = TimingPointsCombo.Text },
                    Separator = _delimiter,
                    Template = TemplateCheckBox.IsChecked ?? false
                };
                App.SourceManager.AddSource(typeof(FileService), init);
                Close();
            }
        }
    }
}
