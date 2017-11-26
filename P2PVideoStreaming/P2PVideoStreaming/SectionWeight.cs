using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class SectionWeight
    {
        public SectionWeight()
        {
        }

        public SectionWeight(int section, double weight)
        {
            Section = section;
            Weight = weight;
        }

        public int Section { get; set; }
        public double Weight { get; set; }
    }
}
