using FTD2XX_NET;
using PSC.Stage.Thorlabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace PSC.Stage
{
    namespace Thorlabs
    {
        #region Module IDs

        public enum Target
        {
            HostController = 0x01,
            RackController = 0x11,
            Bay0 = 0x21,
            Bay1 = 0x22,
            Bay2 = 0x23,
            Bay3 = 0x24,
            Bay4 = 0x25,
            Bay5 = 0x26,
            Bay6 = 0x27,
            Bay7 = 0x28,
            Bay8 = 0x29,
            Bay9 = 0x2A,
            GenericUSB = 0x50
        }

        #endregion

        #region Command IDs

        // Messages Applicable to BBD10x, BBD20x,TBD001 and KBD101

        public enum MGMSG
        {
            MGMSG_MOD_IDENTIFY = 0x0223,
            MGMSG_MOD_SET_CHANENABLESTATE = 0x0210,
            MGMSG_MOD_REQ_CHANENABLESTATE = 0x0211,
            MGMSG_MOD_GET_CHANENABLESTATE = 0x0212,
            MGMSG_HW_DISCONNECT = 0x0002,
            MGMSG_HW_RESPONSE = 0x0080,
            MGMSG_HW_RICHRESPONSE = 0x0081,
            MGMSG_HW_START_UPDATEMSGS = 0x0011,
            MGMSG_HW_STOP_UPDATEMSGS = 0x0012,
            MGMSG_HW_REQ_INFO = 0x0005,
            MGMSG_HW_GET_INFO = 0x0006,
            MGMSG_RACK_REQ_BAYUSED = 0x0060,
            MGMSG_RACK_GET_BAYUSED = 0x0061,
            MGMSG_MOD_SET_DIGOUTPUTS = 0x0213,
            MGMSG_MOD_REQ_DIGOUTPUTS = 0x0214,
            MGMSG_MOD_GET_DIGOUTPUTS = 0x0215,
            MGMSG_MOT_SET_MOTTRIGIOCONFIG = 0x0260,
            MGMSG_MOT_REQ_MOTTRIGIOCONFIG = 0x0261,
            MGMSG_MOT_GET_MOTTRIGIOCONFIG = 0x0262,
            MGMSG_MOT_SET_IOCONFIG = 0x0263,
            MGMSG_MOT_REQ_IOCONFIG = 0x0264,
            MGMSG_MOT_GET_IOCONFIG = 0x0265,
            MGMSG_MOT_SET_POSTRIGENSTATE = 0x0272,
            MGMSG_MOT_REQ_POSTRIGENSTATE = 0x0273,
            MGMSG_MOT_GET_POSTRIGENSTATE = 0x274,
            MGMSG_MOT_SET_POSCOUNTER = 0x0410,
            MGMSG_MOT_REQ_POSCOUNTER = 0x0411,
            MGMSG_MOT_GET_POSCOUNTER = 0x0412,
            MGMSG_MOT_SET_ENCCOUNTER = 0x0409,
            MGMSG_MOT_REQ_ENCCOUNTER = 0x040A,
            MGMSG_MOT_GET_ENCCOUNTER = 0x040B,
            MGMSG_MOT_SET_VELPARAMS = 0x0413,
            MGMSG_MOT_REQ_VELPARAMS = 0x0414,
            MGMSG_MOT_GET_VELPARAMS = 0x0415,
            MGMSG_MOT_SET_JOGPARAMS = 0x0416,
            MGMSG_MOT_REQ_JOGPARAMS = 0x0417,
            MGMSG_MOT_GET_JOGPARAMS = 0x0418,
            MGMSG_MOT_SET_GENMOVEPARAMS = 0x043A,
            MGMSG_MOT_REQ_GENMOVEPARAMS = 0x043B,
            MGMSG_MOT_GET_GENMOVEPARAMS = 0x043C,
            MGMSG_MOT_SET_MOVERELPARAMS = 0x0445,
            MGMSG_MOT_REQ_MOVERELPARAMS = 0x0446,
            MGMSG_MOT_GET_MOVERELPARAMS = 0x0447,
            MGMSG_MOT_SET_MOVEABSPARAMS = 0x0450,
            MGMSG_MOT_REQ_MOVEABSPARAMS = 0x0451,
            MGMSG_MOT_GET_MOVEABSPARAMS = 0x0452,
            MGMSG_MOT_SET_HOMEPARAMS = 0x0440,
            MGMSG_MOT_REQ_HOMEPARAMS = 0x0441,
            MGMSG_MOT_GET_HOMEPARAMS = 0x0442,
            MGMSG_MOT_SET_LIMSWITCHPARAMS = 0x0423,
            MGMSG_MOT_REQ_LIMSWITCHPARAMS = 0x0424,
            MGMSG_MOT_GET_LIMSWITCHPARAMS = 0x0425,
            MGMSG_MOT_MOVE_HOME = 0x0443,
            MGMSG_MOT_MOVE_HOMED = 0x0444,
            MGMSG_MOT_MOVE_RELATIVE = 0x0448,
            MGMSG_MOT_MOVE_COMPLETED = 0x0464,
            MGMSG_MOT_MOVE_ABSOLUTE = 0x0453,
            MGMSG_MOT_MOVE_JOG = 0x046A,
            MGMSG_MOT_MOVE_VELOCITY = 0x0457,
            MGMSG_MOT_MOVE_STOP = 0x0465,
            MGMSG_MOT_MOVE_STOPPED = 0x0466,
            MGMSG_MOT_SET_EEPROMPARAMS = 0x04B9,
            MGMSG_MOT_SET_PMDPOSITIONLOOPPARAMS = 0x04D7,
            MGMSG_MOT_REQ_PMDPOSITIONLOOPPARAMS = 0x04D8,
            MGMSG_MOT_GET_PMDPOSITIONLOOPPARAMS = 0x04D9,
            MGMSG_MOT_SET_PMDMOTOROUTPUTPARAMS = 0x04DA,
            MGMSG_MOT_REQ_PMDMOTOROUTPUTPARAMS = 0x04DB,
            MGMSG_MOT_GET_PMDMOTOROUTPUTPARAMS = 0x04DC,
            MGMSG_MOT_SET_PMDTRACKSETTLEPARAMS = 0x04E0,
            MGMSG_MOT_REQ_PMDTRACKSETTLEPARAMS = 0x04E1,
            MGMSG_MOT_GET_PMDTRACKSETTLEPARAMS = 0x04E2,
            MGMSG_MOT_SET_PMDPROFILEMODEPARAMS = 0x04E3,
            MGMSG_MOT_REQ_PMDPROFILEMODEPARAMS = 0x04E4,
            MGMSG_MOT_GET_PMDPROFILEMODEPARAMS = 0x04E5,
            MGMSG_MOT_SET_PMDJOYSTICKPPARAMS = 0x04E6,
            MGMSG_MOT_REQ_PMDJOYSTICKPPARAMS = 0x04E7,
            MGMSG_MOT_GET_PMDJOYSTICKPPARAMS = 0x04E8,
            MGMSG_MOT_SET_PMDCURRENTLOOPPARAMS = 0x04D4,
            MGMSG_MOT_REQ_PMDCURRENTLOOPPARAMS = 0x04D5,
            MGMSG_MOT_GET_PMDCURRENTLOOPPARAMS = 0x04D6,
            MGMSG_MOT_SET_PMDSETTLEDCURRENTLOOPPARAMS = 0x04E9,
            MGMSG_MOT_REQ_PMDSETTLEDCURRENTLOOPPARAMS = 0x04EA,
            MGMSG_MOT_GET_PMDSETTLEDCURRENTLOOPPARAMS = 0x04EB,
            MGMSG_MOT_SET_PMDSTAGEAXISPARAMS = 0x04F0,
            MGMSG_MOT_REQ_PMDSTAGEAXISPARAMS = 0x04F1,
            MGMSG_MOT_GET_PMDSTAGEAXISPARAMS = 0x04F2,
            MGMSG_MOT_GET_DCSTATUSUPDATE = 0x0491,
            MGMSG_MOT_REQ_DCSTATUSUPDATE = 0x0490,
            MGMSG_MOT_ACK_DCSTATUSUPDATE = 0x0492,
            MGMSG_MOT_REQ_STATUSBITS = 0x0429,
            MGMSG_MOT_GET_STATUSBITS = 0x042A,
            MGMSG_MOT_SUSPEND_ENDOFMOVEMSGS = 0x046B,
            MGMSG_MOT_RESUME_ENDOFMOVEMSGS = 0x046C,
            MGMSG_MOT_SET_TDIPARAMS = 0x04FB,
            MGMSG_MOT_REQ_TDIPARAMS = 0x04FC,
            MGMSG_MOT_GET_TDIPARAMS = 0x04FD,
            MGMSG_MOT_SET_TRIGGER = 0x0500,
            MGMSG_MOT_REQ_TRIGGER = 0x0501,
            MGMSG_MOT_GET_TRIGGER = 0x0502,
        };

        #endregion

        #region Pulse Parameters

        public static class Pulse
        {
            public enum Edge
            {
                High,
                Low
            }
        }

        #endregion

        #region Typed Constants

        public static class Status
        {
            public const UInt32 ForwardHardwareLimitSwitchIsActive = 0x00000001;
            public const UInt32 ReverseHardwareLimitSwitchIsActive = 0x00000002;
            public const UInt32 ForwardSoftwareLimitSwitchIsActive = 0x00000004;
            public const UInt32 ReverseSoftwareLimitSwitchIsActive = 0x00000008;
            public const UInt32 InMotionMovingForward = 0x00000010;
            public const UInt32 InMotionMovingReverse = 0x00000020;
            public const UInt32 InMotionJoggingForward = 0x00000040;
            public const UInt32 InMotionJoggingReverse = 0x00000080;
            public const UInt32 MotorConnected = 0x00000100;
            public const UInt32 InMotionHoming = 0x00000200;
            public const UInt32 Homed = 0x00000400;
            public const UInt32 Tracking = 0x00001000;
            public const UInt32 Settled = 0x00002000;
            public const UInt32 MotionError = 0x00004000;
            public const UInt32 MotorCurrentLimitReached = 0x01000000;
            public const UInt32 ChannelIsEnabled = 0x80000000;

            public const UInt32 IsMovingMask = 0 //Status.InMotionHoming // [Note: We have removed 'InMotionHoming' due to an occasional fault condition that would leave this bit stuck on]
                                                                    | Status.InMotionMovingForward
                                                                    | Status.InMotionMovingReverse
                                                                    | Status.InMotionJoggingForward
                                                                    | Status.InMotionJoggingReverse;
        }

        public static class StopMode
        {
            public const byte Immediate = 0x01;
            public const byte Controlled = 0x02;
        }

        public static class Direction
        {
            public const byte Forward = 0x01;
            public const byte Reverse = 0x02;
        }

        public static class EnableState
        {
            public const byte Enabled = 0x01;
            public const byte Disabled = 0x02;
        }

        public enum VelocityProfile : UInt16
        {
            Trapezoidal = 0x00,
            SCurve = 0x02,
        }

        #endregion

        #region BBD30x

        public static class BBD30x
        {
            #region Triggers

            public enum Channel : ushort
            {
                One = 0x01,
                Two = 0x02,
                Three = 0x03,
            }

            public enum IOPort : ushort
            {
                BNC1 = 0x01,
                BNC2 = 0x02,
                BNC3 = 0x03,
            }

            public enum IOMode : ushort
            {
                DigIn = 0x00,
                DigOut = 0x01,
                AnalogOut = 0x02,
            }

            public enum TrigPolarity : ushort
            {
                High = 0x01,
                Low = 0x02,
            }

            public enum TrigInMode : ushort
            {
                Disabled = 0x00,
                GPI = 0x01,
                RelativeMove = 0x02,
                AbsoluteMove = 0x03,
                HomeMove = 0x04,
                Stop = 0x05,
            }

            public enum TrigInSource : ushort
            {
                Software = 0x00,
                BNC1 = 0x01,
                BNC2 = 0x02,
                BNC3 = 0x03,
            }

            public enum TrigOutMode : ushort
            {
                GPO = 0x0A,
                InMotion = 0x0B,
                AtMaxVelocity = 0x0C,
                AtPositionStepsFwd = 0x0D,
                AtPositionStepsRev = 0x0E,
                AtPositionStepsBoth = 0x0F,
                AtForwardLimit = 0x10,
                AtReverseLimit = 0x11,
                AtEitherLimit = 0x12,
            }

            public enum TrigOutSource : ushort
            {
                Software = 0x00,
                Channel1 = 0x01,
                Channel2 = 0x02,
                Channel3 = 0x03,
            }

            public enum TrigOutState : byte
            {
                Arm = 0x01,
                Disarm = 0x02,
            }

            #endregion
        }

        #endregion
    }

    public class StageComThorlabs : StageComFTDI
    {
        #region Private fields

        private const int packetQueueMaxLength = 128;
        private const int rxBufferLength = 512;
        private byte[] rxBuffer = new byte[rxBufferLength];
        private List<byte> rxStorage = new List<byte>();
        private ManualResetEvent killRxThreadEvent;
        private AutoResetEvent rxDataEvent;
        private Thread rxDataThread;
        private Dictionary<MessageKey, List<Action<Packet>>> callbackDictionary = new Dictionary<MessageKey, List<Action<Packet>>>();

        #endregion

        #region Constructors

        public StageComThorlabs()
            : base()
        {
        }

        #endregion

        #region StageComFTDI Overrides

        public override void Connect()
        {
            base.Connect();

            this.ftdi.SetTimeouts(50, 1000).ThrowOnError();

            this.rxStorage.Clear();

            this.StartRxThread();
        }

        public override void Disconnect()
        {
            this.KillRxThread();

            base.Disconnect();
        }

        public override void Dispose()
        {
            base.Dispose();

            this.KillRxThread();    // just to be sure?
        }

        #endregion

        #region Events

        public event Action<Packet> PacketReceived;

        private void PacketReceivedNotify(Packet packet) => this.PacketReceived?.Invoke(packet);

        #endregion

        #region Private API

        private void StartRxThread()
        {
            try
            {
                this.killRxThreadEvent = new ManualResetEvent(false);
                this.rxDataEvent = new AutoResetEvent(false);

                this.rxDataThread = new Thread(this.RxThreadProc)
                {
                    Priority = ThreadPriority.Highest,
                    Name = "Thorlabs communication.",
                    IsBackground = true
                };

                this.rxDataThread.Start();
                while (!this.rxDataThread.IsAlive)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(50));
                }

                this.ftdi.SetEventNotification(FTDI.FT_EVENTS.FT_EVENT_RXCHAR, this.rxDataEvent).ThrowOnError();
            }
            catch (Exception ex)
            {
                try
                {
                    this.killRxThreadEvent = null;
                    this.rxDataEvent = null;
                    this.rxDataThread = null;
                }
                catch (Exception iex)
                {
                    throw new AggregateException(new Exception[] { ex, iex });
                }
                throw ex;
            }
        }
        private void KillRxThread()
        {
            if (null == this.rxDataThread)
            {
                return;
            }

            try
            {
                this.killRxThreadEvent.Set();

                if (this.rxDataThread.IsAlive)
                {
                    if (!this.rxDataThread.Join(TimeSpan.FromSeconds(1)))
                    {
                        this.rxDataThread.Abort();
                        Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    }
                }
            }
            finally
            {
                this.rxDataThread = null;
                this.rxDataEvent = null;
                this.killRxThreadEvent = null;
            }
        }
        private void RxThreadProc()
        {
            try
            {
                var events = new WaitHandle[] {
                    this.killRxThreadEvent,
                    this.rxDataEvent
                };

                while (true)
                {
                    switch (WaitHandle.WaitAny(events))
                    {
                        case 0: // kill event
                            return;

                        case 1: // data received event
                            this.ReadBytes();
                            break;

                        default:
                            throw new ArgumentOutOfRangeException("event");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    return;
                }

                // Note: We have no local error reporting capability in this test app
                Console.WriteLine(ex.Message);
                
            }
        }
        private void ReadBytes()
        {
            uint rxQueue = 0;

            do
            {
                uint numBytesRead = 0;

                lock (this.SyncObj)
                {
                    this.ftdi.GetRxBytesAvailable(ref rxQueue).ThrowOnError();

                    if (0 < rxQueue)
                    {
                        this.ftdi.Read(this.rxBuffer, rxBufferLength, ref numBytesRead).ThrowOnError();
                    }
                }

                if (0 < numBytesRead)
                {
                    this.rxStorage.AddRange(this.rxBuffer.Take(Convert.ToInt32(numBytesRead)).ToArray());

                    // look for fully formed packets:

                    while (this.rxStorage.Count >= 6)
                    {
                        var packet = new Packet(this.rxStorage.Take(6).ToArray());
                        var packetLength = 6 + packet.DataLength;

                        if (6 == packetLength)
                        {
                            this.rxStorage.RemoveRange(0, packetLength);
                            this.DispatchPacket(packet);
                        }
                        else if (rxStorage.Count >= packetLength)
                        {
                            packet = new Packet(this.rxStorage.Take(packetLength).ToArray());
                            this.rxStorage.RemoveRange(0, packetLength);
                            this.DispatchPacket(packet);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            while (0 < rxQueue);
        }

        private void DispatchPacket(Packet packet)
        {
            this.LogPacket(packet);

            var msg = packet.MessageID;
            var source = packet.Source;

            IEnumerable<Action<Packet>> callbacks;
            lock (this.callbackDictionary)
            {
                callbacks = this.callbackDictionary.Where(
                    kvp => (null == kvp.Key.msg || msg == kvp.Key.msg)
                        && (null == kvp.Key.node || source == kvp.Key.node))
                    .SelectMany(kvp => kvp.Value)
                    .Distinct()
                    .ToArray();
            }

            foreach (var cb in callbacks)
            {
                cb(packet);
            }

            this.PacketReceivedNotify(packet);
        }

        private void LogPacket(Packet packet)
        {
            Console.WriteLine(string.Format("{0}: {1}, Raw bytes: {2}",
                Target.HostController == packet.Source ? "Tx" : "Rx",
                packet?.ToString() ?? "<null>",
                packet?.AsString ?? "<null>"));
        }

        #endregion

        #region Public API

        public void SendCommand(MGMSG msg, Target dest, byte p1 = 0x0, byte p2 = 0x0)
        {
            this.SendPacket(new Packet(msg, p1, p2, dest, Target.HostController));
        }
        public void SendCommand(MGMSG msg, Target dest, params object[] args)
        {
            this.SendPacket(new Packet(msg, dest, args));
        }
        public void SendPacket(Packet packet)
        {
            this.LogPacket(packet);

            uint bytesWritten = 0;

            this.ftdi.Write(packet.RawBytes, packet.RawBytes.Length, ref bytesWritten).ThrowOnError();

            if (bytesWritten != packet.RawBytes.Length)
            {
                throw new Exception("Error writing packet data to USB.");
            }
        }

        public Packet SendAndReceive(MGMSG msg, MGMSG reply, Target dest, TimeSpan timeout, byte p1 = 0x0, byte p2 = 0x0)
        {
            Packet packet = null;
            var evt = new ManualResetEvent(false);
            var cb = new Action<Packet>(p => { packet = p; evt.Set(); });

            this.Subscribe(reply, dest, cb);

            try
            {
                this.SendCommand(msg, dest, p1, p2);

                if (evt.WaitOne(timeout))
                {
                    return packet;
                }
                throw new TimeoutException();
            }
            finally
            {
                this.Unsubscribe(reply, dest, cb);
            }
        }
        public Packet SendAndReceive(MGMSG msg, MGMSG reply, Target dest, TimeSpan timeout, params object[] args)
        {
            Packet packet = null;
            var evt = new ManualResetEvent(false);
            var cb = new Action<Packet>(p => { packet = p; evt.Set(); });

            this.Subscribe(reply, dest, cb);

            try
            {
                this.SendCommand(msg, dest, args);

                if (evt.WaitOne(timeout))
                {
                    return packet;
                }
                throw new TimeoutException();
            }
            finally
            {
                this.Unsubscribe(reply, dest, cb);
            }
        }
        public async Task<Packet> SendAndReceiveAsync(MGMSG msg, MGMSG reply, Target dest, TimeSpan timeout, byte p1 = 0x0, byte p2 = 0x0)
        {
            var tcs = new TaskCompletionSource<Packet>();
            var cb = new Action<Packet>(packet => tcs.SetResult(packet));

            this.Subscribe(reply, dest, cb);

            try
            {
                this.SendCommand(msg, dest, p1, p2);

                if (await Task.WhenAny(tcs.Task, Task.Delay(timeout)).ConfigureAwait(false) == tcs.Task)
                {
                    return tcs.Task.Result;
                }

                throw new TimeoutException();
            }
            finally
            {
                this.Unsubscribe(reply, dest, cb);
            }
        }
        public async Task<Packet> SendAndReceiveAsync(MGMSG msg, MGMSG reply, Target dest, TimeSpan timeout, params object[] args)
        {
            var tcs = new TaskCompletionSource<Packet>();
            var cb = new Action<Packet>(packet => tcs.SetResult(packet));

            this.Subscribe(reply, dest, cb);

            try
            {
                this.SendCommand(msg, dest, args);

                if (await Task.WhenAny(tcs.Task, Task.Delay(timeout)).ConfigureAwait(false) == tcs.Task)
                {
                    return tcs.Task.Result;
                }

                throw new TimeoutException();
            }
            finally
            {
                this.Unsubscribe(reply, dest, cb);
            }
        }

        public void Subscribe(MGMSG? msg, Target? source, Action<Packet> callback)
        {
            this.Subscribe(new MessageKey(msg, source), callback);
        }
        public void Subscribe(MessageKey key, Action<Packet> callback)
        {
            lock (this.callbackDictionary)
            {
                if (this.callbackDictionary.ContainsKey(key))
                {
                    this.callbackDictionary[key].Add(callback);
                }
                else
                {
                    this.callbackDictionary[key] = new List<Action<Packet>>(new Action<Packet>[] { callback });
                }
            }
        }
        public void Unsubscribe(MGMSG? msg, Target? source, Action<Packet> callback)
        {
            this.Unsubscribe(new MessageKey(msg, source), callback);
        }
        public void Unsubscribe(MessageKey key, Action<Packet> callback)
        {
            lock (this.callbackDictionary)
            {
                if (this.callbackDictionary.ContainsKey(key))
                {
                    var item = this.callbackDictionary[key];

                    if (item.Contains(callback))
                    {
                        item.Remove(callback);
                    }

                    if (!item.Any())
                    {
                        this.callbackDictionary.Remove(key);
                    }
                }
            }
        }

        #region Local classes

        public class Packet
        {
            #region Constructors

            public Packet()
            {
                this.RawBytes = new byte[6];
            }
            public Packet(MGMSG id, byte param1 = 0x0, byte param2 = 0x0, Target dest = Target.Bay0, Target source = Target.HostController)
                : this()
            {
                this.MessageID = id;
                this.Param1 = param1;
                this.Param2 = param2;
                this.Dest = dest;
                this.Source = source;
            }
            public Packet(MGMSG id, Target dest, params object[] args)
                : this(id, 0x0, 0x0, dest, Target.HostController)
            {
                // args may only be one of the following types:
                //  word   (unsigned 16 bit integer [2 bytes])
                //  short  (signed 16 bit integer [2 bytes])
                //  dword  (unsigned 32 bit integer [4 bytes])
                //  long   (signed 32 bit integer [4 bytes])
                //  char   (1 byte)
                // char[N] (string of N bytes)

                var list = new List<byte>();

                foreach (object arg in args)
                {
                    IEnumerable<byte> bytes;

                    if (arg is Int16)
                    {
                        bytes = BitConverter.GetBytes((Int16)arg);
                    }
                    else if (arg is UInt16)
                    {
                        bytes = BitConverter.GetBytes((UInt16)arg);
                    }
                    else if (arg is Int32)
                    {
                        bytes = BitConverter.GetBytes((Int32)arg);
                    }
                    else if (arg is UInt32)
                    {
                        bytes = BitConverter.GetBytes((UInt32)arg);
                    }
                    else if (arg is byte)
                    {
                        bytes = new byte[] { (byte)arg };
                    }
                    else if (arg is byte[])
                    {
                        bytes = arg as byte[];
                    }
                    else
                    {
                        throw new Exception("Unsupported data type encountered.");
                    }

                    list.AddRange(bytes);
                }

                var data = list.ToArray();

                if (data.Length > 0xFFFF)
                {
                    throw new Exception("Too much data provided for Thorlabs packet format.");
                }

                this.DataLength = Convert.ToUInt16(data.Length);

                this.RawBytes = this.RawBytes.Take(6).Concat(data).ToArray();
            }
            public Packet(byte[] srcData)
            {
                if (null == srcData || srcData.Length < 6)
                {
                    throw new Exception("Data provided does not describe a well formed packet.");
                }
                this.RawBytes = srcData;
            }

            #endregion

            #region Public Properties

            public MGMSG MessageID
            {
                get => (MGMSG)Convert.ToUInt16((Convert.ToUInt16(this.RawBytes[1]) << 8) | this.RawBytes[0]);
                set
                {
                    var u16 = Convert.ToUInt16(value);
                    this.RawBytes[0] = Convert.ToByte(0xFF & u16);
                    this.RawBytes[1] = Convert.ToByte(0xFF & (u16 >> 8));
                }
            }
            public byte Param1
            {
                get => this.RawBytes[2];
                set => this.RawBytes[2] = value;
            }
            public byte Param2
            {
                get => this.RawBytes[3];
                set => this.RawBytes[3] = value;
            }
            public UInt16 DataLength
            {
                get
                {
                    if (0 == (0x80 & this.RawBytes[4]))
                    {
                        return 0x00;
                    }

                    return Convert.ToUInt16((Convert.ToUInt16(this.RawBytes[3]) << 8) | this.RawBytes[2]);
                }
                set
                {
                    var newPacket = this.RawBytes.Take(6).ToArray();

                    if (0 == value)
                    {
                        newPacket[4] = Convert.ToByte(newPacket[4] & ~0x80);
                    }
                    else
                    {
                        newPacket[2] = Convert.ToByte(0xFF & value);
                        newPacket[3] = Convert.ToByte(0xFF & (value >> 8));
                        newPacket[4] |= 0x80;

                        newPacket = newPacket.Concat(new byte[value]).ToArray();
                    }

                    this.RawBytes = newPacket;
                }
            }
            public Target Dest
            {
                get => (Target)Convert.ToByte(this.RawBytes[4] & ~0x80);
                set => this.RawBytes[4] = Convert.ToByte(Convert.ToByte(value) | (this.RawBytes[4] & 0x80));
            }
            public Target Source
            {
                get => (Target)this.RawBytes[5];
                set => this.RawBytes[5] = Convert.ToByte(value);
            }

            public byte[] RawBytes { get; private set; } = new byte[6];

            public string AsString => string.Join(" ", this.RawBytes.Select(b => string.Format("{0:X2}", b)));

            #endregion

            #region Private API

            private void SetPacketBytes(byte[] srcPacket)
            {
                if (null == srcPacket || srcPacket.Length < 6)
                {
                    throw new Exception("USB packet header length must be at least 6-bytes.");
                }

                this.RawBytes = srcPacket.ToArray();
            }
            private void AppendPacketBytes(byte[] srcBytes) => this.RawBytes = this.RawBytes.Concat(srcBytes).ToArray();

            #endregion

            #region Overrides

            public override string ToString()
            {
                var msgId = (MGMSG)this.MessageID;

                var s = string.Format("[{0}", msgId.ToString());

                if (this.DataLength > 0)
                {
                    s += string.Format(", data = {0} bytes", this.DataLength);
                }
                else
                {
                    s += string.Format(", P1 = 0x{0:X2}, P2 = 0x{1:X2}", this.Param1, this.Param2);
                }

                s += string.Format(", DST = {0}, SRC = {1}]", this.Dest, this.Source);

                return s;
            }

            #endregion
        }

        public class MessageKey : IEquatable<MessageKey>, ICloneable
        {
            #region Public fields

            public MGMSG? msg;
            public Target? node;

            #endregion

            #region Constructors

            public MessageKey()
            {
            }
            public MessageKey(MGMSG? msg, Target? node)
            {
                this.msg = msg;
                this.node = node;
            }
            public MessageKey(MGMSG msg)
            {
                this.msg = msg;
            }
            public MessageKey(Target node)
            {
                this.node = node;
            }
            public MessageKey(MessageKey other)
            {
                this.msg = other.msg;
                this.node = other.node;
            }

            #endregion

            #region IEquatable

            public bool Equals(MessageKey obj)
            {
                if (null == obj)
                {
                    return false;
                }

                if (object.ReferenceEquals(this, obj))
                {
                    return true;
                }

                var msgEqual = int.Equals(this.msg ?? (MGMSG)(-1), obj.msg ?? (MGMSG)(-1));
                var nodeEqual = int.Equals(this.node ?? (Target)(-1), obj.node ?? (Target)(-1));

                return msgEqual && nodeEqual;
            }

            #endregion

            #region ICloneable

            public object Clone()
            {
                return new MessageKey(this);
            }

            #endregion

            #region Overrides

            public override bool Equals(object obj)
            {
                return this.Equals(obj as MessageKey);
            }
            public override int GetHashCode()
            {
                unchecked // overflow/wrap is fine
                {
                    int hash = (int)2166136261;

                    hash = hash * 16777619 ^ (this.msg ?? (MGMSG)(-1)).GetHashCode();
                    hash = hash * 16777619 ^ (this.node ?? (Target)(-1)).GetHashCode();

                    return hash;
                }
            }
            public override string ToString()
            {
                return string.Format("[{0}, {1}]",
                    null == this.msg ? "<none>" : this.msg.ToString(),
                    null == this.node ? "<none>" : this.node.ToString());
            }

            #endregion
        }

        #endregion

        #endregion
    }
}

