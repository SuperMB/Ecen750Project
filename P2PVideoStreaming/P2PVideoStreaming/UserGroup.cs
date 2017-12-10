using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class UserGroup
    {
        public UserGroup(double videoRate)
        {
            VideoRate = videoRate;
            Users = new List<User>();
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
                    Functions.AddRandomContact(userInGroup);
                }
        }

        public int Size
        {
            get
            {
                return Users.Count;
            }
        }

        public double VideoRate { get; set; }
        public List<User> Users { get; set; }
        public double DataUploadedThisTimeSlot { get; set; }
    }
}
