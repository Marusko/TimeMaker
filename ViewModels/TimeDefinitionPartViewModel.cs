using System.ComponentModel;
using System.Globalization;
using System.Windows;
using TimeMaker.Models;
using TimeMaker.Services;

namespace TimeMaker.ViewModels
{
    /// <summary>
    /// Editable form of one <see cref="TimeDefinitionPartModel"/>. The fields stay
    /// strings so a half-typed value does not throw while the preview refreshes.
    /// </summary>
    public class TimeDefinitionPartViewModel : INotifyPropertyChanged
    {
        private string _firstTime = string.Empty;
        public string FirstTime
        {
            get => _firstTime;
            set => Set(ref _firstTime, value, nameof(FirstTime));
        }

        private string _firstBib = string.Empty;
        public string FirstBib
        {
            get => _firstBib;
            set => Set(ref _firstBib, value, nameof(FirstBib));
        }

        private string _lastBib = string.Empty;
        public string LastBib
        {
            get => _lastBib;
            set => Set(ref _lastBib, value, nameof(LastBib));
        }

        private string _waveSize = string.Empty;
        public string WaveSize
        {
            get => _waveSize;
            set => Set(ref _waveSize, value, nameof(WaveSize));
        }

        private string _waveInterval = string.Empty;
        public string WaveInterval
        {
            get => _waveInterval;
            set => Set(ref _waveInterval, value, nameof(WaveInterval));
        }

        private string _header = string.Empty;
        public string Header
        {
            get => _header;
            set => Set(ref _header, value, nameof(Header));
        }

        private string _error = string.Empty;
        public string Error
        {
            get => _error;
            private set
            {
                if (Set(ref _error, value, nameof(Error)))
                {
                    OnPropertyChanged(nameof(ErrorVisibility));
                }
            }
        }

        public Visibility ErrorVisibility => string.IsNullOrEmpty(Error) ? Visibility.Collapsed : Visibility.Visible;

        public static TimeDefinitionPartViewModel FromModel(TimeDefinitionPartModel model)
        {
            return new TimeDefinitionPartViewModel()
            {
                FirstTime = model.FirstTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                FirstBib = model.FirstBib.ToString(CultureInfo.InvariantCulture),
                LastBib = model.LastBib.ToString(CultureInfo.InvariantCulture),
                WaveSize = model.WaveSize.ToString(CultureInfo.InvariantCulture),
                WaveInterval = TimeDefinitionPartModel.IntervalToString(model.WaveInterval)
            };
        }

        /// <summary>
        /// Validates the typed values and, when they are complete and consistent,
        /// produces the model. <see cref="Error"/> is refreshed either way.
        /// </summary>
        public bool TryBuild(out TimeDefinitionPartModel model)
        {
            model = new TimeDefinitionPartModel();

            if (!SourceService.TryParseTime(FirstTime.Trim(), out var firstTime))
            {
                Error = "Neplatný prvý čas (napr. 10:00:00)";
                return false;
            }
            if (!int.TryParse(FirstBib.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var firstBib)
                || firstBib < 0)
            {
                Error = "Neplatné prvé číslo";
                return false;
            }
            if (!int.TryParse(LastBib.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var lastBib)
                || lastBib < 0)
            {
                Error = "Neplatné posledné číslo";
                return false;
            }
            if (lastBib < firstBib)
            {
                Error = "Posledné číslo musí byť väčšie alebo rovné prvému";
                return false;
            }
            if (lastBib - firstBib + 1 > TimeDefinitionPartModel.MaxEntriesPerPart)
            {
                Error = $"Príliš veľký rozsah čísel (max {TimeDefinitionPartModel.MaxEntriesPerPart})";
                return false;
            }
            if (!int.TryParse(WaveSize.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var waveSize)
                || waveSize < 1)
            {
                Error = "Počet vo vlne musí byť aspoň 1";
                return false;
            }
            if (!TimeDefinitionPartModel.TryParseInterval(WaveInterval, out var interval))
            {
                Error = "Neplatný čas medzi vlnami (napr. 30 alebo 01:30)";
                return false;
            }

            Error = string.Empty;
            model = new TimeDefinitionPartModel()
            {
                FirstTime = firstTime,
                FirstBib = firstBib,
                LastBib = lastBib,
                WaveSize = waveSize,
                WaveInterval = interval
            };
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool Set(ref string field, string value, string propertyName)
        {
            if (field == value)
            {
                return false;
            }
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
