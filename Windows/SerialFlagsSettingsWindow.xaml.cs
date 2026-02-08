using System.Windows;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for SerialFlagsSettingsWindow.xaml
    /// </summary>
    public partial class SerialFlagsSettingsWindow
    {
        private readonly char _correctChar;

        public SerialFlagsSettingsWindow(bool isBackup)
        {
            InitializeComponent();
            if (isBackup)
            {
                BoxCorrect.IsChecked = true;
                BoxCorrect.Content = "* - platný";
                BoxQuestion.IsChecked = false;
                BoxQuestion.IsEnabled = false;
                BoxSmallC.IsChecked = true;
                BoxBigC.IsChecked = true;
                BoxInfo.IsChecked = false;
                BoxInfo.IsEnabled = false;
                BoxMemo.IsChecked = false;
                BoxMemo.IsEnabled = false;
                _correctChar = '*';
            }
            else
            {
                BoxCorrect.IsChecked = true;
                BoxCorrect.Content = "medzera - platný";
                BoxQuestion.IsChecked = true;
                BoxSmallC.IsChecked = true;
                BoxBigC.IsChecked = true;
                BoxInfo.IsChecked = true;
                BoxMemo.IsChecked = false;
                _correctChar = ' ';
            }
        }

        public List<char> GetFlags()
        {
            var flags = new List<char>();
                        
            if (BoxCorrect.IsChecked != null && (bool)BoxCorrect.IsChecked) flags.Add(_correctChar);
            if (BoxQuestion.IsChecked != null && (bool)BoxQuestion.IsChecked) flags.Add('?');
            if (BoxSmallC.IsChecked != null && (bool)BoxSmallC.IsChecked) flags.Add('c');
            if (BoxBigC.IsChecked != null && (bool)BoxBigC.IsChecked) flags.Add('C');
            if (BoxInfo.IsChecked != null && (bool)BoxInfo.IsChecked) flags.Add('i');
            if (BoxMemo.IsChecked != null && (bool)BoxMemo.IsChecked) flags.Add('m');

            return flags;
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
