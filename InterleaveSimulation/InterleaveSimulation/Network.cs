using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterleaveSimulation
{
    class Network
    {
        public Network(int contactListSize)
        {
            TimeSlot = 0;
            _nextServerSection = 0;
            _finished = false;

            ContactListSize = contactListSize;
        }

        public void RunSimulation()
        {
            MakeUsers();
            CreateContactLists();
            CreateServer();

            DistributeFile();

            Console.Out.WriteLine("Finished");
            Console.Out.WriteLine($"Final Time Slot: {TimeSlot}.");
        }

        public void MakeUsers()
        {
            Users = new User[NumberOfUsers];
            for (int i = 0; i < Users.Length; i++)
            {
                Users[i] = new User(i);
                User user = Users[i];
                user.File = new File(NumberOfSections);
            }
        }

        public void CreateContactLists()
        {
            foreach (User user in Users)
            {
                user.ContactList = new User[ContactListSize];
                for (int i = 0; i < ContactListSize; i++)
                {
                    bool contactAdded = false;
                    while (!contactAdded)
                    {
                        User contact = Users[Functions.Random.Next(Users.Length)];
                        bool contactAlreadyAdded = false;
                        if (contact != user)
                        {
                            foreach (User addedContact in user.ContactList)
                                if (addedContact == contact)
                                    contactAlreadyAdded = true;

                            if (!contactAlreadyAdded)
                            {
                                contactAdded = true;
                                for (int j = 0; j < ContactListSize; j++)
                                    if (user.ContactList[j] == null)
                                    {
                                        user.ContactList[j] = contact;
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }

        public void CreateServer()
        {
            Server = new User(-1);
            Server.File = new File(NumberOfSections);
            for (int i = 0; i < NumberOfSections; i++)
                Server.File.InsertSection(new FileSection(i));
        }

        public void DistributeFile()
        {
            while (!_finished)
            {
                OddSection();
                EvenSection();
            }
        }

        public void OddSection()
        {
            if (CheckForCompletion())
                return;

            foreach (User user in Users)
                user.MoveToOddSection();

            TimeSlot++;

            //server pushing out the next section
            if (_nextServerSection < NumberOfSections)
            {
                Users[Functions.Random.Next(Users.Length)].ReceivePush(Server.File.FileSections[_nextServerSection]);
                _nextServerSection++;
            }

            foreach (User user in Users)
                user.Push();
        }

        public void EvenSection()
        {
            if (CheckForCompletion())
                return;

            foreach (User user in Users)
                user.MoveToEvenSection();

            TimeSlot++;

            foreach (User user in Users)
                user.Pull();
        }

        public bool CheckForCompletion()
        {
            foreach (User user in Users)
                if (!user.File.Completed)
                    return false;

            _finished = true;
            return true;
        }

        public const int NumberOfUsers = 500;
        public const int NumberOfSections = 1000;
        public int ContactListSize;
        public User[] Users { get; set; }
        public User Server { get; set; }
        public int TimeSlot { get; private set; }
        private bool _finished;
        private int _nextServerSection;
    }
}
