namespace TimeMaker.Models
{
    /// <summary>One Simple API entry on the RaceResult event.</summary>
    /// <param name="Label">
    /// Short name, and the key everything is matched by: <see cref="Services.RaceResultService.LoadApi"/>
    /// switches on it, and setup finds an entry already on the event by it.
    /// </param>
    /// <param name="Url">The endpoint definition, exactly as RaceResult stores it.</param>
    public sealed record RaceResultApiEndpoint(string Label, string Url);

    /// <summary>
    /// One thing the user can switch on or off, which is not always one Simple API entry: the
    /// impulse invalidation needs two of them (<c>invalid</c> and <c>search</c>) and is useless
    /// with only one, so they are offered together as a single row and created together.
    /// </summary>
    /// <param name="Title">What the entry is called in the UI.</param>
    /// <param name="Mandatory">
    /// Mandatory entries are always created - the app cannot work without them - so their
    /// checkbox is shown ticked and locked. Optional ones can be turned off.
    /// </param>
    /// <param name="Endpoints">The Simple API entries this row creates - one, or several.</param>
    /// <param name="Hint">One line saying what turning it off costs.</param>
    public sealed record RaceResultApiDefinition(
        string Title,
        bool Mandatory,
        IReadOnlyList<RaceResultApiEndpoint> Endpoints,
        string? Hint = null)
    {
        /// <summary>The labels this row covers, as RaceResult shows them - "invalid, search".</summary>
        public string LabelText => string.Join(", ", Endpoints.Select(e => e.Label));
    }

    /// <summary>
    /// Every Simple API entry Time Maker needs, in one place. The list is the same one the
    /// API Creator writes for Time Maker, kept here so the settings window can create the
    /// entries itself instead of the user copying them in by hand.
    ///
    /// Nothing at runtime depends on an entry's URL, only on its label - the URLs matter to
    /// setup, which compares them against what the event already has and corrects an entry
    /// that points somewhere else.
    /// </summary>
    public static class RaceResultApiCatalog
    {
        /// <summary>Label of the entry that lists all the others. Its link is the one pasted into the app.</summary>
        public const string ApiLabel = "api";

        /// <summary>The host RaceResult's own cloud administers events on, which is where all but an on-premise install logs in.</summary>
        public const string DefaultServer = "events.raceresult.com";

        /// <summary>The host RaceResult's own cloud serves the Simple API from.</summary>
        public const string CloudSimpleApiHost = "api.raceresult.com";

        /// <summary>
        /// The entries, in the order the selection dialog lists them. Mandatory first, so what
        /// cannot be turned off is read before what can.
        /// </summary>
        public static IReadOnlyList<RaceResultApiDefinition> Entries { get; } = new[]
        {
            new RaceResultApiDefinition("Pridávanie impulzov", true,
                new[] { new RaceResultApiEndpoint("manual", "rawdata/addmanual") },
                "Bez neho nie je kam odosielať časy."),
            new RaceResultApiDefinition("Meracie body", true,
                new[] { new RaceResultApiEndpoint("points", "timingpoints/get") },
                "Bez nich sa nedá vytvoriť zdroj."),
            new RaceResultApiDefinition("Spoločné API", true,
                new[] { new RaceResultApiEndpoint(ApiLabel, "simpleapi/get") },
                "Odkaz na toto API sa načíta do Time Maker."),
            // Two entries, one switch: invalidation looks the impulse up first and only then
            // invalidates it, so one without the other does nothing.
            new RaceResultApiDefinition("Zneplatnenie impulzov", false,
                new[]
                {
                    new RaceResultApiEndpoint("search", "rawdata/get"),
                    new RaceResultApiEndpoint("invalid", "rawdata/setinvalid"),
                },
                "Vyhľadanie a zneplatnenie impulzu - potrebné pre automatické aj ručné zneplatnenie."),
            new RaceResultApiDefinition("Zoznam čísel", false,
                new[] { new RaceResultApiEndpoint("bibs", "data/list?&fields=Bib&listformat=JSON") },
                "Potrebné pre CSV šablónu."),
        };

        /// <summary>Every Simple API entry the catalog knows, flattened out of <see cref="Entries"/>.</summary>
        public static IEnumerable<RaceResultApiEndpoint> AllEndpoints =>
            Entries.SelectMany(e => e.Endpoints);

        /// <summary>
        /// Where the Simple API of an event administered on <paramref name="loginServer"/> lives.
        /// On RaceResult's cloud the two are different hosts - events are administered on
        /// <see cref="DefaultServer"/> and the Simple API is served from
        /// <see cref="CloudSimpleApiHost"/> - so the login server cannot simply be reused.
        /// Anywhere else it is the same host, which is what an on-premise install wants.
        /// </summary>
        public static string SimpleApiHostFor(string loginServer) =>
            string.Equals(loginServer, DefaultServer, StringComparison.OrdinalIgnoreCase)
                ? CloudSimpleApiHost
                : loginServer;
    }

    /// <summary>What setup did to one Simple API entry.</summary>
    public enum RaceResultApiOutcome
    {
        /// <summary>Already there and pointing at the right endpoint - left exactly as it was.</summary>
        Unchanged,

        /// <summary>Was not on the event at all.</summary>
        Created,

        /// <summary>Was there but disabled or pointing somewhere else, so it was rewritten (keeping its key).</summary>
        Corrected,
    }

    /// <summary>The outcome of one run of setup: the link to load, and what it took to get there.</summary>
    public sealed class RaceResultSetupResult
    {
        public required string ApiLink { get; init; }

        public int Created { get; init; }

        public int Corrected { get; init; }

        public int Unchanged { get; init; }

        public bool NothingChanged => Created == 0 && Corrected == 0;

        /// <summary>
        /// The short form, for the status line in the settings window - which has room for two
        /// lines, and sits right beside the event it is talking about.
        /// </summary>
        public string Summary =>
            NothingChanged
                ? $"Všetko už bolo nastavené, {Unchanged} API zostalo nezmenených."
                : $"Vytvorených {Created}, opravených {Corrected}, nezmenených {Unchanged} API.";

        /// <summary>A sentence saying what happened, for the dialog shown once setup returns.</summary>
        public string Describe(string eventName)
        {
            if (NothingChanged)
            {
                return $"Podujatie '{eventName}' už bolo správne nastavené, "
                       + $"všetkých {Unchanged} API zostalo nezmenených.";
            }

            var parts = new List<string>();
            if (Created > 0)
            {
                parts.Add($"vytvorených {Created}");
            }
            if (Corrected > 0)
            {
                parts.Add($"opravených {Corrected}");
            }
            if (Unchanged > 0)
            {
                parts.Add($"nezmenených {Unchanged}");
            }

            return $"Podujatie '{eventName}': {string.Join(", ", parts)} API. "
                   + "Ostatné API podujatia zostali nedotknuté.";
        }
    }
}
