using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessAWS
{
    public class JSON
    {
    public string StationNo { get; set; }
    public int ProjectID { get; set; }

    public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Seconds { get; set; }
        public float Value { get; set; }

        public long SearchDatetime { get; set; }
    }
}
