using GeenenRFID;
using System.ComponentModel;
using System.Diagnostics;

namespace RFIDCounter
{
    class MainViewModel: INotifyPropertyChanged
    {
        private GeenenReader m_rfidReader = null;
        private CounterData m_counterData = null;

        private bool m_connected = false;
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
                    RaisePropertyChanged("laps");
                }
            }
        }

        public MainViewModel()
        {
            this.m_counterData = new CounterData();
            this.laps = this.m_counterData.laps;
            
            connectRFID();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void connectRFID()
        {
            if (m_rfidReader == null)
            {
                m_rfidReader = new GeenenReader();
                m_rfidReader.ReaderReceivedTags += m_rfidReader_ReaderReceivedTags;
            }

            if (m_rfidReader.connect("FX75007F3771", 5084))
            {
                this.m_connected = true;
            }
            else
            {
                Debug.WriteLine("Failed to connect.");
                this.m_connected = false;
            }
        }

        void m_rfidReader_ReaderReceivedTags(object sender, RFIDEventArgs e)
        {
            this.laps = m_counterData.addTags(e.tags, m_interval);
        }

    }
}
