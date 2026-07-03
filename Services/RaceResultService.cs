using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Windows.Threading;
using TimeMaker.Models;

namespace TimeMaker.Services
{
    public class RaceResultService : IDisposable
    {
        public List<ApiTimingPoint> Points { get; set; } = new();
        public bool TemplateEnabled { get; set; }
        public bool ClearEnabled { get; set; }
        public bool ShowErrorNotification { get; set; }

        private HttpClient _httpClient;
        private DispatcherTimer? _timer;
        private DispatcherTimer? _bibTimer;
        private DispatcherTimer? _collectTimer;
        private string _manualUrl = "";
        private string _pointsUrl = "";
        private string _bibListUrl = "";
        private string _rawSearchUrl = "";
        private string _invalidateUrl = "";
        private List<string> _bibList = new();
        private ConcurrentQueue<DataModel> _unsentData = new();

        public event EventHandler<RaceResultApiLoadedEventArgs>? RaceResultApiLoaded;
        public event EventHandler<RaceResultBibsLoadedEventArgs>? RaceResultBibsLoaded;
        public event EventHandler<RaceResultTimeSentEventArgs>? RaceResultTimeSent;

        public RaceResultService()
        {
            // Without a short timeout a hung server stacks up in-flight
            // requests, because the send timer ticks every second.
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        private void Clear()
        {
            App.Logger.Log("[RR] Clearing data");
            Points.Clear();
            _bibList.Clear();
            _manualUrl = "";
            _pointsUrl = "";
            _bibListUrl = "";
            _rawSearchUrl = "";
            _invalidateUrl = "";
            TemplateEnabled = false;
            ClearEnabled = false;
        }

        public async Task Start()
        {
            App.Logger.Log("[RR] Starting...");
            // Stop any timers from a previous Start so a re-save of the
            // settings does not leave orphaned timers ticking.
            _timer?.Stop();
            _bibTimer?.Stop();
            _collectTimer?.Stop();
            _timer = new();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += SendDataToRr;
            _bibTimer = new();
            _bibTimer.Interval = TimeSpan.FromSeconds(30);
            _bibTimer.Tick += LoadBibsAuto;
            _collectTimer = new();
            _collectTimer.Interval = TimeSpan.FromSeconds(15);
            _collectTimer.Tick += CollectData;
            if (TemplateEnabled)
            {
                await LoadBibs(_bibListUrl);
                RaceResultBibsLoaded?.Invoke(this, new RaceResultBibsLoadedEventArgs { Bibs = _bibList });
                _bibTimer.Start();
            }
            _collectTimer.Start();
            App.Logger.Log("[RR] Started");
        }

        public async Task LoadApi(string apiLink)
        {
            Clear();
            App.Logger.Log("[RR] Loading API...");
            var evArgs = new RaceResultApiLoadedEventArgs();
            var apis = await GetJsonAsync<List<ApiModel>>(apiLink, "Neúspešné načítanie API");

            var index = apiLink.LastIndexOf("/", StringComparison.Ordinal);
            if (index == -1)
            {
                App.Logger.LogError("[RR] Cannot load API - cannot find common part of the link");
                throw new HttpRequestException("Neúspešné načítanie API\nChyba: \n[Nepodarilo sa nájsť spoločnú časť]");
            }
            var link = apiLink.Substring(0, index + 1);

            bool hasInvalidApi = false;
            bool hasSearchApi = false;

            foreach (var api in apis)
            {
                var label = api.Label?.ToLower().Trim();
                var enabled = api.Disabled == false;
                var url = link + api.Key;

                switch (label)
                {
                    case "points":
                        (evArgs.PointsApiStatus, evArgs.PointsApiLoaded) = ApiStatus("Points", enabled);
                        if (enabled)
                        {
                            _pointsUrl = url;
                            await LoadPoints(url);
                        }
                        break;
                    case "manual":
                        (evArgs.ManualApiStatus, evArgs.ManualApiLoaded) = ApiStatus("Manual", enabled);
                        if (enabled)
                        {
                            _manualUrl = url;
                        }
                        break;
                    case "bibs":
                        (evArgs.BibsApiStatus, evArgs.BibsApiLoaded) = ApiStatus("Bibs", enabled);
                        if (enabled)
                        {
                            _bibListUrl = url;
                            TemplateEnabled = true;
                        }
                        break;
                    case "invalid":
                        (evArgs.InvalidApiStatus, evArgs.InvalidApiLoaded) = ApiStatus("Invalid", enabled);
                        if (enabled)
                        {
                            _invalidateUrl = url;
                        }
                        hasInvalidApi = enabled;
                        break;
                    case "search":
                        (evArgs.RawSearchApiStatus, evArgs.RawSearchApiLoaded) = ApiStatus("Raw Search", enabled);
                        if (enabled)
                        {
                            _rawSearchUrl = url;
                        }
                        hasSearchApi = enabled;
                        break;
                }
            }

            ClearEnabled = hasInvalidApi && hasSearchApi;
            RaceResultApiLoaded?.Invoke(this, evArgs);
            App.Logger.Log("[RR] Successfully loaded APIs");
            if (!ClearEnabled)
            {
                NotificationService.ShowInfoNotification("Zneplatnenie", "Automatické / ručné zneplatnenie je vypnuté");
            }
        }

        private static (string Status, bool Loaded) ApiStatus(string name, bool enabled)
        {
            if (enabled)
            {
                App.Logger.Log($"[RR] Successfully loaded {name} API");
                return ("Načítané", true);
            }
            App.Logger.LogWarning($"[RR] {name} API is off");
            return ("Vypnuté", false);
        }

        private async Task LoadBibs(string apiLink)
        {
            App.Logger.Log("[RR] Loading Bibs from API...");
            var bibs = await GetJsonAsync<List<List<string>>>(apiLink, "Neúspešné načítanie štartových čísel");
            _bibList = bibs.Where(b => b.Count > 0).Select(b => b[0]).ToList();
            App.Logger.Log("[RR] Successfully loaded Bibs from API");
        }

        private async void LoadBibsAuto(object? sender, EventArgs e)
        {
            App.Logger.Log("[RR] AUTO bibs reload");
            try
            {
                await LoadBibs(_bibListUrl);
                RaceResultBibsLoaded?.Invoke(this, new RaceResultBibsLoadedEventArgs { Bibs = _bibList });
                StartSendTimerIfNeeded();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("[RR] AUTO bibs reload failed", ex);
            }
        }

        private async Task LoadPoints(string apiLink)
        {
            App.Logger.Log("[RR] Loading Points from API...");
            Points = await GetJsonAsync<List<ApiTimingPoint>>(apiLink, "Neúspešné načítanie meracích bodov");
            App.Logger.Log("[RR] Successfully loaded Points from API");
        }

        /// <summary>
        /// GET + status check + JSON deserialize with the shared error handling
        /// (logged and rethrown as a user-facing HttpRequestException).
        /// </summary>
        private async Task<T> GetJsonAsync<T>(string url, string errorPrefix) where T : class
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url);
            }
            catch (Exception e)
            {
                App.Logger.LogError($"[RR] {errorPrefix} - request failed", e);
                throw new HttpRequestException($"{errorPrefix}\nChyba: \n[{e.Message}]");
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response);
                App.Logger.LogError($"[RR] {errorPrefix} - {error}");
                throw new HttpRequestException($"{errorPrefix}\nChyba: \n[{error}]");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            T? result;
            try
            {
                result = JsonConvert.DeserializeObject<T>(responseString);
            }
            catch (Exception e)
            {
                App.Logger.LogError($"[RR] {errorPrefix} - cannot deserialize data", e);
                throw new HttpRequestException($"{errorPrefix}\nChyba: \n[Nemôžem deserializovať dáta]");
            }

