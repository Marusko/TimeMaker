using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Windows.Threading;
using TimeMaker.Models;

namespace TimeMaker.Services
{
    public class RaceResultService : IDisposable
    {
        public List<ApiTimingPoint> Points { get; set; } = new();
        public bool TemplateEnabled { get; set; }

        private HttpClient _httpClient;
        private DispatcherTimer? _timer;
        private DispatcherTimer? _bibTimer;
        private DispatcherTimer? _collectTimer;
        private string _manualUrl = "";
        private string _pointsUrl = "";
        private string _bibListUrl = "";
        private List<string> _bibList = new();
        private ConcurrentQueue<DataModel> _unsentData = new();
        
        public event EventHandler<RaceResultApiLoadedEventArgs>? RaceResultApiLoaded;
        public event EventHandler<RaceResultBibsLoadedEventArgs>? RaceResultBibsLoaded;
        public event EventHandler<RaceResultTimeSentEventArgs>? RaceResultTimeSent;

        public RaceResultService()
        {
            _httpClient = new HttpClient();
        }

        private void Clear()
        {
            App.Logger.Log("[RR] Clearing data");
            Points.Clear();
            _bibList.Clear();
            _manualUrl = "";
            _pointsUrl = "";
            _bibListUrl = "";
        }

        public async Task Start()
        {
            App.Logger.Log("[RR] Starting...");
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
            HttpResponseMessage response;
            var evArgs = new RaceResultApiLoadedEventArgs();
            try
            {
                response = await _httpClient.GetAsync(apiLink);
            }
            catch (Exception e)
            {
                App.Logger.LogError("[RR] Cannot load API", e);
                throw new HttpRequestException($"Neúspešné načítanie API\nChyba: \n[{e.Message}]");
            }
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                List<ApiModel>? apis;
                try
                {
                    apis = JsonConvert.DeserializeObject<List<ApiModel>>(responseString);
                }
                catch (Exception e)
                {
                    App.Logger.LogError("[RR] Cannot load API - cannot deserialize data", e);
                    throw new HttpRequestException("Neúspešné načítanie API\nChyba: \n[Nemôžem deserializovať dáta]");
                }
                if (apis == null)
                {
                    App.Logger.LogError("[RR] Cannot load API - API list is null");
                    throw new HttpRequestException("Neúspešné načítanie API\nChyba: \n[API sú null]");
                }

                var index = apiLink.LastIndexOf("/", StringComparison.Ordinal);
                if (index == -1)
                {
                    App.Logger.LogError("[RR] Cannot load API - cannot find common part of the link");
                    throw new HttpRequestException("Neúspešné načítanie API\nChyba: \n[Nepodarilo sa nájsť spoločnú časť]");
                }
                var link = apiLink.Substring(0, index + 1);
                foreach (var api in apis)
                {
                    if (api.Label != null && api.Label.ToLower().Trim().Equals("points"))
                    {
                        if (api.Disabled != null && !(bool)api.Disabled)
                        {
                            _pointsUrl = link + api.Key;
                            await LoadPoints(_pointsUrl);
                            App.Logger.Log("[RR] Successfully loaded Points API");
                            evArgs.PointsApiStatus = "Načítané";
                        }
                        else
                        {
                            App.Logger.LogWarning("[RR] Points API is off");
                            evArgs.PointsApiStatus = "Vypnuté";
                        }
                    }
                    else if (api.Label != null && api.Label.ToLower().Trim().Equals("manual"))
                    {
                        if (api.Disabled != null && !(bool)api.Disabled)
                        {
                            _manualUrl = link + api.Key;
                            App.Logger.Log("[RR] Successfully loaded Manual API");
                            evArgs.ManualApiStatus = "Načítané";
                        }
                        else
                        {
                            App.Logger.LogWarning("[RR] Manual API is off");
                            evArgs.ManualApiStatus = "Vypnuté";
                        }
                    }
                    else if (api.Label != null && api.Label.ToLower().Trim().Equals("bibs"))
                    {
                        if (api.Disabled != null && !(bool)api.Disabled)
                        {
                            _bibListUrl = link + api.Key;
                            TemplateEnabled = true;
                            App.Logger.Log("[RR] Successfully loaded Bibs API");
                            evArgs.BibsApiStatus = "Načítané";
                        }
                        else
                        {
                            App.Logger.LogWarning("[RR] Bibs API is off");
                            evArgs.BibsApiStatus = "Vypnuté";
                        }
                    }
                }
                RaceResultApiLoaded?.Invoke(this, evArgs);
                App.Logger.Log("[RR] Successfully loaded APIs");
            }
            else
            {
                App.Logger.LogError($"[RR] Cannot load API - {response.StatusCode}");
                throw new HttpRequestException($"Neúspešné načítanie API\nChyba: \n[{response.StatusCode}]");
            }
        }

        private async Task LoadBibs(string apiLink)
        {
            App.Logger.Log("[RR] Loading Bibs from API...");
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(apiLink);
            }
            catch (Exception e)
            {
                App.Logger.LogError("[RR] Cannot load Bibs from API", e);
                throw new HttpRequestException($"Neúspešné načítanie štartových čísel\nChyba: \n[{e.Message}]");
            }
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                List<List<string>>? bibs;
                try
                {
                    bibs = JsonConvert.DeserializeObject<List<List<string>>>(responseString);
                }
                catch (Exception e)
                {
                    App.Logger.LogError("[RR] Cannot load Bibs from API - cannot deserialize data", e);
                    throw new HttpRequestException("Neúspešné načítanie štartových čísel\nChyba: \n[Nemôžem deserializovať dáta]");
                }

