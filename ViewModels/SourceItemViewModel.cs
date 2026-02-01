using System.ComponentModel;

namespace TimeMaker.ViewModels
{
    public class SourceItemViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }

        private string _progress;
        public string Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(IsNotRunning));
                }
            }
        }

        public bool IsNotRunning => !IsRunning;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateProgress(string progress)
        {
            Progress = progress;
        }
    }
}
