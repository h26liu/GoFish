/*!	\file		GoFishServiceHost/Program.cs
	\author		Haohan Liu, Dmytro Liaska
	\date		2019-04-08
*/

using GoFishLibrary;
using System;
using System.ServiceModel;

namespace GoFishServiceHost
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost servHost = null;

            try
            {
                // Register the service Address
                //servHost = new ServiceHost(typeof(Shoe), new Uri("net.tcp://localhost:13200/CardsLibrary/"));
                servHost = new ServiceHost(typeof(Shoe));

                // Register the service Contract and Binding
                // servHost.AddServiceEndpoint(typeof(IShoe), new NetTcpBinding(), "ShoeService");

                // Run the service
                servHost.Open();
                Console.WriteLine("Service started. Press any key to quit.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                // Wait for a keystroke
                Console.ReadKey();
                if (servHost != null)
                    servHost.Close();
            }
        }
    }
}
