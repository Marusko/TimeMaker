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
            _rawSearchUrl = "";
            _invalidateUrl = "";
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
            bool hasFirstPart = false;
            bool hasSecondPart = false;
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
                            evArgs.PointsApiLoaded = true;
                        }
                        else
                        {
                            App.Logger.LogWarning("[RR] Points API is off");
                            evArgs.PointsApiStatus = "Vypnuté";
                            evArgs.PointsApiLoaded = false;
                        }
                    }
                    else if (api.Label != null && api.Label.ToLower().Trim().Equals("manual"))
                    {
                        if (api.Disabled != null && !(bool)api.Disabled)
                        {
                            _manualUrl = link + api.Key;
                            App.Logger.Log("[RR] Successfully loaded Manual API");
                            evArgs.ManualApiStatus = "Načítané";
                            evArgs.ManualApiLoaded = true;
                        }
                        else
                        {
                            App.Logger.LogWarning("[RR] Manual API is off");
                            evArgs.ManualApiStatus = "Vypnuté";
                            evArgs.ManualApiLoaded = false;
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
                            evArgs.BibsApiLoaded = true;
                        }
                        else
                        {
                            App.Logger.LogWarning("[RR] Bibs API is off");
                            evArgs.BibsApiStatus = "Vypnuté";
                            evArgs.BibsApiLoaded = false;
                        }
                    }
                    else if (api.Label != null && api.Label.ToLower().Trim().Equals("invalid"))
                    {
                        if (api.Disabled != null && !(bool)api.Disabled)
                        {
                            _invalidateUrl = link + api.Key;
                            hasFirstPart = true;
                            App.Logger.Log("[RR] Successfully loaded Invalid API");
                            evArgs.InvalidApiStatus = "Načítané";
                            evArgs.InvalidApiLoaded = true;
                        }
                        else
                        {
                            hasFirstPart = false;
                            App.Logger.LogWarning("[RR] Invalid API is off");
                            evArgs.InvalidApiStatus = "Vypnuté";
                            evArgs.InvalidApiLoaded = false;
                        }
                    }
                    else if (api.Label != null && api.Label.ToLower().Trim().Equals("search"))
                    {
                        if (api.Disabled != null && !(bool)api.Disabled)
                        {
                            _rawSearchUrl = link + api.Key;
                            hasSecondPart = true;
                            App.Logger.Log("[RR] Successfully loaded Raw Search API");
                            evArgs.RawSearchApiStatus = "Načítané";
                            evArgs.RawSearchApiLoaded = true;
                        }
                        else
                        {
                            hasSecondPart = false;
                            App.Logger.LogWarning("[RR] Raw Search API is off");
                            evArgs.RawSearchApiStatus = "Vypnuté";
                            evArgs.RawSearchApiLoaded = false;
                        }
                    }
                }
                ClearEnabled = hasFirstPart && hasSecondPart;
                RaceResultApiLoaded?.Invoke(this, evArgs);
                App.Logger.Log("[RR] Successfully loaded APIs");
                if (!ClearEnabled)
                {
                    NotificationService.ShowInfoNotification("Zneplatnenie", "Automatické / ručné zneplatnenie je vypnuté");
                }
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

        public void AddManual(DataModel model)
        {
            App.Logger.Log($"[RR] Adding manual data - {model.Id}");
            _unsentData.Enqueue(model);
            if (!_unsentData.IsEmpty)
            {
                if (_timer is { IsEnabled: false })
                {
                    _timer.Start();
                }
            }
        }

        private void CollectData(object? sender, EventArgs e)
        {
            App.Logger.Log("[RR] AUTO collecting data...");
            try
            {
                foreach (var source in App.SourceManager.Sources)
                {
                    if (source.Value.Running)
                    {
                        if (!source.Value.DataDictionary.IsEmpty)
                        {
                            var unsent = source.Value.GetUnsentData();
                            foreach (var data in unsent)
                            {
                                _unsentData.Enqueue(data);
                            }
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
            catch (Exception ex)
            {
                App.Logger.LogError("[RR] AUTO data collection failed", ex);
            }
        }

        private async void SendDataToRr(object? sender, EventArgs e)
        {
            if (!_unsentData.IsEmpty)
            {
                App.Logger.Log("[RR] AUTO sending data...");
                var data = _unsentData.TryDequeue(out var item) ? item : null;
                try
                {
                    if (data != null)
                    {
                        HttpResponseMessage resp;
                        resp = ClearEnabled ? (data.IsClear ? await SendClearData(data) : await SendData(data)) : (data.IsClear ? new HttpResponseMessage(HttpStatusCode.FailedDependency) : await SendData(data));
                        RaceResultTimeSent?.Invoke(this, new RaceResultTimeSentEventArgs()
                        {
                            Id = data.Id,
                            SourceId = data.SourceId,
                            Status = resp.IsSuccessStatusCode ? UploadStatus.Completed : UploadStatus.Failed,
                            StatusCode = resp.StatusCode.ToString()
                        });
                        if (!resp.IsSuccessStatusCode)
                        {
                            App.Logger.LogError($"[RR] AUTO sending data failed - {resp.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (data != null)
                    {
                        RaceResultTimeSent?.Invoke(this, new RaceResultTimeSentEventArgs()
                        {
                            Id = data.Id,
                            SourceId = data.SourceId,
                            Status = UploadStatus.Failed,
                            StatusCode = ex.Message
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
            string time = d.Time.ToTimeSpan().TotalSeconds.ToString(CultureInfo.InvariantCulture).Replace(',', '.');
            var conn = $"{_manualUrl}?&timingpoint={d.TimingPoint.Name}&bib={d.Bib}&time={time}";
            var resp = await _httpClient.GetAsync(conn);
            return resp;
        }

        private async Task<HttpResponseMessage> SendClearData(DataModel d)
        {
            string time = d.Time.ToTimeSpan().TotalSeconds.ToString(CultureInfo.InvariantCulture).Replace(',', '.');
            var conn = $"{_rawSearchUrl}?&bib={d.Bib}";
            var resp = await _httpClient.GetAsync(conn);
            if (resp.IsSuccessStatusCode)
            {
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

                if (results != null)
                {
                    var single = (from r in results where r.TimingPoint == d.TimingPoint.Name && r.Time.Equals(time) select r).FirstOrDefault();
                    if (single != null)
                    {
                        var invConn = $"{_invalidateUrl}?&id={single.Id}&invalid=true";
                        var invResp = await _httpClient.GetAsync(invConn);
                        return invResp;
                    }
                    else
                    {
                        return new HttpResponseMessage(HttpStatusCode.NotFound);
                    }
                }
                else
                {
                    App.Logger.LogError("[RR] Cannot load timing data from API - results list is null");
                    throw new HttpRequestException("Neúspešné načítanie časových dát\nChyba: \n[Impulzy sú null]");
                }
            }
            return resp;
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
