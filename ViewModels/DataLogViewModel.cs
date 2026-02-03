using System.ComponentModel;
using System.Runtime.CompilerServices;
using TimeMaker.Models;

namespace TimeMaker.ViewModels
{
    public class DataLogViewModel : INotifyPropertyChanged
    {
        public string Id { get; set; } = string.Empty;
        public string Raw { get; set; } = string.Empty;
        public string Bib { get; set; } = string.Empty;
        public TimeOnly Time { get; set; }
        public ApiTimingPoint TimingPoint { get; set; } = new();

        private UploadStatus _status = UploadStatus.Pending;
        public UploadStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ChangeStatus(UploadStatus status)
        {
            Status = status;
        }
    }
}
