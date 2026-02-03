using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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
        public string StatusCode { get; set; } = string.Empty;

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
                    OnPropertyChanged(nameof(ErrorVisibility));
                }
            }
        }

        private bool _isClear;
        public bool IsClear
        {
            get => _isClear;
            set
            {
                if (_isClear != value)
                {
                    _isClear = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ClearVisibility));
                    OnPropertyChanged(nameof(ClearLabel));
                }
            }
        }

        public Visibility ClearVisibility => IsClear ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ErrorVisibility => Status == UploadStatus.Failed ? Visibility.Visible : Visibility.Collapsed;
        public string ClearLabel => IsClear ? "Áno" : "";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ChangeStatus(UploadStatus status, string statusCode)
        {
            StatusCode = statusCode;
            Status = status;
        }
    }
}
