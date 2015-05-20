using GalaSoft.MvvmLight;
using GeenenRFID;
using System.ComponentModel;
using System.Diagnostics;
using System.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace RFIDCounter.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private GeenenReader m_rfidReader = null;
        private CounterData m_counterData = null;
        List<string> m_allowedChips = null;
        private string m_piUrl = "";

        private int m_interval = 10;
        private int m_laps = 0;

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

        public bool isScanning
        {
            get
            {
                return m_rfidReader.isConnected();
            }
        }

        public bool Stopped
        {
            get
            {
                return !m_rfidReader.isConnected();
            }
        }

        public MainViewModel()
        {
            this.m_counterData = new CounterData();
            this.laps = this.m_counterData.laps;
            this.m_allowedChips = new List<string>();

            readConfig();
            connectRFID();
        }

        private void readConfig()
        {
            try
            {
                m_interval = Int32.Parse(ConfigurationManager.AppSettings["ChipInterval"]);

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

        private async void sendLapsToPi()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var json = "{\"lineA\":" + m_laps + ",\"lineB\":\"\"}";
                    var response = await client.PostAsync(m_piUrl, new StringContent(json));
                    var responseString = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
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

        void m_rfidReader_ReaderReceivedTags(object sender, RFIDEventArgs e)
        {
            var allowedTags = e.tags.Where(t => m_allowedChips.Contains(t));
            this.laps = m_counterData.addTags(allowedTags, m_interval);
        }
    }
}