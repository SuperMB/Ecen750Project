using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    static class Functions
    {
        static Functions()
        {
            Random = new Random(4);
            ServerConsumedBandwidth = new Dictionary<UserGroup, double>();
            GroupHasBeenLowered = false;
            GroupHasBeenRaised = false;
        }

        public static double BandwidthNeededPerSection(double videoRate)
        {
            return videoRate * TimePerSlot;
        }

        public static bool GroupHasBeenLowered { get; set; }
        public static void LowerGroup(User user)
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
        public static bool GroupHasBeenRaised { get; set; }
        public static void RaiseGroup(User user)
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

        public static void LowerUser(User user)
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

        public static void RaiseUser(User user)
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

        public static void RandomizeContactLists()
        {
            foreach (User user in Users)
                CreateNewContactList(user);
        }

        public static void CreateNewContactList(User user)
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

        public static void AddRandomContact(User user)
        {
            if (user.UserGroup.Size <= ContactListSize)
                return;

            bool contactAdded = false;
            while (!contactAdded)
            {
                User contact = user.UserGroup.Users[Random.Next(user.UserGroup.Size)];
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



        public static Random Random { get; set; }
        public const double TimePerSlot = .2;
        public static User Server { get; set; }
        public static Dictionary<UserGroup,int[]> TimesFileServed { get; set; }
        public static List<int>[] TimesFileServedList { get; set; }
        public const int MaxServerDistributionPerSection = 18;// Math.Ceiling(Math.Log(NumberOfUsers) / Math.Log(2));//10;
        public const int NumberOfUsers = 500;
        public const int ContactListSize = 40;
        public static List<UserGroup> UserGroups;
        public static double[] VideoRates = new double[]
        {
            4.5, //TODO
            3.5,
            2.5,
            1.5,
            0.5
        };
        public static Dictionary<UserGroup, double> ServerConsumedBandwidth { get; set; }
        public static User[] Users;
        public const double SamplesToAverage = 10;
        public const int GroupSizeToDrop = ContactListSize;
    }
}
