using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Windows;
using TimeMaker.Models;
using TimeMaker.ViewModels;
using TimeMaker.Windows;

namespace TimeMaker.Services
{
    public class SerialPortService : SourceService
    {
        public override string Id { get; protected set; } = Guid.NewGuid().ToString();
        public override Type InternalType { get; protected set; } = typeof(SerialPortService);
        public override string Name { get; protected set; } = string.Empty;
        public override string Type { get; protected set; } = string.Empty;
        public override string Source { get; protected set; } = string.Empty;
        public override string Target { get; protected set; } = string.Empty;
        public string TargetFinish { get; protected set; } = string.Empty;
        public string TargetRunTime { get; protected set; } = string.Empty;
        public TimyMode Mode { get; set; } = TimyMode.Stopwatch;
        public override int SentOk { get; protected set; }
        public override int SentError { get; protected set; }
        public override bool Running { get; protected set; }
        public override ConcurrentDictionary<string, DataModel> DataDictionary { get; protected set; } = new();
        public override ObservableCollection<DataLogViewModel> LogData { get; protected set; } = new();
        public override ConcurrentDictionary<string, DataLogViewModel> LogDataLookup { get; protected set; } = new();
        public override SourceItemViewModel SourceItemViewModel { get; protected set; } = new();

        private SerialPort _port = new();
        private ConcurrentDictionary<string, (Regex RegexManual, Regex RegexAuto, string TimingPoint)> _algePoints = new();
        private ConcurrentDictionary<string, (Regex RegexManual, Regex RegexAuto, string TimingPoint)> _algeClearPoints = new();
        private List<char> _impulseModifiers = [' ', '?', 'c', 'C', 'i'];
        private string _tempImpulse = "";

        public override void Init(SourceInitModel initModel)
        {
            if (initModel is SerialSourceInitModel model)
            {
                SourceItemViewModel = new SourceItemViewModel()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.Name,
                    Type = "Timy" + (model.Mode == TimyMode.Stopwatch ? " stopwatch" : " backup"),
                    Source = model.Source,
                    Target = $"C0(M): {model.FirstTarget.Name} C1(M): {model.SecondTarget.Name} RT(M): {model.ThirdTarget.Name}",
                    Status = "Pripravené",
                    IsRunning = false
                };
                Id = SourceItemViewModel.Id;
                Name = model.Name;
                Type = "Timy" + (model.Mode == TimyMode.Stopwatch ? " stopwatch" : " backup");
                Source = model.Source;
                Target = model.FirstTarget.Name;
                TargetFinish = model.SecondTarget.Name;
                TargetRunTime = model.ThirdTarget.Name;
                Mode = model.Mode;
                _impulseModifiers = model.Flags;
                SetPoints(Target, TargetFinish, TargetRunTime);
                SetClearPoints(Target, TargetFinish, TargetRunTime);
                App.Logger.Log($"[SS] Initialized SerialPortSource: {Id}");
            }
        }

        public override void Start()
        {
            if (!Running)
            {
                App.Logger.Log($"[SS] Starting SerialPortSource: {Id}");
                _port = new SerialPort();
                if (!Connect(Source))
                {
                    // Do not report the source as active when the port is dead.
                    SourceItemViewModel.UpdateStatus("Chyba - port sa nepodarilo otvoriť");
                    App.Logger.LogWarning($"[SS] SerialPortSource not started, port unavailable: {Id}");
                    return;
                }
                Running = true;
                SourceItemViewModel.IsRunning = true;
                SourceItemViewModel.UpdateStatus("Aktívne");
                SentError = 0;
                SentOk = 0;
                LogData.Clear();
                LogDataLookup.Clear();
                App.RaceResult.RaceResultTimeSent += OnRaceResultTimeSent;
                _port.DataReceived += Read;
                App.Logger.Log($"[SS] Started SerialPortSource: {Id}");
            }
        }

        private void OnRaceResultTimeSent(object? sender, RaceResultTimeSentEventArgs e)
        {
            if (e.SourceId.Equals(Id))
            {
                if (LogDataLookup.TryGetValue(e.Id, out var item))
                {
                    item.ChangeStatus(e.Status, e.StatusCode);
                }
                if (e.Status == UploadStatus.Completed)
                {
                    SentOk++;
                }
                else if (e.Status == UploadStatus.Failed)
                {
                    SentError++;
                }
                SourceItemViewModel.UpdateStatus($"Aktívne - OK: {SentOk}, Chyba: {SentError}");
            }
        }

