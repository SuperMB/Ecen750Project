using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class Network
    {
        public Network(int contactListSize)
        {
            _numberOfSections = (int)(_videoLength / Functions.TimePerSlot);
            ServerConsumedBandwidth = new Dictionary<UserGroup, double>();
            GroupHasBeenLowered = false;
            GroupHasBeenRaised = false;

            ContactListSize = contactListSize;
            GroupSizeToDrop = ContactListSize;
        }

        public void RunSimulation()
        {
            _finished = false;
            _timeSlot = 0;

            _usersUploadTotal = 0;
            _usersDownloadTotal = 0;
            _serverUploadTotal = 0;

            MakeUsers();
            CreateUserGroups();
            CreateContactLists();
            CreateServer();

            DistributeFile();
            Console.Out.WriteLine("Finished");
            Console.Out.WriteLine($"Final Time Slot: {_timeSlot}.");
            Console.Out.WriteLine($"User Total Download Bandwidth: {_usersDownloadTotal}.");
            Console.Out.WriteLine($"User Total Upload Bandwidth: {_usersUploadTotal}, {_usersUploadTotal / _usersDownloadTotal}.");
            Console.Out.WriteLine($"Server Total Upload Bandwidth: {_serverUploadTotal}, {_serverUploadTotal / _usersDownloadTotal}.");

            double delay = CalculateDelay();
            Console.Out.WriteLine($"Total Delay: {delay}.");
            Console.Out.WriteLine($"Average Delay: {delay / NumberOfUsers}.");
            double timeBuffering = CalculateTimeBuffering();
            Console.Out.WriteLine($"Total Buffer Time: {timeBuffering}.");
            Console.Out.WriteLine($"Average Buffer Time: {timeBuffering / NumberOfUsers}.");
            double balance = CalculateBalance();
            Console.Out.WriteLine($"Total Balance: {balance}.");
            Console.Out.WriteLine($"Average Balance: {balance / NumberOfUsers}.");

            PrintUserGroupTotals();

            //foreach (User user in Functions.Users)
            //Console.Out.WriteLine($"User {user.UserNumber} - {user.AmountDownloaded}");
        }

        private void PrintUserGroupTotals()
        {
            //return;

            foreach (UserGroup userGroup in UserGroups)
            {
                Console.Out.WriteLine($"User Group {userGroup.VideoRate} Count: {userGroup.Users.Count}, AverageUploadBandwidth: {userGroup.AverageUploadBandwidth()}, Balance: {userGroup.AverageBalance()}");
            }
            Console.Out.WriteLine();
        }

        bool _noChange = false;
        bool _print = false;
        int _startingRate = 3;

        private void MakeUsers()
        {
            Users = new User[NumberOfUsers];
            int sections = 2;
            for (int i = 0; i < sections; i++)
                for (int j = 0; j < Users.Length / sections; j++)
                {
                    int position = i * Users.Length / sections + j;
                    Users[position] = new User(
                        position,
                        _startingRate * Functions.TimePerSlot,
                        //(2 + i * 6) * Functions.TimePerSlot,
                        15 * Functions.TimePerSlot,
                        -1,// Gets reset later
                        this
                        )
                    {
                        File = new File(_numberOfSections)
                    };
                }
        }

        private void CreateContactLists()
        {
            foreach (User user in Users)
                user.ContactList = new List<User>();

            foreach (User user in Users)
                CreateNewContactList(user);
        }

        private void CreateServer()
        {
            _server = new User(
                -1,
                1000 * Functions.TimePerSlot,
                -1, //doesn't matter
                1,
                this
                )
            {
                File = new File(_numberOfSections)
            };
            for (int i = 0; i < _numberOfSections; i++)
                _server.File.InsertSection(new FileSection(i));
            Server = _server;
            TimesFileServedList = new List<int>[_numberOfSections];
            for (int i = 0; i < _numberOfSections; i++)
                TimesFileServedList[i] = new List<int>();
        }

        private void CreateUserGroups()
        {
            UserGroups = new List<UserGroup>();

            foreach (double videoRate in VideoRates)
                UserGroups.Add(new UserGroup(videoRate, this));

            foreach (User user in Users)
                UserGroups[0].AddUser(user);

            foreach (UserGroup userGroup in UserGroups)
                ServerConsumedBandwidth[userGroup] = 0;

            TimesFileServed = new Dictionary<UserGroup, int[]>();
            foreach (UserGroup userGroup in UserGroups)
                TimesFileServed[userGroup] = new int[_numberOfSections];
        }

        private bool ValidateUserGroups()
        {
            foreach (UserGroup userGroup in UserGroups)
                if (!userGroup.ValidateContactLists())
                    return false;

            return true;
        }

        private void DistributeFile()
        {
            while (!(_timeSlot == .5 * _videoLength))//Finished())
            {
                if (_timeSlot % 50 == 0)
                {
                    //Console.Out.WriteLine(ValidateUserGroups());
                    if(_print)
                        PrintUserGroupTotals();
                }

                User[] randomizedUsers = Users.OrderBy(user => Functions.Random.Next()).ToArray();
                foreach (User user in randomizedUsers)
                    user.ExecuteTimeSlotDownloads();

                foreach (User user in randomizedUsers)
                {
                    _usersUploadTotal += user.ConsumedUploadBandwidth;
                    _usersDownloadTotal += user.ConsumedDownloadBandwidth;
                }
                _serverUploadTotal += _server.ConsumedUploadBandwidth;

                foreach (User user in randomizedUsers)
                    user.UserGroup.DataUploadedThisTimeSlot += user.ConsumedUploadBandwidth;
                foreach (User user in randomizedUsers)
                    user.UpdateAtEndOfTimeSlot();
                Server.UpdateAtEndOfTimeSlot();
                foreach (UserGroup userGroup in UserGroups)
                    ServerConsumedBandwidth[userGroup] = 0;
                foreach (User user in randomizedUsers)
                    user.PlayTimeSlot();

                _timeSlot++;

                foreach (UserGroup userGroup in UserGroups)
                    userGroup.DataUploadedThisTimeSlot = 0;
                foreach (User user in randomizedUsers)
                    AdjustUser(user);
                GroupHasBeenLowered = false;
                GroupHasBeenRaised = false;

                if (_timeSlot % (10 / Functions.TimePerSlot) == 0 && _timeSlot > 0)
                    RandomizeContactLists();
            }
        }

        private void AdjustUser(User user)
        {
            if(_noChange)
                return;

            //if (user.ConsecutiveNegativeBalance >= 20)
            if (user.Balance < -5)
            {
                LowerGroup(user);
                //Functions.LowerUser(user);
                PrintUserGroupTotalsToFile();
            }
            else if (user.Balance >= 10)
            {
                RaiseGroup(user);
                //Functions.RaiseUser(user);
                PrintUserGroupTotalsToFile();
            }
        }

        private void PrintUserGroupTotalsToFile()
        {
            return;
            if (File == null)
                File = new StreamWriter("Output.txt");

            foreach (UserGroup userGroup in UserGroups)
            {
                File.WriteLine($"User Group {userGroup.VideoRate} Count: {userGroup.Users.Count}");
            }
            File.WriteLine();
        }

        private bool Finished()
        {
            foreach (User user in Users)
                if (!user.PlayBackFinished)
                    //if (!user.File.Completed)
                    return false;

            return true;
        }

        private double CalculateDelay()
        {
            double delay = 0;
            foreach (User user in Users)
                delay += user.TimeDelayed;

            return delay;
        }

        private double CalculateBalance()
        {
            double balance = 0;
            foreach (User user in Users)
                balance += user.Balance;

            return balance;
        }

        private double CalculateTimeBuffering()
        {
            double timeBuffering = 0;
            foreach (User user in Users)
                timeBuffering += user.TimeBuffering;

            return timeBuffering;
        }



        public bool GroupHasBeenLowered { get; set; }
        public void LowerGroup(User user)
        {
            if (true) //!GroupHasBeenLowered)
            {
                user.UserGroup.Users.Sort((user1, user2) =>
                {
                    return user1.Balance.CompareTo(user2.Balance);
                });

                UserGroup userGroup = user.UserGroup;

                int amountToLower = userGroup.Size > GroupSizeToDrop ? GroupSizeToDrop : userGroup.Size;
                for (int i = 0; i < amountToLower; i++)
                    LowerUser(userGroup.Users[0]);
            }
            GroupHasBeenLowered = true;
        }
        public bool GroupHasBeenRaised { get; set; }
        public void RaiseGroup(User user)
        {
            if (true) //!GroupHasBeenRaised)
            {
                user.UserGroup.Users.Sort((user1, user2) =>
                {
                    return user2.Balance.CompareTo(user1.Balance);
                });

                UserGroup userGroup = user.UserGroup;

                int amountToRaise = userGroup.Size > GroupSizeToDrop ? GroupSizeToDrop : userGroup.Size;
                for (int i = 0; i < amountToRaise; i++)
                    RaiseUser(userGroup.Users[0]);
            }
            GroupHasBeenRaised = true;
        }

        public void LowerUser(User user)
        {
            //return; 
            if (user.UserGroup == UserGroups.Last())
                return;

            for (int i = 0; i < UserGroups.Count - 1; i++)
                if (user.UserGroup == UserGroups[i])
                {
                    UserGroup userGroup = user.UserGroup;
                    userGroup.RemoveUser(user);

                    UserGroups[i + 1].AddUser(user);
                    CreateNewContactList(user);

                    user.FreshBalance();
                    return;
                }

            if (user.UserGroup.Size % 20 == 0 && user.UserGroup.Size > 0)
                RandomizeContactLists();
        }

        public void RaiseUser(User user)
        {
            //return; 
            if (user.UserGroup == UserGroups.First())
                return;

            for (int i = 1; i < UserGroups.Count; i++)
                if (user.UserGroup == UserGroups[i])
                {
                    UserGroup userGroup = user.UserGroup;
                    userGroup.RemoveUser(user);

                    UserGroups[i - 1].AddUser(user);
                    CreateNewContactList(user);

                    user.FreshBalance();
                    return;
                }

            if (user.UserGroup.Size % 20 == 0 && user.UserGroup.Size > 0)
                RandomizeContactLists();
        }

        public void RandomizeContactLists()
        {
            foreach (User user in Users)
                CreateNewContactList(user);
        }

        public void CreateNewContactList(User user)
        {
            user.ContactList = new List<User>();
            if (user.UserGroup.Size <= ContactListSize)
            {
                foreach (User userInGroup in user.UserGroup.Users)
                    if (userInGroup == user)
                    {
                        foreach (User otherUsersInGroup in user.UserGroup.Users)
                            if (otherUsersInGroup != user)
                                otherUsersInGroup.ContactList.Add(user);
                    }
                    else
                        user.ContactList.Add(userInGroup);
            }
            else
            {
                for (int i = 0; i < ContactListSize; i++)
                {
                    AddRandomContact(user);
                }
            }
        }

        public void AddRandomContact(User user)
        {
            if (user.UserGroup.Size <= ContactListSize)
                return;

            bool contactAdded = false;
            while (!contactAdded)
            {
                User contact = user.UserGroup.Users[Functions.Random.Next(user.UserGroup.Size)];
                bool contactAlreadyAdded = false;
                if (contact != user)
                {
                    foreach (User addedContact in user.ContactList)
                        if (addedContact == contact)
                            contactAlreadyAdded = true;

                    if (!contactAlreadyAdded)
                    {
                        contactAdded = true;
                        user.ContactList.Add(contact);
                    }
                }
            }
        }




        private int NumberOfUsers = 500;
        private const double _videoLength = 1200;
        private int _numberOfSections;
        private User _server;
        private bool _finished;
        private int _timeSlot;
        private double _serverUploadTotal;
        private double _usersUploadTotal;
        private double _usersDownloadTotal;
        private StreamWriter File;

        public User Server { get; set; }
        public Dictionary<UserGroup, int[]> TimesFileServed { get; set; }
        public List<int>[] TimesFileServedList { get; set; }
        public const int MaxServerDistributionPerSection = 18;// Math.Ceiling(Math.Log(NumberOfUsers) / Math.Log(2));//10;
        public int ContactListSize { get; set; }
        public List<UserGroup> UserGroups;
        public double[] VideoRates = new double[]
        {
            4.5, //TODO
            3.5,
            2.5,
            1.5,
            0.5
        };
        public Dictionary<UserGroup, double> ServerConsumedBandwidth { get; set; }
        public User[] Users;
        public const double SamplesToAverage = 10;
        public int GroupSizeToDrop { get; set; }
    }
}
