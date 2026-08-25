using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using TimeMaker.Models;
using TimeMaker.Services;

namespace TimeMaker.ViewModels
{
    public class DataLogViewModel : INotifyPropertyChanged
    {
        public string Id { get; init; } = string.Empty;
        public string Raw { get; init; } = string.Empty;
        public string Bib { get; set; } = string.Empty;
        public TimeOnly Time { get; init; }
        public ApiTimingPoint TimingPoint { get; init; } = new();
        public string StatusCode { get; private set; } = string.Empty;
        public ObservableCollection<string> BibChanges { get; init; } = new();

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
                    OnPropertyChanged(nameof(SeparatorVisibility));
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
                    OnPropertyChanged(nameof(FlagsLabel));
                    OnPropertyChanged(nameof(ItemBackground));
                    OnPropertyChanged(nameof(RetryVisibility));
                    OnPropertyChanged(nameof(SeparatorVisibility));
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
                    OnPropertyChanged(nameof(FlagsLabel));
                }
            }
        }

        public string ItemBackground => (Status, IsClear) switch
        {
            (UploadStatus.Completed, false) => "LightGreen",
            (UploadStatus.Completed, true) => "LightBlue",
            (UploadStatus.Failed, _) => "LightSalmon",
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

        public Visibility SeparatorVisibility => (ErrorVisibility == Visibility.Visible || RetryVisibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ChangesVisibility => BibChanges.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public string FlagsLabel => (IsClear ? "C" : "") + (BibChanges.Count > 0 ? "B" : "") + (IsQuestion ? "Q" : "");

        private const string CopyTimeFormat = "HH:mm:ss.ffff";

        // Ignored impulses are kept as raw text only - the bib and the time are
        // dug out of it so they can still be copied.
        private static readonly Regex RawTimeRegex = new(@"\d{1,2}:\d{2}:\d{2}(?:[.,]\d{1,4})?", RegexOptions.Compiled);
        // A bib is a whole token of digits, optionally prefixed by impulse flags.
        // Token boundaries are whitespace or a CSV separator, so raw rows of a
        // file source can be read the same way as Timy impulses.
        private static readonly Regex RawBibRegex = new(@"(?<=^|[\s;,|])[*?cCi]*(?<bib>\d+)(?=$|[\s;,|])", RegexOptions.Compiled);
        private static readonly Regex RawTerminatorRegex = new(@"\s00\s*$", RegexOptions.Compiled);

        /// <summary>
        /// Bib to put on the clipboard, falling back to the first number of the
        /// raw impulse. Empty when there is nothing to copy.
        /// </summary>
        public string GetBibToCopy()
        {
            if (!string.IsNullOrEmpty(Bib))
            {
                return Bib;
            }

            // Drop the time and the trailing "00" terminator first, otherwise
            // their digits get picked up instead of the bib.
            var stripped = RawTerminatorRegex.Replace(RawTimeRegex.Replace(Raw, " "), " ");
            var match = RawBibRegex.Match(stripped);
            return match.Success && int.TryParse(match.Groups["bib"].Value, out var bib) ? $"{bib}" : string.Empty;
        }

        /// <summary>
        /// Time to put on the clipboard, falling back to the first time found in
        /// the raw impulse. Empty when there is nothing to copy.
        /// </summary>
        public string GetTimeToCopy()
        {
            if (Time != TimeOnly.MinValue)
            {
                return Time.ToString(CopyTimeFormat);
            }

            var match = RawTimeRegex.Match(Raw);
            if (match.Success && SourceService.TryParseTime(match.Value.Replace(',', '.'), out var time))
            {
                return time.ToString(CopyTimeFormat);
            }
            return string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public DataLogViewModel()
        {
            BibChanges.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(BibChanges));
                OnPropertyChanged(nameof(ChangesVisibility));
                OnPropertyChanged(nameof(FlagsLabel));
            };
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ChangeStatus(UploadStatus status, string statusCode)
        {
            StatusCode = statusCode;
            Status = status;
        }

        public DataModel ToDataModel(string sourceId)
        {
            return new DataModel()
            {
                Id = Id,
                SourceId = sourceId,
                Bib = Bib,
                Time = Time,
                TimingPoint = TimingPoint,
                RawData = Raw,
                IsClear = IsClear
            };
        }
    }
}