        public override List<DataModel> GetUnsentData()
        {
            var unsent = new List<DataModel>(DataDictionary.Count);
            if (Running)
            {
                unsent.AddRange(DataDictionary.Values);
                DataDictionary.Clear();
            }
            App.Logger.Log($"[SS] Unsent data for SerialPortSource: {Id}, {unsent.Count} items");
            return unsent;
        }

        public override void ChangeTarget(TargetChangeModel target)
        {
            if (target is SerialTargetChangeModel model)
            {
                Target = model.Target;
                TargetFinish = model.SecondTarget;
                TargetRunTime = model.ThirdTarget;
                SetPoints(Target, TargetFinish, TargetRunTime);
                SetClearPoints(Target, TargetFinish, TargetRunTime);
                SourceItemViewModel.Target = $"C0(M): {model.Target} C1(M): {model.SecondTarget} RT(M): {model.ThirdTarget}";
            }
        }

        public override void Stop()
        {
            if (Running)
            {
                App.Logger.Log($"[SS] Stopping SerialPortSource: {Id}");
                Running = false;
                SourceItemViewModel.IsRunning = false;
                SourceItemViewModel.UpdateStatus($"Zastavené - OK: {SentOk}, Chyba: {SentError}");
                DataDictionary.Clear();
                App.RaceResult.RaceResultTimeSent -= OnRaceResultTimeSent;
                _port.DataReceived -= Read;
                Disconnect();
                App.Logger.Log($"[SS] Stopped SerialPortSource: {Id}");
            }
        }

        private Regex CreateCompiledRegex(string customRegex)
        {
            // Konvertujeme vlastný formát na štandardný regex
            // [n] -> (?<number>\d+) - zachytáva číslo do group "number"
            // [t] -> (?<time>\d{2}:\d{2}:\d{2}(?:\.\d{1,4})?) - zachytáva čas do group "time", desatinné miesta sekúnd sú voliteľné
            // . = ľubovoľný znak
            // \. = literálna bodka
            App.Logger.Log($"[SS] Creating regex for SerialPortSource: {Id}, Pattern: {customRegex}");
            string pattern = customRegex;
            string literalDotPlaceholder = "___LITERAL_DOT_PLACEHOLDER___";
            pattern = pattern.Replace(@"\.", literalDotPlaceholder);
            string wildcardDotPlaceholder = "___WILDCARD_DOT_PLACEHOLDER___";
            pattern = pattern.Replace(".", wildcardDotPlaceholder);
            pattern = Regex.Escape(pattern);
            pattern = pattern.Replace(@"\[n]", @"(?<number>\d+)");
            pattern = pattern.Replace(@"\[t]", @"(?<time>\d{2}:\d{2}:\d{2}(?:\.\d{1,4})?)");
            pattern = pattern.Replace(literalDotPlaceholder, @"\.");
            pattern = pattern.Replace(wildcardDotPlaceholder, ".");

            return new Regex(pattern, RegexOptions.Compiled);
        }

        private void SetPoints(string startPoint, string finishPoint, string runTimePoint)
        {
            // Backup mode impulses are prefixed with '*', stopwatch ones are not.
            var prefix = Mode == TimyMode.Backup ? "*" : "";
            try
            {
                App.Logger.Log($"[SS] Setting {Mode} points for SerialPortSource: {Id}");
                _algePoints.Clear();
                _algePoints.TryAdd("Start", (CreateCompiledRegex($"{prefix}[n] C0M [t] 00"), CreateCompiledRegex($"{prefix}[n] C0 [t] 00"), startPoint));
                _algePoints.TryAdd("Finish", (CreateCompiledRegex($"{prefix}[n] C1M [t] 00"), CreateCompiledRegex($"{prefix}[n] C1 [t] 00"), finishPoint));
                if (!string.IsNullOrEmpty(runTimePoint))
                {
                    _algePoints.TryAdd("RunTime", (CreateCompiledRegex($"{prefix}[n] RTM [t] 00"), CreateCompiledRegex($"{prefix}[n] RT [t] 00"), runTimePoint));
                }
            }
            catch (Exception e)
            {
                App.Logger.LogError($"[SS] Error setting {Mode} points", e);
                ThemedDialog.Show("Chyba impulzov", $"Nepodarilo sa nastaviť pravidlá pre impulzy. Prosím vymažte a znova pridajte tento zdroj. Chyba: [{e.Message}]", ThemedDialogIcon.Error);
            }
        }

