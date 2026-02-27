using System.Windows;
using System.Windows.Controls;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for ExportSeparatorWindow.xaml
    /// </summary>
    public partial class ExportSeparatorWindow
    {
        private char _delimiter = ';';
        public ExportSeparatorWindow()
        {
            InitializeComponent();
        }

        private void DelimiterChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button)
            {
                _delimiter = button.Content.ToString()?[0] ?? ';';
            }
        }

        public char GetDelimiter()
        {
            return _delimiter;
        }

        private void Export(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
