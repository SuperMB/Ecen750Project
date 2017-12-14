using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    static class Functions
    {
        static Functions()
        {
            Random = new Random(4);
        }

        public static double BandwidthNeededPerSection(double videoRate)
        {
            return videoRate * TimePerSlot;
        }

        public static Random Random { get; set; }
        public const double TimePerSlot = .2;
        
    }
}
