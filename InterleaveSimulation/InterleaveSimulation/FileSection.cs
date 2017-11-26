using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterleaveSimulation
{
    class FileSection
    {
        public FileSection(int sectionNumber)
        {
            SectionNumber = sectionNumber;
        }
        
        public int SectionNumber { get; set; }
    }
}
