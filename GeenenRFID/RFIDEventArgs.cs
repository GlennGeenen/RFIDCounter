using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeenenRFID
{
    public class RFIDEventArgs
    {
        public IEnumerable<string> tags { get; private set; }

        public RFIDEventArgs(IEnumerable<string> tags)
        {
            this.tags = tags;
        }
    }
}
