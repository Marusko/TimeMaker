namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for ExportProgressWindow.xaml
    /// </summary>
    public partial class ExportProgressWindow
    {
        public ExportProgressWindow()
        {
            InitializeComponent();
        }

        public void Report(int value)
        {
            ProgressBar.Value = value;
        }
    }
}
