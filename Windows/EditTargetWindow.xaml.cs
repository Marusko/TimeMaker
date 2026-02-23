using System.Windows;
using TimeMaker.Models;
using TimeMaker.Services;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for EditTargetWindow.xaml
    /// </summary>
    public partial class EditTargetWindow
    {
        private SourceService _sourceService;
        public EditTargetWindow(SourceService sourceService)
        {
            InitializeComponent();
            _sourceService = sourceService;
            Title = _sourceService.Name;
            Init();
        }

        private void Init()
        {
            StartPoint.Items.Clear();
            foreach (var p in App.RaceResult.Points)
            {
                StartPoint.Items.Add(p.Name);
            }
            if (_sourceService is FileService fileService)
            {
                FirstTargetLabel.Content = "Merací bod:";
                FinishPoint.Visibility = Visibility.Collapsed;
                SecondTargetLabel.Visibility = Visibility.Collapsed;
                RunTimePoint.Visibility = Visibility.Collapsed;
                ThirdTargetLabel.Visibility = Visibility.Collapsed;
                StartPoint.SelectedItem = fileService.Target;
                Height = 165;
            }
            else if (_sourceService is SerialPortService serialService)
            {
                FinishPoint.Items.Clear();
                foreach (var p in App.RaceResult.Points)
                {
                    FinishPoint.Items.Add(p.Name);
                }
                RunTimePoint.Items.Clear();
                foreach (var p in App.RaceResult.Points)
                {
                    RunTimePoint.Items.Add(p.Name);
                }
                StartPoint.SelectedItem = serialService.Target;
                FinishPoint.SelectedItem = serialService.TargetFinish;
                if (serialService.Mode == TimyMode.Stopwatch)
                {
                    RunTimePoint.SelectedItem = serialService.TargetRunTime;
                    RunTimePoint.IsEnabled = true;
                }
            }
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if (_sourceService is FileService fileService)
            {
                fileService.ChangeTarget(new FileTargetChangeModel() { Target = StartPoint.Text });
            }
            else if (_sourceService is SerialPortService serialService)
            {
                serialService.ChangeTarget(new SerialTargetChangeModel() { Target = StartPoint.Text, SecondTarget = FinishPoint.Text, ThirdTarget = RunTimePoint.Text });
            }

            Close();
        }
    }
}
