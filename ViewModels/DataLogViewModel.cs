using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using TimeMaker.Models;

namespace TimeMaker.ViewModels
{
    public class DataLogViewModel : INotifyPropertyChanged
    {
        public string Id { get; init; } = string.Empty;
        public string Raw { get; init; } = string.Empty;
        public string Bib { get; init; } = string.Empty;
        public TimeOnly Time { get; init; }
        public ApiTimingPoint TimingPoint { get; init; } = new();
        public string StatusCode { get; private set; } = string.Empty;

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
                    OnPropertyChanged(nameof(ItemBackground));
                    OnPropertyChanged(nameof(RetryVisibility));
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
                    OnPropertyChanged(nameof(ClearLabel));
                    OnPropertyChanged(nameof(ItemBackground));
                    OnPropertyChanged(nameof(RetryVisibility));
                }
            }
        }

        private bool _isQuestion;
        public bool IsQuestion
        {
            get => _isQuestion;
            set
            {
                if (_isQuestion != value)
                {
                    _isQuestion = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ItemFontWeight));
                }
            }
        }

        public string ItemBackground => (Status, IsClear) switch
        {
            (UploadStatus.Completed, false) => "LightGreen",
            (UploadStatus.Completed, true) => "LightBlue",
            (UploadStatus.Failed, _) => "LightCoral",
            (UploadStatus.Ignored, _) => "LightGray",
            _ => "White"  // Default/Pending
        };

        public FontWeight ItemFontWeight => IsQuestion ? FontWeights.Bold : FontWeights.Normal;

        public Visibility ErrorVisibility => Status == UploadStatus.Failed ? Visibility.Visible : Visibility.Collapsed;

        public Visibility RetryVisibility => (Status, IsClear, App.RaceResult.ClearEnabled) switch
        {
            (UploadStatus.Failed,false ,_) => Visibility.Visible,
            (UploadStatus.Failed, true, true) => Visibility.Visible,
            _ => Visibility.Collapsed
        };

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
