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

            TimePlayed = 0;
            TimeDelayed = 0;
            TimeBuffering = 0;
            VideoPosition = 0;
            ConsecutiveNegativeBalance = 0;
            ConsecutivePositiveBalance = 0;
            _buffering = true;
            PlayBackFinished = false;

            AmountDownloaded = 0;
            AmountUploaded = 0;

            _uploadList = new List<double>();

            Balance = 0;
            FreshBalance();
            _sectionsToInsert = new List<FileSection>();
            _downloadSectionsInProgress = new Dictionary<User, DownloadingFrom>();
            //_uploadSectionsInProgress = new Dictionary<User, UploadingTo>();
        }

        public void FreshBalance()
        {
            //Balance = VideoRate;
            if (Balance < 0)
                Balance = 0;
            else
                Balance /= 2;

            ConsecutiveNegativeBalance = 0;
            _uploadList = new List<double>();
        }

        public void ExecuteTimeSlotDownloads()
        {
            if (File.Completed)
                return;

            List<int> previousDownloadSectionsInProgress = new List<int>();
            foreach (User user in _downloadSectionsInProgress.Keys)
                previousDownloadSectionsInProgress.Add(_downloadSectionsInProgress[user].FileSection.SectionNumber);
            ContinueDownloadsFromPreviousTimeSlot();
            if (AvailableDownloadBandwidth == 0)
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
                if (File.FileSections[i] == null && !previousDownloadSectionsInProgress.Contains(i))
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
            while(ConsumedDownloadBandwidth < DownloadBandwidth && neededSections.Count > 0)// && Functions.Server.AvailableUploadBandwidth > 0)
            {
                //generate a weight and find the section according to the range the weight falls into
                int weight = Functions.Random.Next(weightSum + 1);
                int position = 0;
                for (; position < neededSections.Count; position++)
                    if (weight <= neededSections[position].Weight)
                        break;
                int section = neededSections[position].Section;

                double amountToSubtract = 0;
                if (position == 0)
                    amountToSubtract = neededSections[position].Weight;
                else
                    amountToSubtract = neededSections[position].Weight - neededSections[position - 1].Weight;
                if (position < neededSections.Count - 1)
                    for (int i = position + 1; i < neededSections.Count; i++)
                        neededSections[i].Weight -= amountToSubtract;
                neededSections.RemoveAt(position);
                if(neededSections.Count > 0)
                    weightSum = (int)neededSections.Last().Weight;

                //Get contacts that have the section, and sort them based on available bandwidth
                List<User> contactsWithSection = ContactsWithSection(section);
                Dictionary<User, double> contactsAvailableUploadBandwidth = ContactsAvailableUploadBandwidth(contactsWithSection);
                List<User> sortedContacts = SortContactsOnBandwidth(contactsAvailableUploadBandwidth);

                //Download Section
                if (sortedContacts.Count > 0)
                {
                    User contactToDownloadFrom = sortedContacts[0];
                    if (contactToDownloadFrom.AvailableUploadBandwidth == 0)
                        DownloadSectionFromServer(section);
                    else
                        DownloadSectionFromContact(contactToDownloadFrom, section, 1);
                }
                else
                    DownloadSectionFromServer(section);
            }
        }

        private void ContinueDownloadsFromPreviousTimeSlot()
        {
            //copy and clear out the download sections in progress
            Dictionary<User, DownloadingFrom> previousDownloadSectionsInProgress = new Dictionary<User, DownloadingFrom>();
            foreach (User user in _downloadSectionsInProgress.Keys)
                previousDownloadSectionsInProgress[user] = _downloadSectionsInProgress[user];
            _downloadSectionsInProgress = new Dictionary<User, DownloadingFrom>();

            if (previousDownloadSectionsInProgress.Count > 0)
                for (int i = previousDownloadSectionsInProgress.Count - 1; i >= 0; i--)
                {
                    if (ConsumedDownloadBandwidth < DownloadBandwidth)
                    {
                        User user = previousDownloadSectionsInProgress.Keys.ElementAt(i);
                        DownloadingFrom downloadingFrom = previousDownloadSectionsInProgress[user];
                        double percentage = DownloadSectionFromContact(
                            user,
                            downloadingFrom.FileSection.SectionNumber,
                            1 - downloadingFrom.PercentageDownloaded
                            );
                        if (percentage != 1 - downloadingFrom.PercentageDownloaded)
                        {
                            downloadingFrom.PercentageDownloaded += percentage;
                            _downloadSectionsInProgress[user] = downloadingFrom;
                        }
                    }
                }

        }

        private void DownloadSectionFromServer(int section)
        {
            double serverConsumedUploadBandwidth = Functions.Server.ConsumedUploadBandwidth;
            DownloadSectionFromContact(Functions.Server, section, 1);
            serverConsumedUploadBandwidth = Functions.Server.ConsumedUploadBandwidth - serverConsumedUploadBandwidth;
            Functions.ServerConsumedBandwidth[UserGroup] += serverConsumedUploadBandwidth;
        }

        private double DownloadSectionFromContact(User contactToDownloadFrom, int section, double percentage)
        {
            DownloadingFrom downloadingFrom = contactToDownloadFrom.RequestSection(this, section, percentage, VideoRate);
            if (downloadingFrom == null)
                return 0;

            if (downloadingFrom.PercentageDownloaded == percentage)
            {
                ConsumedDownloadBandwidth += Functions.BandwidthNeededPerSection(VideoRate) * percentage;
                _sectionsToInsert.Add(downloadingFrom.FileSection);
                return percentage;
            }
            else
            {
                ConsumedDownloadBandwidth += Functions.BandwidthNeededPerSection(VideoRate) * downloadingFrom.PercentageDownloaded;
                _downloadSectionsInProgress[contactToDownloadFrom] = downloadingFrom;
                return downloadingFrom.PercentageDownloaded;
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
            contactsSorted.Reverse();

            return contactsSorted;
        }

        public UploadingTo RequestSection(User user, int section, double percentage, double videoRate)
        {
            if (!File.HasSection(section))
                return null;

            //if (UserNumber < 0
            //    && Functions.TimesFileServed[user.UserGroup][section] >= Functions.MaxServerDistributionPerSection
            //    && percentage == 1)
            //    return null;

            if (AvailableUploadBandwidth == 0)
                return null;

            double bandwidthNeeded = Functions.BandwidthNeededPerSection(videoRate) * percentage;
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
                    PercentageUploaded = (amountUploaded / bandwidthNeeded) * percentage
                };
                //_uploadSectionsInProgress[user] = uploadingTo;
                return uploadingTo;
            }
            else
            {
                ConsumedUploadBandwidth += bandwidthNeeded;

                if (UserNumber < 0)
                {
                    //Functions.TimesFileServedList[section].Add(user.UserNumber);
                    Functions.TimesFileServed[user.UserGroup][section]++;
                }

                return new UploadingTo
                {
                    FileSection = File.FileSections[section],
                    PercentageUploaded = percentage
                };
            }
        }

        //TODO: Finish this, also need to add logic for finishing uploading and downloading a file across a timeslot boundary
        public void UpdateAtEndOfTimeSlot()
        {
            foreach (FileSection fileSection in _sectionsToInsert)
                File.InsertSection(fileSection);
            _sectionsToInsert.RemoveAll(fileSection => true);

            UpdateBalance();

            AmountDownloaded += ConsumedDownloadBandwidth;
            AmountUploaded += ConsumedUploadBandwidth;
            if (_uploadList.Count >= Functions.SamplesToAverage)
                _uploadList.RemoveAt(0);
            _uploadList.Add(ConsumedUploadBandwidth);

            ConsumedDownloadBandwidth = 0;
            ConsumedUploadBandwidth = 0;
        }

        public void PlayTimeSlot()
        {
            if(_buffering)
                if(FinishedBuffering())
                    _buffering = false;
                else 
                    TimeBuffering += Functions.TimePerSlot;

            if (!PlayBackFinished & !_buffering)
                if(File.HasSection(VideoPosition))
                {
                    VideoPosition++;
                    TimePlayed += Functions.TimePerSlot;
                    if (VideoPosition == File.NumberOfSections)
                        PlayBackFinished = true;
                }
                else
                    TimeDelayed += Functions.TimePerSlot;
        }

        public bool FinishedBuffering()
        {
            if (!_buffering)
                return true; 

            for (int i = 0; i < LoadAmountTillPlay / Functions.TimePerSlot; i++)
                if (!File.HasSection(i))
                    return false;

            return true;
        }

        public void UpdateBalance()
        {
            if (this != Functions.Server && _uploadList.Count >= Functions.SamplesToAverage && !PlayBackFinished)
            {
                //Balance += /*2 * Math.Ceiling(Math.Log(UserGroup.Size) / Math.Log(2)) * VideoRate * Functions.TimePerSlot / UserGroup.Size*/ - Functions.ServerConsumedBandwidth[UserGroup] / UserGroup.Size + UserGroup.DataUploadedThisTimeSlot / UserGroup.Size;// - ConsumedDownloadBandwidth; // * UploadBandwidth / DownloadBandwidth;
                //Balance += UserGroup.DataUploadedThisTimeSlot / UserGroup.Size - Functions.ServerConsumedBandwidth[UserGroup] == 0 ? 0 : Math.Log(Functions.ServerConsumedBandwidth[UserGroup]);// / UserGroup.Size;
                //Balance += .85*ConsumedUploadBandwidth - AdjustedLog(Functions.ServerConsumedBandwidth[UserGroup] + 1) / (AdjustedLog(UserGroup.Size) + 1);// / UserGroup.Size;
                //Balance += 1.3 * ((AdjustedLog(UserGroup.DataUploadedThisTimeSlot) - AdjustedLog(Math.Sqrt(VideoRate) * Functions.ServerConsumedBandwidth[UserGroup])) / Math.Sqrt(UserGroup.Size));
                double weight = VideoRate * .75; // 1.2 * Math.Sqrt(VideoRate);
                //if (weight < 1)
                //    weight = 1;
                double value = (SamplesAverage() - weight * Functions.ServerConsumedBandwidth[UserGroup] / UserGroup.Size);
                double negative = 1;
                if(value < 0)
                {
                    negative = -1;
                    value = Math.Sqrt(-value);
                }
                Balance += negative * value;

                if (Balance < 0)
                    ConsecutiveNegativeBalance++;
                else
                    ConsecutiveNegativeBalance = 0;

                if (Balance > 0)
                    ConsecutivePositiveBalance++;
                else
                    ConsecutivePositiveBalance = 0;                
            }
        }

        private double SamplesAverage()
        {
            double sum = 0;
            foreach (double sample in _uploadList)
                sum += sample;

            return sum / _uploadList.Count;
        }

        public double AdjustedLog(double number)
        {
            if (number == 0)
                return 0;

            return Math.Log(number);
        }

        public double UploadBandwidth { get; set; }
        public double ConsumedUploadBandwidth { get; set; }
        public double PreviousConsumedUploadBandwidth { get; set; }
        public double AvailableUploadBandwidth
        {
            get
            {
                return UploadBandwidth - ConsumedUploadBandwidth;
            }
        }
        public double DownloadBandwidth { get; set; }
        public double ConsumedDownloadBandwidth { get; set; }
        public double PreviousConsumedDownloadBandwidth { get; set; }
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
        public double Balance { get; set; }
        public double TimePlayed { get; set; }
        public double TimeDelayed { get; set; }
        public double TimeBuffering { get; set; }
        public int VideoPosition { get; set; }
        public bool PlayBackFinished { get; set; }
        public const double LoadAmountTillPlay = 4;
        public UserGroup UserGroup { get; set; }
        public int ConsecutiveNegativeBalance { get; set; }
        public int ConsecutivePositiveBalance { get; set; }

        public double AmountDownloaded { get; set; }
        public double AmountUploaded { get; set; }

        private bool _buffering;
        private List<FileSection> _sectionsToInsert;
        private Dictionary<User, DownloadingFrom> _downloadSectionsInProgress;
        private List<double> _uploadList;
        
        //private Dictionary<User, UploadingTo> _uploadSectionsInProgress;
    }
}
