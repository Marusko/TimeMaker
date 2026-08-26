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
        private List<TimeDefinitionPartModel> _definition = new();
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

        private void SourceKindChecked(object sender, RoutedEventArgs e)
        {
            // Checked fires before the panels exist during InitializeComponent.
            if (FilePanel == null || DefinitionPanel == null)
            {
                return;
            }
            var isFile = FileRadio.IsChecked ?? false;
            FilePanel.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;
            DefinitionPanel.Visibility = isFile ? Visibility.Collapsed : Visibility.Visible;
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
                if (res == null || string.IsNullOrEmpty(op.FileName))
                {
                    ThemedDialog.Show("CSV súbor", "CSV súbor je potrebný", ThemedDialogIcon.Warning);
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
                ThemedDialog.Show("Chyba", $"Nastala chyba pri načítavaní súboru: {ex.Message}", ThemedDialogIcon.Error);
                App.Logger.LogError("[CF] Error loading file", ex);
                FileNameLabel.Content = "Nie je vybraný žiadny súbor";
                _path = string.Empty;
            }
        }

        private void EditDefinition(object sender, RoutedEventArgs e)
        {
            var window = new CreateTimeDefinitionWindow(_definition);
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                _definition = window.Definition;
                DefinitionLabel.Content = TimeDefinition.Summary(_definition);
            }
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            var isFile = FileRadio.IsChecked ?? false;
            var missingSource = isFile ? string.IsNullOrEmpty(_path) : _definition.Count == 0;
            if (missingSource || string.IsNullOrEmpty(TimingPointsCombo.Text) || string.IsNullOrEmpty(NameText.Text))
            {
                ThemedDialog.Show("Chyba", "Prosím zadajte všetky potrebné dáta", ThemedDialogIcon.Error);
            }
            else
            {
                var init = new FileSourceInitModel()
                {
                    Name = NameText.Text,
                    Source = isFile ? _path : string.Empty,
                    FirstTarget = new ApiTimingPoint(){ Name = TimingPointsCombo.Text },
                    Separator = _delimiter,
                    Template = TemplateCheckBox.IsChecked ?? false,
                    Definition = isFile ? new List<TimeDefinitionPartModel>() : _definition
                };
                App.SourceManager.AddSource(typeof(FileService), init);
                Close();
            }
        }
    }
}
