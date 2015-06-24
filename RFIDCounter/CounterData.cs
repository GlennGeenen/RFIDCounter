using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace RFIDCounter
{
    public class CounterData
    {
        private string m_fileName = "save.xml";

        private List<TagData> tagDataList = new List<TagData>();
        private System.Timers.Timer m_timer = null;

        public int m_laps = 0;

        public CounterData()
        {
            deserialize();

            foreach (TagData data in tagDataList)
            {
                m_laps += data.seenCount;
            }

            m_timer = new System.Timers.Timer(30000);
            m_timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Task.Run(new Action(() => serialize(m_fileName, tagDataList)));
        }

        public void start()
        {
            m_timer.Enabled = true; 
        }

        public void stop()
        {
            m_timer.Enabled = false; 
        }

        public void reset()
        {
            string filename = @"saves\" + DateTime.Now.ToString("dd-MM-yyyyTH-mm-ss") + ".xml";
            serialize(filename, tagDataList);
            tagDataList = new List<TagData>();
            serialize(m_fileName, tagDataList);
        }

        public int addTags(IEnumerable<String> tags, int interval)
        {
            DateTime now = DateTime.Now;

            int oldLaps = m_laps;

            TagData tag = null;
            foreach (var chip in tags)
            {
                tag = getChip(chip);
                if(tag != null)
                {
                    if ((now - tag.lastSeen).TotalSeconds > interval)
                    {
                        tag.lastSeen = now;
                        ++tag.seenCount;
                        Console.Beep();
                        ++m_laps;
                    }
                }
                else
                {
                    tagDataList.Add(new TagData(chip, now));
                    Console.Beep();
                    ++m_laps;
                }
            }

            if (oldLaps != m_laps)
            {
                return m_laps;
            }
            else
            {
                return -1;
            }
        }

        private TagData getChip(string chip)
        {
            for (int i = tagDataList.Count - 1; i >= 0; --i)
            {
                if (tagDataList[i].tag == chip)
                {
                    return tagDataList[i];
                }
            }
            return null;
        }

        private static void serialize(string fileName, List<TagData> dataList)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<TagData>));
                using (TextWriter WriteFileStream = new StreamWriter(fileName))
                {
                    serializer.Serialize(WriteFileStream, dataList);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void deserialize()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<TagData>));
                using (TextReader readFileStream = new StreamReader(m_fileName))
                {
                    tagDataList = (List<TagData>)serializer.Deserialize(readFileStream);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

    [Serializable()]
    public class TagData 
    {
        public string tag;
        public int seenCount;
        public DateTime lastSeen;

        public TagData()
        {
        }

        public TagData(string chip, DateTime time)
        {
            this.tag = chip;
            this.lastSeen = time;
            this.seenCount = 1;
        }
    }
}
