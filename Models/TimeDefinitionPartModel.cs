using System.Globalization;

namespace TimeMaker.Models
{
    /// <summary>
    /// One block of a time definition: a continuous bib range whose first time is
    /// <see cref="FirstTime"/>, split into waves of <see cref="WaveSize"/> bibs,
    /// each wave <see cref="WaveInterval"/> after the previous one. A definition is
    /// a list of these, so a longer gap between categories is simply the next part.
    /// </summary>
    public class TimeDefinitionPartModel
    {
        /// <summary>Upper bound on the bibs a single part may generate.</summary>
        public const int MaxEntriesPerPart = 50000;

        public TimeOnly FirstTime { get; set; }
        public int FirstBib { get; set; }
        public int LastBib { get; set; }
        public int WaveSize { get; set; } = 1;
        public TimeSpan WaveInterval { get; set; }

        public int EntryCount => LastBib - FirstBib + 1;

        public IEnumerable<TimeDefinitionEntry> Generate()
        {
            var time = FirstTime;
            var inWave = 0;
            for (var bib = FirstBib; bib <= LastBib; bib++)
            {
                if (inWave == WaveSize)
                {
                    // TimeOnly.Add wraps over midnight, which is what a race
                    // running past 00:00 needs anyway.
                    time = time.Add(WaveInterval);
                    inWave = 0;
                }
                yield return new TimeDefinitionEntry(bib.ToString(CultureInfo.InvariantCulture), time);
                inWave++;
            }
        }

        /// <summary>
        /// Parses a wave interval written as seconds ("30"), "mm:ss" or "hh:mm:ss",
        /// each part optionally fractional. Plain TimeSpan.TryParse is not used
        /// because it reads "1:30" as one and a half hours.
        /// </summary>
        public static bool TryParseInterval(string s, out TimeSpan interval)
        {
            interval = TimeSpan.Zero;
            s = s.Trim();
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            var parts = s.Split(':');
            if (parts.Length > 3)
            {
                return false;
            }

            double total = 0;
            foreach (var part in parts)
            {
                if (!double.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                    || value < 0)
                {
                    return false;
                }
                total = total * 60 + value;
            }

            interval = TimeSpan.FromSeconds(total);
            return true;
        }

        public static string IntervalToString(TimeSpan interval)
        {
            return interval < TimeSpan.FromHours(1)
                ? interval.ToString(@"mm\:ss", CultureInfo.InvariantCulture)
                : interval.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
        }
    }

    public static class TimeDefinition
    {
        /// <summary>Slovak counts: 1 / 2-4 / 5+ take three different forms.</summary>
        public static string Plural(int count, string one, string few, string many)
        {
            var form = count == 1 ? one : count is >= 2 and <= 4 ? few : many;
            return $"{count} {form}";
        }

        public static string Parts(int count) => Plural(count, "časť", "časti", "častí");

        public static string Times(int count) => Plural(count, "čas", "časy", "časov");

        public static string Summary(IReadOnlyCollection<TimeDefinitionPartModel> definition)
        {
            var times = definition.Sum(p => p.EntryCount);
            return $"{Parts(definition.Count)}, {Times(times)}";
        }
    }

    public readonly record struct TimeDefinitionEntry(string Bib, TimeOnly Time)
    {
        /// <summary>Whole seconds stay short; a fractional wave interval is shown in full.</summary>
        public string TimeText => Time.Ticks % TimeSpan.TicksPerSecond == 0
            ? Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
            : Time.ToString("HH:mm:ss.ffff", CultureInfo.InvariantCulture);
    }
}
