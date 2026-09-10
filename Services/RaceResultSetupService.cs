using System.Net.Http;
using System.Security.Cryptography;
using RaceResultClient;
using TimeMaker.Models;

namespace TimeMaker.Services
{
    /// <summary>
    /// The RaceResult session behind the settings window's "prihlásiť sa" mode: logs in, says
    /// which events the account can reach, and creates the Simple API entries Time Maker needs
    /// on the one that was picked.
    ///
    /// It looks before it writes, the same way the Trakster setup does: an entry that is already
    /// there, enabled and pointing at the right endpoint is left exactly as it is (key and all,
    /// so a link already pasted somewhere else stays valid), one that is missing is created, and
    /// one that points somewhere else is corrected. Entries the catalog does not mention - other
    /// apps' APIs on the same event - are read and posted back untouched, because
    /// <c>simpleapi/saveall</c> replaces the whole set. Nothing is ever deleted; an API the user
    /// unticks is simply not created.
    ///
    /// Nothing else in the app talks to RaceResult this way. The running app reads and writes
    /// through the Simple API links (<see cref="RaceResultService"/>), which need no login at
    /// all - this session exists only while the settings window is open, and
    /// <see cref="Dispose"/> logs it out again.
    /// </summary>
    public sealed class RaceResultSetupService : IDisposable
    {
        private ApiClient? _client;
        private bool _loggedIn;
        private string _server = RaceResultApiCatalog.DefaultServer;
        private bool _useHttps = true;

        /// <summary>Whether a RaceResult session is currently open.</summary>
        public bool IsLoggedIn => _loggedIn;

        /// <summary>
        /// Logs in and returns the upcoming events the account can reach. Upcoming only: setup
        /// prepares an event that has yet to be timed, and one already run has nothing to add.
        /// A second call replaces the first session rather than piling one on top of it.
        /// </summary>
        public async Task<IReadOnlyList<EventListItem>> LoginAndListEventsAsync(
            RaceResultCredentials credentials)
        {
            await LogoutAsync();

            _server = string.IsNullOrWhiteSpace(credentials.Server)
                ? RaceResultApiCatalog.DefaultServer
                : credentials.Server.Trim().Trim('/');
            _useHttps = credentials.UseHttps;

            App.Logger.Log($"[RR-SETUP] Logging in to {_server} "
                + $"({(credentials.HasApiKey ? "API key" : "user and password")}, https: {_useHttps})");
            var client = new ApiClient(_server, _useHttps);
            try
            {
                await RetryAsync(() => client.Public.LoginAsync(LoginOptionsFor(credentials)));
            }
            catch (Exception ex)
            {
                client.Dispose();
                App.Logger.LogError("[RR-SETUP] Login failed", ex);
                throw new HttpRequestException($"Neúspešné prihlásenie\nChyba: \n[{Describe(ex)}]");
            }

            _client = client;
            _loggedIn = true;
            App.Logger.Log("[RR-SETUP] Logged in");

            try
            {
                var events = await RetryAsync(() => client.Public.GetNextEventListAsync(100));
                App.Logger.Log($"[RR-SETUP] {events.Length} upcoming event(s) returned");
                return events;
            }
            catch (Exception ex)
            {
                App.Logger.LogError("[RR-SETUP] Cannot load event list", ex);
                throw new HttpRequestException($"Neúspešné načítanie podujatí\nChyba: \n[{Describe(ex)}]");
            }
        }

