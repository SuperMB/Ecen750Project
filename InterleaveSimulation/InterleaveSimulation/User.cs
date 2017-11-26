using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterleaveSimulation
{
    class User
    {
        public User(int userNumber)
        {
            _sectionsToInsert = new List<FileSection>();
            _largestPushReceived = null;
            UserNumber = userNumber;
        }

        public void Push()
        {
            if (_nextSectionToPush == null)
                return;

            User user = RandomContact();
            user.ReceivePush(_nextSectionToPush);
        }

        public void ReceivePush(FileSection fileSection)
        {
            if (_largestPushReceived == null)
                _largestPushReceived = fileSection;
            else if (fileSection.SectionNumber > _largestPushReceived.SectionNumber)
                _largestPushReceived = fileSection;

            if(!File.Completed)
                _sectionsToInsert.Add(fileSection);
        }

        public void MoveToOddSection()
        {
            InsertSections();
        }

        public void MoveToEvenSection()
        {
            _nextSectionToPush = _largestPushReceived;
            _largestPushReceived = null;
            InsertSections();
        }

        public void Pull()
        {
            if (File.Completed)
                return;

            //User user = RandomContact();
            foreach (User contact in ContactList)
            {
                if (Functions.Random.Next(2) == 1)
                {
                    FileSection fileSection = contact.ReceivePull(File.LowestSectionNotReceived);
                    if (fileSection != null)
                        _sectionsToInsert.Add(fileSection);
                }
            }
        }

        public FileSection ReceivePull(int sectionNumber)
        {
            if (File.HasSection(sectionNumber))
                return File.FileSections[sectionNumber];

            return null;
        }

        private void InsertSections()
        {
            foreach (FileSection section in _sectionsToInsert)
                File.InsertSection(section);

            _sectionsToInsert = new List<FileSection>();
        }

        private User RandomContact()
        {
            return ContactList[Functions.Random.Next(ContactList.Length)];
        }

        public File File { get; set; } //Set externally
        public User[] ContactList { get; set; } //Set externally
        public int UserNumber { get; set; }
        private FileSection _largestPushReceived;
        private FileSection _nextSectionToPush;
        private List<FileSection> _sectionsToInsert;
    }
}
