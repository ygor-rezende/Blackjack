using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using BlackJackLibrary;

namespace BlackJackHost
{
    internal class Program
    {
        static void Main()
        {
            ServiceHost servHost = null;

            try
            {
                // Instantiate the SertviceHost (endpoint configuration is 
                // "looked-up" by the CLR in the App.config file)
                servHost = new ServiceHost(typeof(Shoe));

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
                // Keep the service running until a keystroke is entered by the system administrator
                Console.ReadKey();
                servHost?.Close();
            }
        }
    }
}