                if (bibs != null)
                {
                    var tmp = (from b in bibs select b[0]).ToList();
                    _bibList = tmp;
                    App.Logger.Log("[RR] Successfully loaded Bibs from API");
                }
                else
                {
                    App.Logger.LogError("[RR] Cannot load Bibs from API - bibs list is null");
                    throw new HttpRequestException("Neúspešné načítanie štartových čísel\nChyba: \n[Čísla sú null]");
                }
            }
            else
            {
                App.Logger.LogError($"[RR] Cannot load Bibs from API - {response.StatusCode}");
                throw new HttpRequestException($"Neúspešné načítanie štartových čísel\nChyba: \n[{response.StatusCode}]");
            }
        }

        private async void LoadBibsAuto(object? sender, EventArgs e)
        {
            App.Logger.Log("[RR] AUTO bibs reload");
            try
            {
                await LoadBibs(_bibListUrl);
                RaceResultBibsLoaded?.Invoke(this, new RaceResultBibsLoadedEventArgs { Bibs = _bibList });
                if (!_unsentData.IsEmpty)
                {
                    if (_timer is { IsEnabled: false })
                    {
                        _timer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogError("[RR] AUTO bibs reload failed", ex);
            }
        }

        private async Task LoadPoints(string apiLink)
        {
            App.Logger.Log("[RR] Loading Points from API...");
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(apiLink);
            }
            catch (Exception e)
            {
                App.Logger.LogError("[RR] Cannot load Points from API", e);
                throw new HttpRequestException($"Neúspešné načítanie meracích bodov\nChyba: \n[{e.Message}]");
            }

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                List<ApiTimingPoint>? points;
                try
                {
                    points = JsonConvert.DeserializeObject<List<ApiTimingPoint>>(responseString);
                }
                catch (Exception e)
                {
                    App.Logger.LogError("[RR] Cannot load Points from API - cannot deserialize data", e);
                    throw new HttpRequestException("Neúspešné načítanie meracích bodov\nChyba: \n[Nemôžem deserializovať dáta]");
                }

                if (points != null)
                {
                    Points = points;
                }
                else
                {
                    App.Logger.LogError("[RR] Cannot load Points from API - points list is null");
                    throw new HttpRequestException("Neúspešné načítanie meracích bodov\nChyba: \n[Body sú null]");
                }
            }
            else
            {
                App.Logger.LogError($"[RR] Cannot load Points from API - {response.StatusCode}");
                throw new HttpRequestException($"Neúspešné načítanie meracích bodov\nChyba: \n[{response.StatusCode}]");
            }
        }

        private void CollectData(object? sender, EventArgs e)
        {
            App.Logger.Log("[RR] AUTO collecting data...");
            foreach (var source in App.SourceManager.Sources)
            {
                if (!source.Value.DataQueue.IsEmpty)
                {
                    var unsent = source.Value.GetUnsentData();
                    foreach (var data in unsent)
                    {
                        _unsentData.Enqueue(data);
                    }
                }
            }
            if (!_unsentData.IsEmpty)
            {
                if (_timer is { IsEnabled: false })
                {
                    _timer.Start();
                }
            }
            App.Logger.Log("[RR] AUTO data collected");
        }

        private async void SendDataToRr(object? sender, EventArgs e)
        {
            App.Logger.Log("[RR] AUTO sending data...");
            if (!_unsentData.IsEmpty)
            {
                var data = _unsentData.TryDequeue(out var item) ? item : null;
                try
                {
                    if (data != null)
                    {
                        var resp = await SendData(data);
                        RaceResultTimeSent?.Invoke(this, new RaceResultTimeSentEventArgs()
                        {
                            Id = data.Id,
                            Status = resp.IsSuccessStatusCode ? UploadStatus.Completed : UploadStatus.Failed
                        });
                    }
                }
                catch (Exception ex)
                {
                    if (data != null)
                    {
                        RaceResultTimeSent?.Invoke(this, new RaceResultTimeSentEventArgs()
                        {
                            Id = data.Id,
                            Status = UploadStatus.Failed
                        });
                    }
                    App.Logger.LogError("[RR] AUTO sending data failed", ex);
                }
            }
            else
            {
                _timer?.Stop();
            }
        }

        private async Task<HttpResponseMessage> SendData(DataModel d)
        {
            string time = d.Time.ToTimeSpan().TotalSeconds.ToString(CultureInfo.InvariantCulture)
                .Replace(',', '.');
            var conn = $"{_manualUrl}?&timingpoint={d.TimingPoint}&bib={d.Bib}&time={time}";
            var resp = await _httpClient.GetAsync(conn);
            return resp;
        }

        public void Dispose()
        {
            _timer?.Stop();
            _bibTimer?.Stop();
            _collectTimer?.Stop();
            _bibList.Clear();
            _unsentData.Clear();
            _httpClient.Dispose();
            App.Logger.Log("[RR] Stopping and disposing resources");
        }
    }
}
