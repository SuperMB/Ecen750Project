using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class Program
    {
        private static int _numberOfUsers = 50;
        private static double _videoLength = 10;
        private static int _contactListSize = 5;
        private static int _numberOfSections = (int)(_videoLength / Functions.TimePerSlot);
        private static User[] _users;
        private static User _server;
        private static bool _finished;
        private static int _timeSlot;

        static void Main(string[] args)
        {
            _finished = false;
            _timeSlot = 0;
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
                _users[i] = new User(
                    i,
                    3,
                    15,
                    4.5
                    )
                {
                    File = new File(_numberOfSections)
                };
            }
        }

        private static void CreateContactLists()
        {
            foreach (User user in _users)
            {
                user.ContactList = new User[_contactListSize].ToList();
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
                                for (int j = 0; j < _contactListSize; j++)
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
        
        private static void CreateServer()
        {
            _server = new User(
                -1,
                50,
                5000,
                4.5
                )
            {
                File = new File(_numberOfSections)
            };
            for (int i = 0; i < _numberOfSections; i++)
                _server.File.InsertSection(new FileSection(i));
            Functions.Server = _server;
        }

        private static void DistributeFile()
        {

        }

    }
}
