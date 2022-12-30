using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace ChatHost
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while(true)
            {
                using (var host = new ServiceHost(typeof(wcf_chat.ServiceChat)))
                {
                    host.Open();
                    Console.WriteLine("Хост запущен");
                    Console.ReadLine();
                }
            }
        }
    }
}