        /// <summary>
        /// Creates what the event is missing and corrects what points elsewhere, then returns the
        /// link of the <c>api</c> entry - the one the settings window loads, exactly as if it had
        /// been copied out of RaceResult by hand.
        /// </summary>
        /// <param name="eventId">The RaceResult event to set up.</param>
        /// <param name="optional">
        /// The optional rows the user ticked. Mandatory ones are always included; an optional one
        /// left out is not created, and if it already exists it is left alone. A row can stand for
        /// more than one Simple API entry - impulse invalidation needs two - and then all of them
        /// are created together.
        /// </param>
        public async Task<RaceResultSetupResult> ProvisionAsync(
            string eventId, IReadOnlyCollection<RaceResultApiDefinition> optional)
        {
            var client = _client
                ?? throw new InvalidOperationException("Nie ste prihlásený do RaceResult.");

            App.Logger.Log($"[RR-SETUP] Setting up event {eventId}");
            var ev = client.ForEvent(eventId);

            SimpleApiItem[] existing;
            try
            {
                existing = await RetryAsync(() => ev.SimpleApi.GetAsync());
            }
            catch (Exception ex)
            {
                App.Logger.LogError("[RR-SETUP] Cannot read the event's Simple API entries", ex);
                throw new HttpRequestException($"Neúspešné načítanie API podujatia\nChyba: \n[{Describe(ex)}]");
            }

            // Everything already on the event, so entries the catalog does not mention travel
            // back untouched in the save - saveall replaces the whole set.
            var merged = new Dictionary<string, SimpleApiItem>(StringComparer.Ordinal);
            foreach (var item in existing)
            {
                merged[item.Label] = item;
            }

            var desired = RaceResultApiCatalog.Entries
                .Where(e => e.Mandatory || optional.Contains(e))
                .SelectMany(e => e.Endpoints)
                .ToList();

            var created = 0;
            var corrected = 0;
            var unchanged = 0;

            foreach (var endpoint in desired)
            {
                var current = merged.GetValueOrDefault(endpoint.Label);

                // Already there, enabled and pointing at the same endpoint - leave it, key and all.
                if (current is not null && !current.Disabled
                    && string.Equals(current.Url, endpoint.Url, StringComparison.Ordinal))
                {
                    unchanged++;
                    continue;
                }

                // Each API needs its own access key. Reuse the existing one if the entry is
                // already on the event, so its link stays valid, otherwise generate a fresh one.
                var key = string.IsNullOrEmpty(current?.Key) ? NewKey() : current.Key;
                merged[endpoint.Label] = new SimpleApiItem(false, key, endpoint.Url, endpoint.Label);

                if (current is null)
                {
                    created++;
                }
                else
                {
                    corrected++;
                }
            }

            if (created + corrected > 0)
            {
                try
                {
                    await RetryAsync(() => ev.SimpleApi.SaveAllAsync(merged.Values.ToList()));

                    // Re-read rather than trust what was posted: the link is built from the key
                    // RaceResult ended up storing, not the one we proposed.
                    existing = await RetryAsync(() => ev.SimpleApi.GetAsync());
                }
                catch (Exception ex)
                {
                    App.Logger.LogError("[RR-SETUP] Cannot save the event's Simple API entries", ex);
                    throw new HttpRequestException($"Neúspešné uloženie API\nChyba: \n[{Describe(ex)}]");
                }

                App.Logger.Log($"[RR-SETUP] {created} created, {corrected} corrected, {unchanged} unchanged on event {eventId}");
            }
            else
            {
                App.Logger.Log($"[RR-SETUP] Event {eventId} was already set up, nothing written");
            }

            var api = existing.FirstOrDefault(e =>
                string.Equals(e.Label, RaceResultApiCatalog.ApiLabel, StringComparison.OrdinalIgnoreCase));

            if (api is null || string.IsNullOrWhiteSpace(api.Key))
            {
                App.Logger.LogError($"[RR-SETUP] No '{RaceResultApiCatalog.ApiLabel}' entry on event {eventId} after saving");
                throw new HttpRequestException(
                    $"Neúspešné načítanie odkazu\nChyba: \n[API '{RaceResultApiCatalog.ApiLabel}' sa na podujatí nenašlo]");
            }

            // The same scheme the login used. https unless the user turned it off for an
            // on-premise server without a certificate - the settings window warns about the
            // plain-HTTP link it then gets, exactly as it does for a pasted one.
            var host = RaceResultApiCatalog.SimpleApiHostFor(_server).Trim('/');
            var scheme = _useHttps ? "https" : "http";
            return new RaceResultSetupResult
            {
                ApiLink = $"{scheme}://{host}/{eventId}/{api.Key}",
                Created = created,
                Corrected = corrected,
                Unchanged = unchanged,
            };
        }

