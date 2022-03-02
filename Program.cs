using PSC.Stage;
using PSC.Stage.Thorlabs;
using System;
using System.Threading;
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
                Console.WriteLine("PSC test of Thorlabs BBD302 position triggers");

                // define a stage axis
                var axisConfig = new StageAxisThorlabsBBD30x.Configuration()
                {
                    Index = 2,
                    Direction = 1,
                    SpeedMin = 1,
                    SpeedMax = 100000,
                    RangeMin = -37500,
                    RangeMax = 37500,
                    RangeOffset = 0,
                    HomeDirection = 1,
                    EncoderCountsPerUm = 20,
                    VelocityScaleEncPerUm = 134.21773,
                    AccelScaleEncPerUm = 0.013744,
                };

                // Define the trigger pulse parameters:
                // 100 usec pulse width
                // falling pulse (low)
                // BNC1 output port
                var syncPulse = new PulseParam() { PulseWidth = 100, PulseEdge = Pulse.Edge.Low, PulsePort = BBD30x.IOPort.BNC1 };

                // create the controller communication object
                var controller = new StageComThorlabs() { Description = "Brushless Motor Controller" };

                // create the axis object
                var axis = new StageAxisThorlabsBBD30x(controller);

                // establish connection to the controller
                Console.WriteLine("Connecting ---------------------");
                controller.Connect();
                Console.WriteLine("Done ---------------------------");

                // configure the axis (this will enable the axis)
                Console.WriteLine("Configuring --------------------");
                axis.Configure(axisConfig);
                Console.WriteLine("Done ---------------------------");

                Console.WriteLine("Checking for errors ------------");
                axis.CheckForErrorCondition();
                Console.WriteLine("Done ---------------------------");

                // home the stage if required
                if (!axis.IsHomed())
                {
                    Console.WriteLine("Homing -------------------------");

                    axis.Home();

                    while (axis.IsMoving()) { Thread.Sleep(1000); }

                    Console.WriteLine("Done ---------------------------");
                }

                Console.WriteLine("Setting velocity ---------------");
                axis.SetVelocity(10000);
                Console.WriteLine("Done ---------------------------");

                // Move to start of scan line
                Console.WriteLine("Moving to start of scan line ---");
                axis.MoveAbsolute(axis.Config.RangeMin);
                while (axis.IsMoving()) { Thread.Sleep(1000); }
                Console.WriteLine("Done ---------------------------");

                // program the position triggers
                Console.WriteLine("Programming position triggers --");
                axis.SetupPositionTriggers(
                    startPos_um: (axisConfig.RangeMax + axisConfig.RangeMin / 2),   // start triggers at midpoint
                    increment_um: 100,                                              // pulse every 100 um
                    count: 100,                                                     // generate 100 pulses
                    syncPulse: syncPulse,
                    biDirectional: false,
                    repeatCount: 1);
                Console.WriteLine("Done ---------------------------");

                // move to end of scan line.  
                Console.WriteLine("Scanning -----------------------");
                axis.MoveAbsolute(axis.Config.RangeMax);
                while (axis.IsMoving()) { Thread.Sleep(1000); }
                Console.WriteLine("Done ---------------------------");

                // move back to start of scan line
                Console.WriteLine("Moving to start of scan line ---");
                axis.MoveAbsolute(axis.Config.RangeMin);
                while (axis.IsMoving()) { Thread.Sleep(1000); }
                Console.WriteLine("Done ---------------------------");

                // disarm the position triggers
                Console.WriteLine("Disarming position triggers ----");
                axis.ClearPositionTriggers();
                Console.WriteLine("Done ---------------------------");


                // disconnect the controller
                Console.WriteLine("Disconnecting ------------------");
                axis.Dispose();
                controller.Dispose();
                Console.WriteLine("Done ---------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }
    }
}