        private void SetClearPoints(string startPoint, string finishPoint, string runTimePoint)
        {
            try
            {
                App.Logger.Log($"[SS] Setting clear points for SerialPortSource: {Id}");
                _algeClearPoints.Clear();
                _algeClearPoints.TryAdd("StartClear", (CreateCompiledRegex("c[n] C0M [t] 00"), CreateCompiledRegex("c[n] C0 [t] 00"), startPoint));
                _algeClearPoints.TryAdd("FinishClear", (CreateCompiledRegex("c[n] C1M [t] 00"), CreateCompiledRegex("c[n] C1 [t] 00"), finishPoint));
                _algeClearPoints.TryAdd("StartBigClear", (CreateCompiledRegex("C[n] C0M [t] 00"), CreateCompiledRegex("C[n] C0 [t] 00"), startPoint));
                _algeClearPoints.TryAdd("FinishBigClear", (CreateCompiledRegex("C[n] C1M [t] 00"), CreateCompiledRegex("C[n] C1 [t] 00"), finishPoint));
                if (!string.IsNullOrEmpty(runTimePoint))
                {
                    _algeClearPoints.TryAdd("RunTimeClear", (CreateCompiledRegex("c[n] RTM [t] 00"), CreateCompiledRegex("c[n] RT [t] 00"), runTimePoint));
                    _algeClearPoints.TryAdd("RunTimeBigClear", (CreateCompiledRegex("C[n] RTM [t] 00"), CreateCompiledRegex("C[n] RT [t] 00"), runTimePoint));
                }
            }
            catch (Exception e)
            {
                App.Logger.LogError("[SS] Error setting clear points", e);
                ThemedDialog.Show("Chyba mazacích impulzov", $"Nepodarilo sa nastaviť pravidlá pre mazacie impulzy. Prosím vymažte a znova pridajte tento zdroj. Chyba: [{e.Message}]", ThemedDialogIcon.Error);
            }
        }

