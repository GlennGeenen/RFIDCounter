using GalaSoft.MvvmLight;
using GeenenRFID;
using System.Diagnostics;
using System.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Net;
using System.IO;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using System.Threading.Tasks;

namespace RFIDCounter.ViewModel
{
    public class MainViewModel : ViewModelBase
    {

        private GeenenReader m_rfidReader = null;
        private CounterData m_counterData = null;
        private List<string> m_allowedChips = null;
        private string m_piUrl = null;

        private static int s_interval = 10;
        private int m_laps = 0;
        private bool m_started = false;

        public RelayCommand StartCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }
        public RelayCommand ResetCommand { get; private set; }

        public int laps
        {
            get
            {
                return m_laps;
            }
            set
            {
                if (m_laps != value)
                {
                    m_laps = value;
                    sendLapsToPi();
                    RaisePropertyChanged("laps");
                }
            }
        }

        public bool Started
        {
            get
            {
                return m_started;
            }
            set
            {
                if (m_started != value)
                {
                    m_started = value;
                    RaisePropertyChanged("Started");
                    RaisePropertyChanged("Stopped");
                }
            }
        }

        public bool Stopped
        {
            get
            {
                return !m_started;
            }
        }

        public MainViewModel()
        {
            this.m_counterData = new CounterData();
            this.laps = this.m_counterData.m_laps;
            this.m_allowedChips = new List<string>();

            readConfig();

            StartCommand = new RelayCommand(start);
            StopCommand = new RelayCommand(stop);
            ResetCommand = new RelayCommand(reset);
        }

        private void readConfig()
        {
            try
            {
                s_interval = Int32.Parse(ConfigurationManager.AppSettings["ChipInterval"]);
                m_piUrl = "http://" + ConfigurationManager.AppSettings["Raspberry"] + ":" + ConfigurationManager.AppSettings["RaspberryPort"];

                string chips = ConfigurationManager.AppSettings["Chips"];
                string[] strArray = chips.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < strArray.Length; i++)
                {
                    m_allowedChips.Add(strArray[i]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void start()
        {
            connectRFID();
            m_counterData.start();
            this.Started = true;

            sendLapsToPi();
        }

        private void stop()
        {
            m_rfidReader.disconnect();
            m_counterData.stop();
            this.Started = false;
        }

        private void reset()
        {
            m_counterData.reset();
            this.laps = 0;
        }

        private void sendLapsToPi()
        {
            Task.Run(new Action(() => sendLaps(m_piUrl, m_laps)));
        }

        private static void sendLaps(string url, int myLaps)
        {
            if (url != null)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    var money = (myLaps / 10.0).ToString("0.00", CultureInfo.InvariantCulture);
                    var json = "{\"lineA\":{\"type\":\"number\",\"value\":" + myLaps + "},\"lineB\":{\"type\":\"money\",\"value\":" + money.ToString() + "}}";

                    byte[] byteArray = Encoding.UTF8.GetBytes(json);
                    request.ContentLength = byteArray.Length;
                    request.Timeout = 250;

                    Stream oStreamOut = request.GetRequestStream();
                    oStreamOut.Write(byteArray, 0, byteArray.Length);
                    oStreamOut.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private void connectRFID()
        {
            if (m_rfidReader == null)
            {
                m_rfidReader = new GeenenReader();
                m_rfidReader.ReaderReceivedTags += m_rfidReader_ReaderReceivedTags;
            }

            if (m_rfidReader.connect(ConfigurationManager.AppSettings["RFID"], UInt32.Parse(ConfigurationManager.AppSettings["RFIDPort"])))
            {
                Debug.WriteLine("RFID reader connected.");
            }
            else
            {
                Debug.WriteLine("RFID reader failed to connect.");
            }
        }

        private void setLapsInUI(int newLaps) 
        {
            this.laps = newLaps;
        }

        private static int DoWorkAsync(CounterData data, IEnumerable<string> tags, List<string> chips)
        {
            var allowedTags = tags.Where(t => chips.Contains(t));
            return data.addTags(allowedTags, s_interval);
        }

        private async void m_rfidReader_ReaderReceivedTags(object sender, RFIDEventArgs e)
        {
            var result = await Task.Run(() => DoWorkAsync(m_counterData, e.tags, m_allowedChips));
            if (result != -1)
            {
                App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => setLapsInUI(result)));
            }
        }

    }
}