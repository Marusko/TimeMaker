namespace TimeMaker.Models
{
    /// <summary>
    /// What the settings window needs to open a RaceResult session: which server, over which
    /// scheme, and one of the two ways RaceResult accepts a login - a user and password, or an
    /// API key. Nothing here is stored anywhere; it lives only for the length of one login.
    /// </summary>
    public sealed record RaceResultCredentials
    {
        /// <summary>The server events are administered on, e.g. <c>events.raceresult.com</c>.</summary>
        public required string Server { get; init; }

        /// <summary>
        /// Off only for an on-premise server without a certificate. The Simple API key travels in
        /// the path, so the link built from a plain-HTTP login is one the app then warns about.
        /// </summary>
        public bool UseHttps { get; init; } = true;

        public string? User { get; init; }

        public string? Password { get; init; }

        public string? ApiKey { get; init; }

        /// <summary>An API key wins when both are filled in, the same way the API Creator does it.</summary>
        public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

        public bool HasUserPassword =>
            !string.IsNullOrWhiteSpace(User) && !string.IsNullOrEmpty(Password);
    }
}