            if (result == null)
            {
                App.Logger.LogError($"[RR] {errorPrefix} - response is null");
                throw new HttpRequestException($"{errorPrefix}\nChyba: \n[Dáta sú null]");
            }

            return result;
        }

        private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
        {
            try
            {
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                return response.StatusCode.ToString();
            }
        }

        public void AddManual(DataModel model)
        {
            App.Logger.Log($"[RR] Adding manual data - {model.Id}");
            _unsentData.Enqueue(model);
            StartSendTimerIfNeeded();
        }

        private void StartSendTimerIfNeeded()
        {
            if (!_unsentData.IsEmpty && _timer is { IsEnabled: false })
            {
                _timer.Start();
            }
        }

        private void CollectData(object? sender, EventArgs e)
        {
            App.Logger.Log("[RR] AUTO collecting data...");
            try
            {
                foreach (var source in App.SourceManager.Sources)
                {
                    if (source.Value.Running && !source.Value.DataDictionary.IsEmpty)
                    {
                        foreach (var data in source.Value.GetUnsentData())
                        {
                            _unsentData.Enqueue(data);
                        }
                    }
                }

                StartSendTimerIfNeeded();
                App.Logger.Log("[RR] AUTO data collected");
            }
            catch (Exception ex)
            {
                App.Logger.LogError("[RR] AUTO data collection failed", ex);
            }
        }

        private async void SendDataToRr(object? sender, EventArgs e)
        {
            if (_unsentData.IsEmpty)
            {
                _timer?.Stop();
                return;
            }

            App.Logger.Log("[RR] AUTO sending data...");
            if (!_unsentData.TryDequeue(out var data))
            {
                return;
            }

            try
            {
                HttpResponseMessage resp;
                resp = ClearEnabled
                    ? (data.IsClear ? await SendClearData(data) : await SendData(data))
                    : (data.IsClear ? new HttpResponseMessage(HttpStatusCode.FailedDependency) : await SendData(data));
                RaceResultTimeSent?.Invoke(this, new RaceResultTimeSentEventArgs()
                {
                    Id = data.Id,
                    SourceId = data.SourceId,
                    Status = resp.IsSuccessStatusCode ? UploadStatus.Completed : UploadStatus.Failed,
                    StatusCode = resp.IsSuccessStatusCode ? "" : await resp.Content.ReadAsStringAsync()
                });
                if (!resp.IsSuccessStatusCode)
                {
                    NotifySendFailure(data);
                    App.Logger.LogWarning($"[RR] AUTO sending data failed - {resp.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                RaceResultTimeSent?.Invoke(this, new RaceResultTimeSentEventArgs()
                {
                    Id = data.Id,
                    SourceId = data.SourceId,
                    Status = UploadStatus.Failed,
                    StatusCode = ex.Message
                });
                NotifySendFailure(data);
                App.Logger.LogError("[RR] AUTO sending data failed", ex);
            }
        }

        private void NotifySendFailure(DataModel data)
        {
            if (ShowErrorNotification && (!data.IsClear || ClearEnabled))
            {
                var type = data.IsClear ? "mazania" : "času";
                NotificationService.ShowRetryNotification("Odoslanie impulzu",
                    $"Neúspešné odoslanie {type} pre číslo {data.Bib} a čas {data.Time:HH:mm:ss.ffff} na bod {data.TimingPoint.Name}",
                    data.SourceId, data.Id);
            }
        }

        private static string FormatTime(TimeOnly time)
        {
            return time.ToTimeSpan().TotalSeconds.ToString(CultureInfo.InvariantCulture);
        }

        private async Task<HttpResponseMessage> SendData(DataModel d)
        {
            var conn = $"{_manualUrl}?timingpoint={Uri.EscapeDataString(d.TimingPoint.Name)}&bib={Uri.EscapeDataString(d.Bib)}&time={FormatTime(d.Time)}";
            return await _httpClient.GetAsync(conn);
        }

        private async Task<HttpResponseMessage> SendClearData(DataModel d)
        {
            string time = FormatTime(d.Time);
            var conn = $"{_rawSearchUrl}?bib={Uri.EscapeDataString(d.Bib)}";
            var resp = await _httpClient.GetAsync(conn);
            if (!resp.IsSuccessStatusCode)
            {
                return resp;
            }

            var responseString = await resp.Content.ReadAsStringAsync();
            List<TimingResult>? results;
            try
            {
                results = JsonConvert.DeserializeObject<List<TimingResult>>(responseString);
            }
            catch (Exception e)
            {
                App.Logger.LogError("[RR] Cannot load timing data from API - cannot deserialize data", e);
                throw new HttpRequestException("Neúspešné načítanie časových dát\nChyba: \n[Nemôžem deserializovať dáta]");
            }

            if (results == null)
            {
                App.Logger.LogError("[RR] Cannot load timing data from API - results list is null");
                throw new HttpRequestException("Neúspešné načítanie časových dát\nChyba: \n[Impulzy sú null]");
            }

            var single = results.FirstOrDefault(r => r.TimingPoint == d.TimingPoint.Name && r.Time.Equals(time));
            if (single == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var invConn = $"{_invalidateUrl}?id={single.Id}&invalid=true";
            return await _httpClient.GetAsync(invConn);
        }

        public void Dispose()
        {
            App.Logger.Log("[RR] Stopping and disposing resources");
            _timer?.Stop();
            _bibTimer?.Stop();
            _collectTimer?.Stop();
            _bibList.Clear();
            _unsentData.Clear();
            _httpClient.Dispose();
        }
    }
}
