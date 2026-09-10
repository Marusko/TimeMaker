namespace TimeMaker.ViewModels
{
    /// <summary>
    /// One RaceResult event in the settings window's picker. The name alone does not tell two
    /// events apart - the same race is run every year, and a test copy sits beside the real one -
    /// so the date and the place are shown under it.
    /// </summary>
    public class RaceResultEventViewModel
    {
        public RaceResultEventViewModel(string id, string name, string date, string place)
        {
            Id = id;
            Name = string.IsNullOrWhiteSpace(name) ? id : name;
            Date = date ?? string.Empty;
            Place = place ?? string.Empty;
        }

        /// <summary>The RaceResult event id - what setup is run against.</summary>
        public string Id { get; }

        public string Name { get; }

        public string Date { get; }

        public string Place { get; }

        /// <summary>
        /// The line under the name. Not every event names a place, and an empty one should not
        /// leave a stray separator behind.
        /// </summary>
        public string Caption => string.IsNullOrWhiteSpace(Place) ? Date : $"{Date} · {Place}";

        /// <summary>Whether this event matches what was typed into the picker's search box.</summary>
        public bool Matches(string filter) =>
            string.IsNullOrWhiteSpace(filter)
            || Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || Place.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }
}