        /// <summary>
        /// Ends the session: logs out, then disposes the client - which owns an
        /// <see cref="HttpClient"/> of its own, so it has to be disposed whatever happened to
        /// the logout. Best effort on the logout itself; a session left open expires on its own
        /// and there is nothing useful to do about it here.
        /// </summary>
        public async Task LogoutAsync()
        {
            var client = _client;
            if (client is null)
            {
                return;
            }

            _client = null;
            try
            {
                if (_loggedIn)
                {
                    await client.Public.LogoutAsync();
                    App.Logger.Log("[RR-SETUP] Logged out");
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogWarning($"[RR-SETUP] Logout failed - {ex.Message}");
            }
            finally
            {
                _loggedIn = false;
                client.Dispose();
            }
        }

        public void Dispose()
        {
            var client = _client;
            if (client is null)
            {
                return;
            }

            // Fire and forget: Dispose cannot await, and the window closing must not block on
            // the network. The client is disposed inside LogoutAsync either way.
            _ = LogoutAsync();
        }

        /// <summary>
        /// Turns the two ways of logging in into what the client expects. An API key wins when
        /// both are filled in, so a key left in the box is never silently ignored.
        /// </summary>
        private static LoginOptions LoginOptionsFor(RaceResultCredentials credentials)
        {
            if (credentials.HasApiKey)
            {
                return new LoginOptions { ApiKey = credentials.ApiKey!.Trim() };
            }

            return new LoginOptions
            {
                User = credentials.User,
                Password = credentials.Password,
            };
        }

        /// <summary>
        /// Generates a Simple API access key in the same style RaceResult uses - a 32-character
        /// uppercase alphanumeric string. Keys are event-bound, so they only need to be distinct
        /// per API within one event.
        /// </summary>
        private static string NewKey()
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var chars = new char[32];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            }

            return new string(chars);
        }

        /// <summary>
        /// Retries a call a couple of times on a transient failure. The RaceResult client uses a
        /// plain <see cref="HttpClient"/>, and the server sits behind a gateway that drops idle
        /// keep-alive connections - so a POST can land on a dead connection and fail with
        /// "response ended prematurely" or a 502/503/504. A retry opens a fresh connection.
        /// </summary>
        private static async Task RetryAsync(Func<Task> action, int maxAttempts = 3)
        {
            await RetryAsync<object?>(async () =>
            {
                await action();
                return null;
            }, maxAttempts);
        }

        private static async Task<T> RetryAsync<T>(Func<Task<T>> action, int maxAttempts = 3)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (attempt < maxAttempts && IsTransient(ex))
                {
                    App.Logger.LogWarning($"[RR-SETUP] Transient failure ({ex.Message}), retrying");
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
                }
            }
        }

        private static bool IsTransient(Exception ex) => ex switch
        {
            HttpRequestException => true,
            ApiException api => api.StatusCode is 502 or 503 or 504,
            _ => false,
        };

        /// <summary>
        /// A user-facing description of a failure from the client. The inner-exception chain is
        /// flattened so the real transport-level cause is shown rather than the outer wrapper.
        /// </summary>
        private static string Describe(Exception ex)
        {
            if (ex is ApiException api)
            {
                return $"{api.StatusCode} - {api.Message}";
            }

            var messages = new List<string>();
            for (Exception? current = ex; current is not null; current = current.InnerException)
            {
                if (!string.IsNullOrWhiteSpace(current.Message))
                {
                    messages.Add(current.Message);
                }
            }

            return string.Join(" - ", messages);
        }
    }
}
