using System;
using System.Linq;

using Symbol.RFID3;
using System.Diagnostics;

namespace GeenenRFID
{
    public class GeenenReader
    {

        private RFIDReader m_reader = null;
        public event EventHandler<RFIDEventArgs> ReaderReceivedTags;

        public GeenenReader()
        {

        }

        public bool isConnected()
        {
            return m_reader.IsConnected;
        }

        public bool connect(string ip, uint port)
        {
            m_reader = new RFIDReader(ip, port, 0);
           
            try
            {
                m_reader.Connect();
                configureReader();
                m_reader.Actions.Inventory.Perform(null, null, null);
            }
            catch (OperationFailureException operationException)
            {
                Debug.WriteLine(operationException.StatusDescription);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RFID Connect failed: " + ex.Message);
                return false;
            }

            return true;
        } 

        public void disconnect()
        {
            if (m_reader.Actions.TagAccess.OperationSequence.Length > 0)
            {
                m_reader.Actions.TagAccess.OperationSequence.StopSequence();
            }
            else
            {
                m_reader.Actions.Inventory.Stop();
            }
            m_reader.Disconnect();
        }

        private void configureReader()
        {
            m_reader.Events.ReadNotify += new Events.ReadNotifyHandler(Events_ReadNotify);
            m_reader.Events.AttachTagDataWithReadEvent = false;

            m_reader.Events.StatusNotify += new Events.StatusNotifyHandler(Events_StatusNotify);
            m_reader.Events.NotifyGPIEvent = true;
            m_reader.Events.NotifyBufferFullEvent = true;
            m_reader.Events.NotifyBufferFullWarningEvent = true;
            m_reader.Events.NotifyReaderDisconnectEvent = true;
            m_reader.Events.NotifyReaderExceptionEvent = true;
        }

        private void Events_ReadNotify(object sender, Events.ReadEventArgs e)
        {
            try
            {
                Symbol.RFID3.TagData[] tagData = m_reader.Actions.GetReadTags(1000);
                if (tagData != null)
                {
                    var unique_tags = (from tag in tagData select tag.TagID).Distinct();
                    ReaderReceivedTags(this, new RFIDEventArgs(unique_tags));
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void Events_StatusNotify(object sender, Events.StatusEventArgs e)
        {
            switch (e.StatusEventData.StatusEventType)
            {
                case Symbol.RFID3.Events.STATUS_EVENT_TYPE.BUFFER_FULL_EVENT:
                    Debug.WriteLine("RFID READER BUFFER FULL");
                    break;
                case Symbol.RFID3.Events.STATUS_EVENT_TYPE.DISCONNECTION_EVENT:
                    Debug.WriteLine(e.StatusEventData.DisconnectionEventData.DisconnectEventInfo);
                    m_reader.Reconnect();
                    break;
                case Symbol.RFID3.Events.STATUS_EVENT_TYPE.READER_EXCEPTION_EVENT:
                    Debug.WriteLine(e.StatusEventData.ReaderExceptionEventData.ReaderExceptionEventInfo);
                    break;
                default:
                    break;
            }

        }

    }
}
