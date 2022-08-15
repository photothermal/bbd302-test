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
                Console.WriteLine("PSC test of Thorlabs BBD302 status messages");

                var ackTask = default(Task);
                var cancelSource = new CancellationTokenSource();
                var cancellationToken = cancelSource.Token;

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


                try
                {
                    // start loop to send acq messages
                    Console.WriteLine("Starting ACQ messages ----------");
                    Console.WriteLine("Done ---------------------------");
                    ackTask = Task.Run(async () =>
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            controller.SendPacket(new StageComThorlabs.Packet(MGMSG.MGMSG_MOT_ACK_DCSTATUSUPDATE, Target.Bay0));
                            controller.SendPacket(new StageComThorlabs.Packet(MGMSG.MGMSG_MOT_ACK_DCSTATUSUPDATE, Target.Bay1));

                            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                        }
                    });


                    Console.WriteLine("Press any key to stop messages...");
                    Console.ReadKey();
                }
                finally
                {
                    // halt ACK messages
                    Console.WriteLine("Halting ACQ messages -----------");
                    cancelSource?.Cancel();
                    ackTask.Wait(1000);
                    Console.WriteLine("Done ---------------------------");


                    // unsubscribe from update messages
                    Console.WriteLine("Halting Status Updates ---------");
                    controller.SendPacket(new StageComThorlabs.Packet(MGMSG.MGMSG_HW_STOP_UPDATEMSGS, Target.Bay0));
                    controller.SendPacket(new StageComThorlabs.Packet(MGMSG.MGMSG_HW_STOP_UPDATEMSGS, Target.Bay1));
                    Console.WriteLine("Done ---------------------------");


                    // disconnect the controller
                    Console.WriteLine("Disconnecting ------------------");
                    controller.Dispose();
                    Console.WriteLine("Done ---------------------------");

                }
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
