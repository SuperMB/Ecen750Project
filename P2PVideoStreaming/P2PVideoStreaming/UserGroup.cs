using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class UserGroup
    {
        public UserGroup(double videoRate, Network network)
        {
            VideoRate = videoRate;
            Users = new List<User>();
            Network = network;
        }

        public double AddUser(User user)
        {
            Users.Add(user);
            user.VideoRate = VideoRate;
            user.UserGroup = this;
            return VideoRate;
        }

        public void RemoveUser(User user)
        {
            Users.Remove(user);
            user.UserGroup = null;

            foreach (User userInGroup in Users)
                if (userInGroup.ContactList.Contains(user))
                {
                    userInGroup.ContactList.Remove(user);
                    Network.AddRandomContact(userInGroup);
                }
        }

        public int Size
        {
            get
            {
                return Users.Count;
            }
        }

        public double AverageBalance()
        {
            if (Size == 0)
                return 0;

            double balance = 0;
            foreach (User user in Users)
                balance += user.Balance;

            return balance / Users.Count;
        }

        public bool ValidateContactLists()
        {
            foreach (User user in Users)
                foreach (User contact in user.ContactList)
                    if (contact.UserGroup != this)
                        return false;

            return true;
        }

        public double AverageUploadBandwidth()
        {
            if (Size == 0)
                return 0;

            double uploadBandwidth = 0;
            foreach (User user in Users)
                uploadBandwidth += user.UploadBandwidth;

            return uploadBandwidth / Size / Functions.TimePerSlot;
        }

        public double VideoRate { get; set; }
        public List<User> Users { get; set; }
        public Network Network { get; private set; }
        public double DataUploadedThisTimeSlot { get; set; }
    }
}