        private void Read(object sender, SerialDataReceivedEventArgs ea)
        {
            try
            {
                if (_port.IsOpen)
                {
                    string str;
                    string complete = "";

                    str = _port.ReadExisting().Replace("\n", "").Replace("\r", "|");

                    if (!_tempImpulse.EndsWith("|"))
                    {
                        _tempImpulse += str;
                        if (_tempImpulse.EndsWith("|"))
                        {
                            complete = _tempImpulse;
                            _tempImpulse = "";
                            App.Logger.Log($"[SS] Read from serial port {Source}: {complete}");
                            // No DiscardInBuffer here - bytes of the next impulse
                            // may already be waiting and must not be thrown away.
                        }
                    }
                    else
                    {
                        complete = _tempImpulse;
                        _tempImpulse = "";
                        _tempImpulse += str;
                    }

                    if (!string.IsNullOrEmpty(complete))
                    {
                        var impulses = complete.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var impulse in impulses)
                        {
                            bool added = false;
                            if (_impulseModifiers.Contains(impulse[0]))
                            {
                                var parsed = ParseAlgeImpulse(impulse);
                                if (!string.IsNullOrEmpty(parsed))
                                {
                                    var d = StringToData(parsed, impulse);
                                    if (d != null)
                                    {
                                        added = true;
                                        var vm = new DataLogViewModel()
                                        {
                                            Id = d.Id,
                                            Raw = d.RawData,
                                            Bib = d.Bib,
                                            Time = d.Time,
                                            TimingPoint = d.TimingPoint,
                                            Status = UploadStatus.Pending,
                                            IsClear = d.IsClear,
                                            IsQuestion = d.IsQuestion
                                        };

                                        Application.Current.Dispatcher.Invoke(() => { LogData.Add(vm); });

                                        DataDictionary.AddOrUpdate(d.Id, d, (_, _) => d);
                                        LogDataLookup.AddOrUpdate(d.Id, vm, (_, _) => vm);
                                    }
                                }
                            }
                            if (!added)
                            {
                                var vm = new DataLogViewModel()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Raw = impulse,
                                    Bib = "",
                                    Time = TimeOnly.MinValue,
                                    TimingPoint = new ApiTimingPoint() { Name = "" },
                                    Status = UploadStatus.Ignored,
                                    IsClear = false
                                };
                                Application.Current.Dispatcher.Invoke(() => { LogData.Add(vm); });
                                LogDataLookup.AddOrUpdate(vm.Id, vm, (_, _) => vm);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ThemedDialog.Show("Chyba", $"Chyba pri čítaní zo sériového portu: {ex.Message}", ThemedDialogIcon.Warning);
                App.Logger.LogError($"[SS] Error reading from serial port {Source}", ex);
            }
        }

        private string? ParseAlgeImpulse(string impulse)
        {
            var str = Regex.Replace(impulse, @"\s+", " ").Trim();

            if (str.Length > 0)
            {
                return TryParseFromCollection(str, _algeClearPoints, true, false) ?? TryParseFromCollection(str, _algePoints, false, str[0] == '?');
            }
            return null;
        }

        private string? TryParseFromCollection(string str, IEnumerable<KeyValuePair<string, (Regex RegexManual, Regex RegexAuto, string TimingPoint)>> collection, bool isClear, bool isQuestion)
        {
            foreach (var v in collection)
            {
                if (string.IsNullOrEmpty(v.Value.TimingPoint))
                    continue;

                foreach (var regex in new[] { v.Value.RegexAuto, v.Value.RegexManual })
                {
                    var match = regex.Match(str);
                    if (match.Success)
                    {
                        return $"{match.Groups["number"].Value}|{match.Groups["time"].Value}|{v.Value.TimingPoint}|{isClear}|{isQuestion}";
                    }
                }
            }

            return null;
        }

        private DataModel? StringToData(string parsedImpulse, string raw)
        {
            var parts = parsedImpulse.Split('|');
            if (parts.Length == 5)
            {
                var number = parts[0];
                var timeStr = parts[1];
                var clear = parts[3];
                var question = parts[4];
                var can = TryParseTime(timeStr, out var parsed);
                var canBib = int.TryParse(number, out var parsedBib);
                var canClear = bool.TryParse(clear, out var parsedClear);
                var canQuestion = bool.TryParse(question, out var parsedQuestion);
                if (can && canBib && canClear && canQuestion)
                {
                    return new DataModel()
                    {
                        Id = Guid.NewGuid().ToString(),
                        SourceId = Id,
                        Bib = $"{parsedBib}",
                        Time = parsed,
                        TimingPoint = new ApiTimingPoint() { Name = parts[2] },
                        RawData = raw,
                        IsClear = parsedClear,
                        IsQuestion = parsedQuestion
                    };
                }
            }
            return null;
        }

        public static List<string> GetAvailablePorts()
        {
            try
            {
                App.Logger.Log("[SS] Getting available ports");
                return SerialPort.GetPortNames().ToList();
            }
            catch (Exception e)
            {
                App.Logger.LogError("[SS] Error getting available ports", e);
                ThemedDialog.Show("Chyba dostupných portov", $"Nepodarilo sa načítať dostupné porty: [{e.Message}]", ThemedDialogIcon.Error);
                return new List<string>();
            }
        }

        public bool TestConnection(string port)
        {
            try
            {
                App.Logger.Log($"[SS] Testing connection on port: {port}");
                if (!Connect(port))
                {
                    return false;
                }
                Thread.Sleep(500);
                return true;
            }
            finally
            {
                Disconnect();
            }
        }

        private bool Connect(string port)
        {
            try
            {
                App.Logger.Log($"[SS] Connecting to port: {port}");
                _port.PortName = port;
                _port.BaudRate = 9600;
                _port.Parity = Parity.None;
                _port.StopBits = StopBits.One;
                _port.Open();
                App.Logger.Log($"[SS] Connected to port: {port}");
                return true;
            }
            catch (Exception e)
            {
                App.Logger.LogError($"[SS] Error connecting to port: {port}", e);
                ThemedDialog.Show("Varovanie - pripojenie", e.Message, ThemedDialogIcon.Error);
                return false;
            }
        }

        private void Disconnect()
        {
            try
            {
                if (_port.IsOpen)
                {
                    App.Logger.Log($"[SS] Disconnecting from port: {_port.PortName}");
                    _port.Close();
                }
            }
            catch (Exception e)
            {
                App.Logger.LogError($"[SS] Error disconnecting from port: {_port.PortName}", e);
                ThemedDialog.Show("Varovanie - odpojenie", e.Message, ThemedDialogIcon.Error);
            }
        }
    }
}
