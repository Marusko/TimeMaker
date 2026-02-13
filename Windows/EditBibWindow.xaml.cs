using System.Windows;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for EditBibWindow.xaml
    /// </summary>
    public partial class EditBibWindow
    {
        private string _bib;
        public EditBibWindow(string bib)
        {
            InitializeComponent();
            _bib = bib;
            BibText.Text = _bib;
        }

        public string GetBib()
        {
            return _bib;
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            _bib = BibText.Text;
            Close();
        }
    }
}
