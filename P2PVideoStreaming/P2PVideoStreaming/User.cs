using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class User
    {
        public User(int userNumber, double uploadRate, double downloadRate, double videoRate)
        {
            UserNumber = userNumber;
            UploadBandwidth = uploadRate;
            DownloadBandwidth = downloadRate;
            ConsumedUploadBandwidth = 0;
            ConsumedDownloadBandwidth = 0;
            VideoRate = videoRate;
            _sectionsToInsert = new List<FileSection>();
            _downloadSectionsInProgress = new Dictionary<User, DownloadingFrom>();
            _uploadSectionsInProgress = new Dictionary<User, UploadingTo>();
        }

        public void ExecuteTimeSlotDownloads()
        {
            if (File.Completed)
                return;

            //Determine the start and stop of the window
            int start = File.LowestSectionNotReceived;
            int end = start + WindowWidth;
            if (end > File.NumberOfSections)
                end = File.NumberOfSections;

            //Within the window, determine which sections are needed, and weight them
            List<SectionWeight> neededSections = new List<SectionWeight>();
            int nextWeight = 1;
            int weightSum = 0;
            for(int i = end - 1; i >= start; i--)
                if (File.FileSections[i] == null)
                {
                    neededSections.Add(new SectionWeight
                    {
                        Section = i,
                        Weight = nextWeight
                    });
                    weightSum += nextWeight;
                    nextWeight++;
                }
            for (int i = 1; i < neededSections.Count; i++)
                neededSections[i].Weight += neededSections[i - 1].Weight;
            
            List<int> sectionsDownloading = new List<int>();
            while(ConsumedDownloadBandwidth < DownloadBandwidth && neededSections.Count > 0)
            {
                //generate a weight and find the section according to the range the weight falls into
                int weight = Functions.Random.Next(weightSum + 1);
                int position = 0;
                for (; position < neededSections.Count; position++)
                    if (weight <= neededSections[position].Weight)
                        break;
                int section = neededSections[position].Section;

                //Get contacts that have the section, and sort them based on available bandwidth
                List<User> contactsWithSection = ContactsWithSection(section);
                Dictionary<User, double> contactsAvailableUploadBandwidth = ContactsAvailableUploadBandwidth(contactsWithSection);
                List<User> sortedContacts = SortContactsOnBandwidth(contactsAvailableUploadBandwidth);

                //Download Section
                User contactToDownloadFrom = sortedContacts[0];
                if (contactToDownloadFrom.AvailableUploadBandwidth == 0)
                    DownloadSectionFromServer(section);
                else
                    DownloadSectionFromContact(contactToDownloadFrom, section);
            }
        }

        private void DownloadSectionFromServer(int section)
        {
            DownloadSectionFromContact(Functions.Server, section);
        }

        private void DownloadSectionFromContact(User contactToDownloadFrom, int section)
        {
            DownloadingFrom downloadingFrom = contactToDownloadFrom.RequestSection(this, section);
            if (downloadingFrom.PercentageDownloaded == 1)
            {
                ConsumedDownloadBandwidth += Functions.BandwidthNeededPerSection(VideoRate);
                _sectionsToInsert.Add(downloadingFrom.FileSection);
            }
            else
            {
                ConsumedDownloadBandwidth += Functions.BandwidthNeededPerSection(VideoRate) * downloadingFrom.PercentageDownloaded;
                _downloadSectionsInProgress[contactToDownloadFrom] = downloadingFrom;
            }
        }

        private List<User> ContactsWithSection(int section)
        {
            List<User> contactsWithSection = new List<User>();
            foreach (User contact in ContactList)
                if (contact.File.HasSection(section))
                    contactsWithSection.Add(contact);

            return contactsWithSection;
        }

        private Dictionary<User, double> ContactsAvailableUploadBandwidth(List<User> contacts)
        {
            Dictionary<User, double> contactsAvailableUploadBandwidth = new Dictionary<User, double>();
            foreach (User contact in contacts)
                contactsAvailableUploadBandwidth[contact] = contact.UploadBandwidth - contact.ConsumedUploadBandwidth;

            return contactsAvailableUploadBandwidth;
        }

        private List<User> SortContactsOnBandwidth(Dictionary<User, double> contactsBandwidth)
        {
            List<KeyValuePair<User, double>> contactsBandwidthList = contactsBandwidth.ToList();

            contactsBandwidthList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
            List<User> contactsSorted = new List<User>();
            foreach (KeyValuePair<User, double> pair in contactsBandwidthList)
                contactsSorted.Add(pair.Key);

            return contactsSorted;
        }


        public UploadingTo RequestSection(User user, int section)
        {
            //todo what if the download bandwidth for a node is less than the avialable upload bandwidth for this node

            if (!File.HasSection(section))
                return null;
            
            if (AvailableUploadBandwidth == 0)
                return null;

            double bandwidthNeeded = Functions.BandwidthNeededPerSection(VideoRate);
            bool uploadBottleneck = bandwidthNeeded > AvailableUploadBandwidth;
            bool downloadBottleneck = bandwidthNeeded > user.AvailableDownloadBandwidth;
            if(uploadBottleneck && downloadBottleneck)
            {
                uploadBottleneck = AvailableUploadBandwidth <= user.AvailableDownloadBandwidth;
                downloadBottleneck = !uploadBottleneck;
            }

            if(uploadBottleneck || downloadBottleneck)
            {
                double amountUploaded = uploadBottleneck ? AvailableUploadBandwidth : user.AvailableDownloadBandwidth;
                ConsumedUploadBandwidth += amountUploaded;
                UploadingTo uploadingTo = new UploadingTo
                {
                    FileSection = File.FileSections[section],
                    PercentageUploaded = amountUploaded / bandwidthNeeded
                };
                _uploadSectionsInProgress[user] = uploadingTo;
                return uploadingTo;
            }
            else
            {
                ConsumedUploadBandwidth += bandwidthNeeded;
                return new UploadingTo
                {
                    FileSection = File.FileSections[section],
                    PercentageUploaded = 1
                };
            }
        }

        //TODO: Finish this, also need to add logic for finishing uploading and downloading a file across a timeslot boundary
        public void UpdateAtEndOfTimeSlot()
        {
            foreach (FileSection fileSection in _sectionsToInsert)
                File.InsertSection(fileSection);
            _sectionsToInsert.RemoveAll(fileSection => true);
        }

        

        public double UploadBandwidth { get; set; }
        public double ConsumedUploadBandwidth { get; set; }
        public double AvailableUploadBandwidth
        {
            get
            {
                return UploadBandwidth - ConsumedUploadBandwidth;
            }
        }
        public double DownloadBandwidth { get; set; }
        public double ConsumedDownloadBandwidth { get; set; }
        public double AvailableDownloadBandwidth
        {
            get
            {
                return DownloadBandwidth - ConsumedDownloadBandwidth;
            }
        }

        public int UserNumber { get; set; }
        public double VideoRate { get; set; }
        public const int WindowWidth = 30;
        public File File { get; set; }
        public List<User> ContactList { get; set; }

        private List<FileSection> _sectionsToInsert;
        private Dictionary<User, DownloadingFrom> _downloadSectionsInProgress;
        private Dictionary<User, UploadingTo> _uploadSectionsInProgress;
    }
}
