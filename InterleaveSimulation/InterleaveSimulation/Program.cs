using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterleaveSimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            Network network = new Network();

            network.MakeUsers();
            network.CreateContactLists();
            network.CreateServer();

            network.DistributeFile();

            Console.Out.WriteLine("Finished");
            Console.Out.WriteLine($"Final Time Slot: {network.TimeSlot}.");

            Console.Read();
        }
    }
}
