using PSC.Stage;
using PSC.Stage.Thorlabs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Pulse = PSC.Stage.Thorlabs.Pulse;
using PulseParam = PSC.Stage.StageAxisThorlabsBBD30x.Configuration.PulseParam;

namespace bbd302_test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("PSC utility to simulate deadlock, forcing Thorlabs BBD302 to halt status messages");

                // create the controller communication object
                var controller = new StageComThorlabs() { Description = "Brushless Motor Controller" };

                // establish connection to the controller
                Console.WriteLine("Connecting ---------------------");
                controller.Connect();
                Console.WriteLine("Done ---------------------------");

                // reqest update messages
                Console.WriteLine("Requesting Status Updates ------");
                controller.SendPacket(new StageComThorlabs.Packet(MGMSG.MGMSG_HW_START_UPDATEMSGS, Target.Bay0));
                controller.SendPacket(new StageComThorlabs.Packet(MGMSG.MGMSG_HW_START_UPDATEMSGS, Target.Bay1));
                Console.WriteLine("Done ---------------------------");


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine("Press any key to close...");
                Console.ReadKey();
            }
        }
    }
}
