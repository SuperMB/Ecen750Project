using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class Program
    {
        
        static void Main(string[] args)
        {
            for (int i = 4; i <= 64; i *= 2)
            //for (int i = 16; i <= 16; i *= 2)
            {
                Console.Out.WriteLine(i);
                Network network = new Network(i);
                network.RunSimulation();
                Console.Out.WriteLine();
            }


            string a = Console.ReadLine();
            while (a != "y")
                a = Console.ReadLine();
        }
    }
}
