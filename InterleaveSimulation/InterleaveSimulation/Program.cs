using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterleaveSimulation
{
    class Program
    {
        private static int _numberOfUsers = 500;
        private static int _numberOfSections = 1000;
        private static int _contactListSize = 3;
        private static User[] _users;
        private static User _server;
        private static int _timeSlot;
        private static bool _finished;
        private static int _nextServerSection;

        static void Main(string[] args)
        {
            _timeSlot = 0;
            _nextServerSection = 0;
            _finished = false;

            MakeUsers();
            CreateContactLists();
            CreateServer();

            DistributeFile();
            Console.Out.WriteLine("Finished");
            Console.Out.WriteLine($"Final Time Slot: {_timeSlot}.");

            Console.Read();
        }

        private static void MakeUsers()
        {
            _users = new User[_numberOfUsers];
            for (int i = 0; i < _users.Length; i++)
            {
                _users[i] = new User(i);
                User user = _users[i];
                user.File = new File(_numberOfSections);
            }
        }

        private static void CreateContactLists()
        {
            foreach (User user in _users)
            {
                user.ContactList = new User[_contactListSize];
                for (int i = 0; i < _contactListSize; i++)
                {
                    bool contactAdded = false;
                    while (!contactAdded)
                    {
                        User contact = _users[Functions.Random.Next(_users.Length)];
                        bool contactAlreadyAdded = false;
                        if (contact != user)
                        {
                            foreach (User addedContact in user.ContactList)
                                if (addedContact == contact)
                                    contactAlreadyAdded = true;

                            if (!contactAlreadyAdded)
                            {
                                contactAdded = true;
                                for(int j = 0; j < _contactListSize; j++)
                                    if(user.ContactList[j] == null)
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

        private static void CreateServer()
        {
            _server = new User(-1);
            _server.File = new File(_numberOfSections);
            for (int i = 0; i < _numberOfSections; i++)
                _server.File.InsertSection(new FileSection(i));
        }

        private static void DistributeFile()
        {
            while(!_finished)
            {
                OddSection();
                EvenSection();
            }
        }

        private static void OddSection()
        {
            if (CheckForCompletion())
                return;

            foreach (User user in _users)
                user.MoveToOddSection();

            _timeSlot++;

            //server pushing out the next section
            if (_nextServerSection < _numberOfSections)
            {
                _users[Functions.Random.Next(_users.Length)].ReceivePush(_server.File.FileSections[_nextServerSection]);
                _nextServerSection++;
            }

            foreach (User user in _users)
                user.Push();                
        }

        private static void EvenSection()
        {
            if (CheckForCompletion())
                return;

            foreach (User user in _users)
                user.MoveToEvenSection();

            _timeSlot++;

            foreach (User user in _users)
                user.Pull();
        }

        private static bool CheckForCompletion()
        {
            foreach (User user in _users)
                if (!user.File.Completed)
                    return false;

            _finished = true;
            return true;
        }
    }
}
