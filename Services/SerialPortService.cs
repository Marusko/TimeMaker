using System.Collections.Concurrent;
using System.IO.Ports;
using System.Windows;
using TimeMaker.Models;
using TimeMaker.ViewModels;

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
        public override ConcurrentQueue<DataModel> DataQueue { get; protected set; } = new();
        public override ConcurrentQueue<DataModel> SentData { get; protected set; } = new();
        public override SourceItemViewModel SourceItemViewModel { get; protected set; } = new();

        private SerialPort _port = new();

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
                    Target = model.FirstTarget.Name,
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
            }
        }

        public override void Start()
        {
            throw new NotImplementedException();
        }

        public override List<DataModel> GetAllData()
        {
            throw new NotImplementedException();
        }

        public override List<DataModel> GetUnsentData()
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        public static List<string> GetAvailablePorts()
        {
            App.Logger.Log("[SS] Getting available ports");
            return SerialPort.GetPortNames().ToList();
        }

        public void TestConnection(string port)
        {
            try
            {
                App.Logger.Log($"[SS] Testing connection on port: {port}");
                Connect(port);
                Thread.Sleep(500);
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
            }
            catch (Exception e)
            {
                App.Logger.LogError($"[SS] Error connecting to port: {port}", e);
                MessageBox.Show(e.Message, "Varovanie - pripojenie", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
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
                MessageBox.Show(e.Message, "Varovanie - odpojenie", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
