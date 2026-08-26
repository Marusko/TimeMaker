using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using TimeMaker.Models;
using TimeMaker.ViewModels;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for CreateTimeDefinitionWindow.xaml
    /// </summary>
    public partial class CreateTimeDefinitionWindow
    {
        /// <summary>How many generated times the preview lists before it is cut off.</summary>
        private const int PreviewLimit = 2000;

        private readonly ObservableCollection<TimeDefinitionPartViewModel> _parts = new();
        private readonly ObservableCollection<TimePreviewItemViewModel> _preview = new();
        private bool _refreshing;

        /// <summary>The definition as saved. Only meaningful when DialogResult is true.</summary>
        public List<TimeDefinitionPartModel> Definition { get; private set; } = new();

        public CreateTimeDefinitionWindow(IEnumerable<TimeDefinitionPartModel> definition)
        {
            InitializeComponent();
            PartsItems.ItemsSource = _parts;
            PreviewList.ItemsSource = _preview;

            foreach (var part in definition)
            {
                Attach(TimeDefinitionPartViewModel.FromModel(part));
            }
            if (_parts.Count == 0)
            {
                Attach(CreateNextPart());
            }
            Renumber();
            RefreshPreview();
        }

        private void Attach(TimeDefinitionPartViewModel part)
        {
            part.PropertyChanged += OnPartChanged;
            _parts.Add(part);
        }

        private void OnPartChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Header, Error and the ErrorVisibility that follows it are set by
            // this window itself; reacting to them would recurse through
            // RefreshPreview.
            if (e.PropertyName is nameof(TimeDefinitionPartViewModel.Header)
                or nameof(TimeDefinitionPartViewModel.Error)
                or nameof(TimeDefinitionPartViewModel.ErrorVisibility))
            {
                return;
            }
            RefreshPreview();
        }

        /// <summary>
        /// A new part continues where the last valid one ended, so adding a category
        /// with a longer gap only means correcting its first time.
        /// </summary>
        private TimeDefinitionPartViewModel CreateNextPart()
        {
            var previous = _parts.LastOrDefault();
            if (previous != null && previous.TryBuild(out var model))
            {
                var waves = (model.EntryCount + model.WaveSize - 1) / model.WaveSize;
                var next = model.FirstTime.Add(model.WaveInterval * waves);
                return new TimeDefinitionPartViewModel()
                {
                    FirstTime = next.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                    FirstBib = (model.LastBib + 1).ToString(CultureInfo.InvariantCulture),
                    LastBib = string.Empty,
                    WaveSize = model.WaveSize.ToString(CultureInfo.InvariantCulture),
                    WaveInterval = TimeDefinitionPartModel.IntervalToString(model.WaveInterval)
                };
            }

            return new TimeDefinitionPartViewModel()
            {
                FirstTime = "10:00:00",
                FirstBib = string.Empty,
                LastBib = string.Empty,
                WaveSize = "1",
                WaveInterval = "00:10"
            };
        }

        private void Renumber()
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                _parts[i].Header = $"Časť {i + 1}";
            }
        }

        private void AddPart(object sender, RoutedEventArgs e)
        {
            Attach(CreateNextPart());
            Renumber();
            RefreshPreview();
        }

        private void RemovePart(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink { DataContext: TimeDefinitionPartViewModel part })
            {
                part.PropertyChanged -= OnPartChanged;
                _parts.Remove(part);
                Renumber();
                RefreshPreview();
            }
        }

        private void RefreshPreview()
        {
            // TryBuild below writes each part's Error, and any binding driven by
            // that notification can call back in here mid-loop. A re-entrant pass
            // would rebuild the list this one is still appending to, leaving the
            // remaining parts listed twice.
            if (_refreshing)
            {
                return;
            }
            _refreshing = true;
            try
            {
                BuildPreview();
            }
            finally
            {
                _refreshing = false;
            }
        }

        private void BuildPreview()
        {
            _preview.Clear();

            var valid = _parts.Count > 0;
            var seen = new HashSet<string>();
            var duplicates = 0;
            var total = 0;
            var truncated = false;
            TimeDefinitionEntry? first = null;
            TimeDefinitionEntry? last = null;

            for (var i = 0; i < _parts.Count; i++)
            {
                if (!_parts[i].TryBuild(out var model))
                {
                    valid = false;
                    continue;
                }

                foreach (var entry in model.Generate())
                {
                    total++;
                    first ??= entry;
                    last = entry;
                    var duplicate = !seen.Add(entry.Bib);
                    if (duplicate)
                    {
                        duplicates++;
                    }
                    if (_preview.Count < PreviewLimit)
                    {
                        _preview.Add(new TimePreviewItemViewModel()
                        {
                            Part = (i + 1).ToString(CultureInfo.InvariantCulture),
                            Bib = entry.Bib,
                            Time = entry.TimeText,
                            IsDuplicate = duplicate
                        });
                    }
                    else
                    {
                        truncated = true;
                    }
                }
            }

            SaveButton.IsEnabled = valid;
            PreviewSummary.Text = BuildSummary(valid, total, duplicates, truncated, first, last);
        }

        private string BuildSummary(bool valid, int total, int duplicates, bool truncated,
            TimeDefinitionEntry? first, TimeDefinitionEntry? last)
        {
            if (_parts.Count == 0)
            {
                return "Pridajte aspoň jednu časť.";
            }

            var lines = new List<string>();
            if (total > 0 && first.HasValue && last.HasValue)
            {
                lines.Add($"{TimeDefinition.Times(total)} · prvý {first.Value.TimeText} · posledný {last.Value.TimeText}");
            }
            else
            {
                lines.Add("Zatiaľ nie je vygenerovaný žiadny čas.");
            }
            if (!valid)
            {
                lines.Add("Časti s chybou nie sú v náhľade zahrnuté.");
            }
            if (duplicates > 0)
            {
                lines.Add($"Pozor: {TimeDefinition.Plural(duplicates, "číslo sa opakuje", "čísla sa opakujú", "čísel sa opakuje")} - použije sa prvý výskyt.");
            }
            if (truncated)
            {
                lines.Add($"Zobrazených je prvých {PreviewLimit} časov.");
            }
            return string.Join(Environment.NewLine, lines);
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            var models = new List<TimeDefinitionPartModel>(_parts.Count);
            foreach (var part in _parts)
            {
                if (!part.TryBuild(out var model))
                {
                    RefreshPreview();
                    ThemedDialog.Show("Chyba", $"{part.Header}: {part.Error}", ThemedDialogIcon.Error);
                    return;
                }
                models.Add(model);
            }

            if (models.Count == 0)
            {
                ThemedDialog.Show("Chyba", "Pridajte aspoň jednu časť definície", ThemedDialogIcon.Error);
                return;
            }

            Definition = models;
            DialogResult = true;
            Close();
        }
    }
}
