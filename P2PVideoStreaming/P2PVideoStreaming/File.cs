using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class File
    {
        public File(int numberOfSections)
        {
            NumberOfSections = numberOfSections;
            FileSections = new FileSection[NumberOfSections];
            LowestSectionNotReceived = 0;
            Completed = false;
        }

        public bool HasSection(FileSection fileSection)
        {
            return FileSections[fileSection.SectionNumber] != null;
        }

        public bool HasSection(int sectionNumber)
        {
            return FileSections[sectionNumber] != null;
        }

        public void InsertSection(FileSection fileSection)
        {
            if (fileSection == null)
                return;

            if (FileSections[fileSection.SectionNumber] == null)
                FileSections[fileSection.SectionNumber] = fileSection;

            if (fileSection.SectionNumber == LowestSectionNotReceived)
            {
                LowestSectionNotReceived++;
                while (LowestSectionNotReceived < FileSections.Length ? FileSections[LowestSectionNotReceived] != null : false)
                    LowestSectionNotReceived++;
            }

            if (LowestSectionNotReceived == FileSections.Length)
                Completed = true;
        }

        public int NumberOfSections { get; set; }
        public FileSection[] FileSections { get; set; }
        public int LowestSectionNotReceived { get; set; }
        public bool Completed { get; private set; }
    }
}
