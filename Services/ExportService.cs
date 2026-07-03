using System.IO;
using System.Text;

namespace TimeMaker.Services
{
    public class ExportService
    {
        private static string Escape(string value, char separator)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            bool mustQuote =
                value.Contains(separator) ||
                value.Contains('"') ||
                value.Contains('\n') ||
                value.Contains('\r');

            if (value.Contains('"'))
                value = value.Replace("\"", "\"\"");

            return mustQuote ? $"\"{value}\"" : value;
        }

        public static async Task ExportAllAsync(string filePath, string sourceId, char separator, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            var source = App.SourceManager.GetSource(sourceId);
            if (source == null)
            {
                App.Logger.LogWarning($"[ES] Export failed: Source not found ({sourceId})");
                return;
            }

            // Snapshot on the calling (UI) thread - the live ObservableCollection
            // may be appended to by a running source during the export.
            var rows = source.LogData.ToList();
            var total = rows.Count;
            int processed = 0;

            await Task.Run(() =>
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

                writer.WriteLine(string.Join(separator, new[]
                {
                    "Id","Status","UploadStatus","Bib","Time","TimingPoint",
                    "RawData","IsClear","IsQuestion","HasBibChanges","BibChanges"
                }));

                foreach (var d in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var line = string.Join(separator, new[]
                    {
                        Escape(d.Id, separator),
                        Escape(d.Status.ToString(), separator),
                        Escape(d.StatusCode, separator),
                        Escape(d.Bib, separator),
                        Escape(d.Time.ToString("HH:mm:ss.ffff"), separator),
                        Escape(d.TimingPoint.Name, separator),
                        Escape(d.Raw, separator),
                        Escape(d.IsClear.ToString().ToLowerInvariant(), separator),
                        Escape(d.IsQuestion.ToString().ToLowerInvariant(), separator),
                        Escape((d.BibChanges.Count > 0).ToString().ToLowerInvariant(), separator),
                        Escape(string.Join("|", d.BibChanges), separator)
                    });

                    writer.WriteLine(line);

                    processed++;
                    int percent = (int)((double)processed / total * 100);
                    progress?.Report(percent);
                }
            });
        }

        public static async Task ExportImpulsesOnlyAsync(string filePath, string sourceId, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            var source = App.SourceManager.GetSource(sourceId);
            if (source == null)
            {
                App.Logger.LogWarning($"[ES] Export impulses failed: Source not found ({sourceId})");
                return;
            }

            App.Logger.Log($"[ES] Exporting impulses data for source ({sourceId})");

            // Snapshot on the calling (UI) thread - the live ObservableCollection
            // may be appended to by a running source during the export.
            var rows = source.LogData.ToList();
            var total = rows.Count;
            int processed = 0;

            await Task.Run(() =>
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

                foreach (var d in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    writer.WriteLine(d.Raw);

                    processed++;
                    int percent = (int)((double)processed / total * 100);
                    progress?.Report(percent);
                }
            });

            App.Logger.Log($"[ES] Export impulses completed for source ({sourceId}), {total} records exported to {filePath}");
        }
    }
}
