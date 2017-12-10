using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class Program
    {
        private static int _numberOfUsers = Functions.NumberOfUsers;
        private static double _videoLength = 120;
        private static int _numberOfSections = (int)(_videoLength / Functions.TimePerSlot);
        private static User _server;
        private static bool _finished;
        private static int _timeSlot;

        

        private static double _serverUploadTotal;
        private static double _usersUploadTotal;
        private static double _usersDownloadTotal;
        
        static void Main(string[] args)
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
            Console.Out.WriteLine($"Average Delay: {delay / _numberOfUsers}.");
            double timeBuffering = CalculateTimeBuffering();
            Console.Out.WriteLine($"Total Buffer Time: {timeBuffering}.");
            Console.Out.WriteLine($"Average Buffer Time: {timeBuffering / _numberOfUsers}.");
            double balance = CalculateBalance();
            Console.Out.WriteLine($"Total Balance: {balance}.");
            Console.Out.WriteLine($"Average Balance: {balance / _numberOfUsers}.");

            PrintUserGroupTotals();

            //foreach (User user in Functions.Users)
                //Console.Out.WriteLine($"User {user.UserNumber} - {user.AmountDownloaded}");

            string a = Console.ReadLine();
            while(a != "y")
                a = Console.ReadLine();
        }

        private static void PrintUserGroupTotals()
        {
            foreach(UserGroup userGroup in Functions.UserGroups)
            {
                Console.Out.WriteLine($"User Group {userGroup.VideoRate} Count: {userGroup.Users.Count}");
            }
        }

        private static void MakeUsers()
        {
            Functions.Users = new User[_numberOfUsers];
            int sections = 4;
            for(int i = 0; i < sections; i++)
                for (int j = 0; j < Functions.Users.Length / sections; j++)
                {
                    int position = i * Functions.Users.Length / sections + j;
                    Functions.Users[position] = new User(
                        position,
                        3 * Functions.TimePerSlot,
                        15 * Functions.TimePerSlot,
                        -1// Gets reset later
                        )
                    {
                        File = new File(_numberOfSections)
                    };
                }
        }

        private static void CreateContactLists()
        {
            foreach (User user in Functions.Users)
                user.ContactList = new List<User>();

            foreach (User user in Functions.Users)
                Functions.CreateNewContactList(user);
        }
        
        private static void CreateServer()
        {
            _server = new User(
                -1,
                1000 * Functions.TimePerSlot,
                -1, //doesn't matter
                1
                )
            {
                File = new File(_numberOfSections)
            };
            for (int i = 0; i < _numberOfSections; i++)
                _server.File.InsertSection(new FileSection(i));
            Functions.Server = _server;
            Functions.TimesFileServedList = new List<int>[_numberOfSections];
            for (int i = 0; i < _numberOfSections; i++)
                Functions.TimesFileServedList[i] = new List<int>();
        }

        private static void CreateUserGroups()
        {
            Functions.UserGroups = new List<UserGroup>();

            foreach (double videoRate in Functions.VideoRates)
                Functions.UserGroups.Add(new UserGroup(videoRate));

            foreach (User user in Functions.Users)
                Functions.UserGroups[0].AddUser(user);

            foreach (UserGroup userGroup in Functions.UserGroups)
                Functions.ServerConsumedBandwidth[userGroup] = 0;

            Functions.TimesFileServed = new Dictionary<UserGroup, int[]>();
            foreach (UserGroup userGroup in Functions.UserGroups)
                Functions.TimesFileServed[userGroup] = new int[_numberOfSections];
        }

        private static void DistributeFile()
        {
            while (!Finished())
            {
                User[] randomizedUsers = Functions.Users.OrderBy(user => Functions.Random.Next()).ToArray();
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
                Functions.Server.UpdateAtEndOfTimeSlot();
                foreach (UserGroup userGroup in Functions.UserGroups)
                    Functions.ServerConsumedBandwidth[userGroup] = 0;
                foreach (User user in randomizedUsers)
                    user.PlayTimeSlot();

                _timeSlot++;

                foreach (UserGroup userGroup in Functions.UserGroups)
                    userGroup.DataUploadedThisTimeSlot = 0;
                foreach (User user in randomizedUsers)
                    AdjustUser(user);

                if (_timeSlot % (10 / Functions.TimePerSlot) == 0 && _timeSlot > 0)
                    Functions.RandomizeContactLists();
            }
        }

        private static void AdjustUser(User user)
        {
            if (user.ConsecutiveNegativeBalance >= 20)
                Functions.LowerUser(user);
            else if (user.ConsecutivePositiveBalance >= 20)
                Functions.RaiseUser(user);
        }

        private static bool Finished()
        {
            foreach (User user in Functions.Users)
                if (!user.PlayBackFinished)
                //if (!user.File.Completed)
                    return false;

            return true;
        }

        private static double CalculateDelay()
        {
            double delay = 0;
            foreach (User user in Functions.Users)
                delay += user.TimeDelayed;

            return delay;
        }

        private static double CalculateBalance()
        {
            double balance = 0;
            foreach (User user in Functions.Users)
                balance += user.Balance;

            return balance;
        }

        private static double CalculateTimeBuffering()
        {
            double timeBuffering = 0;
            foreach (User user in Functions.Users)
                timeBuffering += user.TimeBuffering;

            return timeBuffering;
        }

    }
}
