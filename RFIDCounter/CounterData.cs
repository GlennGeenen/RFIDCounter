using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using System.Xml.Serialization;

namespace RFIDCounter
{
    public class CounterData
    {
        private string m_fileName = "save.xml";

        private List<TagData> tagDataList = new List<TagData>();
        private Timer m_timer = null;

        public int laps = 0;

        public CounterData()
        {
            deserialize();

            foreach (TagData data in tagDataList)
            {
                laps += data.seenCount;
            }

            m_timer = new Timer(10000);
            m_timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            m_timer.Enabled = true; 
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            serialize();
        }

        public int addTags(IEnumerable<String> tags, int interval)
        {
            DateTime now = DateTime.Now;

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
                        ++this.laps;
                    }
                }
                else
                {
                    tagDataList.Add(new TagData(chip, now));
                    Console.Beep();
                    ++this.laps;
                }
            }

            return this.laps;
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

        private void serialize()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<TagData>));
                using (TextWriter WriteFileStream = new StreamWriter(m_fileName))
                {
                    serializer.Serialize(WriteFileStream, tagDataList);
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
