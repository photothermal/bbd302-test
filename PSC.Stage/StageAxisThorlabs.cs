using PSC.Stage.Thorlabs;
using System;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PSC.Stage
{
    public class StageAxisThorlabsBBD30x : IDisposable
    {
        #region Private fields

        protected StageComThorlabs stageCom;
        protected Configuration config;

        protected Target bayID = Target.Bay0;
        protected byte chanId = 0x01;                       // does this ever change?

        const uint rxBufferLength = 512;
        private byte[] rxBuffer = new byte[rxBufferLength];

        protected Int32 encoderMin = 0;                     // Determine these at setup time
        protected Int32 encoderMax = 2200000;               // 

        protected int commandedPos;

        protected UInt32 cachedAcceleration;
        protected UInt32 cachedVelocity;

        #endregion

        #region Constructors

        public StageAxisThorlabsBBD30x(StageComThorlabs stageCom)
        {
            this.stageCom = stageCom;
        }

        #endregion

        #region Public properties

        public Configuration Config => this.config;

        #endregion

        #region StageAxis Overrides

        public virtual void Configure(Configuration configuration = default(Configuration))
        {
            if (null != configuration)
            {
                this.config = configuration;

                // We may add an explicit 'Target' item to the Thorlabs.Configuration later.  For now, just use 'Index'
                this.bayID = (Target)Convert.ToByte(Math.Max(0, this.config.Index - 1) + Target.Bay0);

                // now calculate encoder min/max
                var r1 = this.ConvertAbsoluteUmToEncoder(this.config.RangeMin);
                var r2 = this.ConvertAbsoluteUmToEncoder(this.config.RangeMax);
                this.encoderMin = Math.Min(r1, r2);
                this.encoderMax = Math.Max(r1, r2);
            }

            if (this.IsDisabled)
            {
                return;
            }

            // set velocity profile to trapezoidal
            this.stageCom.SendCommand(
                MGMSG.MGMSG_MOT_SET_PMDPROFILEMODEPARAMS,
                this.bayID,
                Convert.ToInt16(this.chanId),
                Convert.ToInt16(VelocityProfile.Trapezoidal),
                Convert.ToInt32(0),     // 'jerk' parameter (not used for 'Trapezoidal' profiles
                Convert.ToInt16(0),     // not used
                Convert.ToInt16(0)      // not used
                );

            // cache the current acceleration and velocity
            var p = this.stageCom.SendAndReceive(
                MGMSG.MGMSG_MOT_REQ_VELPARAMS,
                MGMSG.MGMSG_MOT_GET_VELPARAMS,
                this.bayID,
                TimeSpan.FromSeconds(1),
                this.chanId);
            this.cachedAcceleration = BitConverter.ToUInt32(p.RawBytes, 12);
            this.cachedVelocity = BitConverter.ToUInt32(p.RawBytes, 16);

            // enable
            this.stageCom.SendCommand(
                MGMSG.MGMSG_MOD_SET_CHANENABLESTATE,
                this.bayID,
                this.chanId,
                EnableState.Enabled);

            this.StartStatusUpdates();
        }

        public virtual void Dispose()
        {
            this.StopStatusUpdates();
        }

        public bool IsDisabled => false;

        public virtual bool Home()
        {
            try
            {
                if (this.IsDisabled)
                {
                    return true;
                }

                //UInt16 homeDirection = 2;       // 1 - forward/Positive or 2 - reverse/Negative
                //UInt16 limitSwitch = 4;         // 1 - hardware reverse or 4 - hardware forward
                //UInt32 homeVelocity = 0x0;      // unknown
                //UInt32 offsetDistance = 0x0;    // unknown


                var p = this.stageCom.SendAndReceive(
                    MGMSG.MGMSG_MOT_REQ_HOMEPARAMS,
                    MGMSG.MGMSG_MOT_GET_HOMEPARAMS,
                    this.bayID,
                    TimeSpan.FromSeconds(1),
                    this.chanId);

                var homeDirection = BitConverter.ToUInt16(p.RawBytes, 8);
                var limitSwitch = BitConverter.ToUInt16(p.RawBytes, 10);
                var homeVelocity = BitConverter.ToUInt32(p.RawBytes, 12);
                var offsetDistance = BitConverter.ToUInt32(p.RawBytes, 16);

                homeDirection = this.config.HomeDirection;

                this.stageCom.SendCommand(
                    MGMSG.MGMSG_MOT_SET_HOMEPARAMS,
                    this.bayID,
                    Convert.ToUInt16(this.chanId),
                    homeDirection,
                    limitSwitch,
                    homeVelocity,
                    offsetDistance);

                this.stageCom.SendCommand(
                    MGMSG.MGMSG_MOT_MOVE_HOME,
                    this.bayID,
                    this.chanId,
                    0x00);

                this.CheckForErrorCondition();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending 'Home' command.", ex);
            }
        }
        public virtual void Stop()
        {
            try
            {
                if (this.IsDisabled)
                {
                    return;
                }

                this.stageCom.SendCommand(
                    MGMSG.MGMSG_MOT_MOVE_STOP,
                    this.bayID,
                    this.chanId,
                    StopMode.Controlled);
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending 'Stop' command.", ex);
            }
        }

        public virtual void MoveAbsolute(double posUm)
        {
            try
            {
                if (this.IsDisabled)
                {
                    return;
                }

                var posEnc = this.ConvertAbsoluteUmToEncoder(posUm);

                this.MoveToAbsoluteEncoderPosition(posEnc);

                this.CheckForErrorCondition();
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending 'MoveAbsolute' command.", ex);
            }
        }

        public virtual void SetVelocity(double umPerSec)
        {
            try
            {
                if (this.IsDisabled)
                {
                    return;
                }

                // limit the velocity
                umPerSec = this.LimitVelocity(Math.Abs(umPerSec));

                // convert to native units
                this.cachedVelocity = this.ConvertVelocityUmPerSecToEncoder(umPerSec);

                // send the new velocity
                this.stageCom.SendCommand(
                    MGMSG.MGMSG_MOT_SET_VELPARAMS,
                    this.bayID,
                    Convert.ToUInt16(this.chanId),
                    Convert.ToUInt32(0),
                    Convert.ToUInt32(this.cachedAcceleration),
                    Convert.ToUInt32(this.cachedVelocity));
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending 'SetVelocity' command.", ex);
            }
        }
        public virtual void SetAcceleration(double umPerSec2)
        {
            try
            {
                if (this.IsDisabled)
                {
                    return;
                }

                // convert to native units
                this.cachedAcceleration = this.ConvertAccelUmPerSec2ToEncoder(umPerSec2);

                // send the new velocity
                this.stageCom.SendCommand(
                    MGMSG.MGMSG_MOT_SET_VELPARAMS,
                    this.bayID,
                    Convert.ToUInt16(this.chanId),
                    Convert.ToUInt32(0),
                    Convert.ToUInt32(this.cachedAcceleration),
                    Convert.ToUInt32(this.cachedVelocity));
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending 'SetAcceleration' command.", ex);
            }
        }

        public virtual bool IsMoving()
        {
            try
            {
                if (this.IsDisabled)
                {
                    return false;
                }

                var status = this.ReadStatus();

                this.CheckForErrorCondition(status);

                return 0 != (Thorlabs.Status.IsMovingMask & status);
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending 'IsMoving' command.", ex);
            }
        }

        public async Task<bool> IsMovingAsync()
        {
            try
            {
                if (this.IsDisabled)
                {
                    return false;
                }

                var status = await this.ReadStatusAsync().ConfigureAwait(false);

                this.CheckForErrorCondition(status);

                return 0 != (Thorlabs.Status.IsMovingMask & status);
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending 'IsMoving' command.", ex);
            }
        }

        #endregion

        #region Configuration

        [XmlType(TypeName = "StageAxisThorlabsBBD30x")]
        public class Configuration
        {
            public int Index;
            public int Direction;
            public double SpeedMin;
            public double SpeedMax;
            public double RangeMin;
            public double RangeMax;
            public double RangeOffset;
            public UInt16 HomeDirection = 1;
            public double EncoderCountsPerUm = 20;
            public double VelocityScaleEncPerUm = 134.21773;
            public double AccelScaleEncPerUm = 0.013744;

            #region Local classes

            public class PulseParam
            {
                public uint PulseWidth { get; set; } = 1000;    // microseconds

                public Pulse.Edge PulseEdge { get; set; } = Pulse.Edge.Low;

                public BBD30x.IOPort PulsePort { get; set; } = BBD30x.IOPort.BNC1;
            }

            #endregion
        }

        #endregion

        #region Private API

        internal Target DeviceIdent => this.bayID;

        internal UInt16 ChanIdent => this.chanId;

        internal double Acceleration => this.ConvertEncoderAccelToUmPerSec2(this.cachedAcceleration);
                     
        internal double EncCountPerUm => this.config?.EncoderCountsPerUm ?? 20;                 // MLS203, 20000 enc/mm

        internal double VelScaleUmPerSec => this.config?.VelocityScaleEncPerUm ?? 134.21773;    // MLS203, 134217.73 mm/s

        internal double AccelScaleUmPerSec2 => this.config?.AccelScaleEncPerUm ?? 0.013744;     // MLS203, 13.744 mm/s^2

        internal double UmToEncoderCount(double um) => um * this.EncCountPerUm;

        internal double EncoderCountToUm(double ustep) => ustep / this.EncCountPerUm;

        internal double ConvertEncoderToAbsoluteUm(double enc)
        {
            var minUm = this.config.RangeMin;
            var maxUm = this.config.RangeMax;
            var offsetUm = this.config.RangeOffset;
            var dir = this.config.Direction;

            if (dir > 0)
            {
                return minUm - offsetUm + this.EncoderCountToUm(enc);
            }
            else
            {
                return offsetUm + maxUm - this.EncoderCountToUm(enc);
            }
        }

        internal double ConvertAbsoluteUmToEncoderF(double um)
        {
            var minUm = this.config.RangeMin;
            var maxUm = this.config.RangeMax;
            var offsetUm = this.config.RangeOffset;
            var dir = this.config.Direction;

            if (dir > 0)
            {
                return this.UmToEncoderCount(offsetUm - minUm + um);
            }
            else
            {
                return this.UmToEncoderCount(offsetUm + maxUm - um);
            }
        }

        internal int ConvertAbsoluteUmToEncoder(double um)
        {
            return Convert.ToInt32(Math.Round(this.ConvertAbsoluteUmToEncoderF(um)));
        }

        internal UInt32 ConvertVelocityUmPerSecToEncoder(double umPerSec)
        {
            return Math.Max(1, Convert.ToUInt32(Math.Round(umPerSec * this.VelScaleUmPerSec)));
        }

        internal double ConvertEncoderVelocityToUmPerSec(UInt32 tlVel)
        {
            return tlVel / this.VelScaleUmPerSec;
        }

        internal UInt32 ConvertAccelUmPerSec2ToEncoder(double umPerSec2)
        {
            return Math.Max(1, Convert.ToUInt32(Math.Round(umPerSec2 * this.AccelScaleUmPerSec2)));
        }

        internal double ConvertEncoderAccelToUmPerSec2(UInt32 tlAccel)
        {
            return tlAccel / this.AccelScaleUmPerSec2;
        }

        internal UInt32 ReadStatus()
        {
            try
            {
                var packet = this.stageCom.SendAndReceive(
                    MGMSG.MGMSG_MOT_REQ_STATUSBITS,
                    MGMSG.MGMSG_MOT_GET_STATUSBITS,
                    this.bayID,
                    TimeSpan.FromMilliseconds(1000),
                    this.chanId);

                return BitConverter.ToUInt32(packet.RawBytes, 8);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading Thorlabs axis status.", ex);
            }
        }

        internal async Task<UInt32> ReadStatusAsync()
        {
            try
            {
                var packet = await this.stageCom.SendAndReceiveAsync(
                    MGMSG.MGMSG_MOT_REQ_STATUSBITS,
                    MGMSG.MGMSG_MOT_GET_STATUSBITS,
                    this.bayID,
                    TimeSpan.FromMilliseconds(1000),
                    this.chanId).ConfigureAwait(false);

                return BitConverter.ToUInt32(packet.RawBytes, 8);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading Thorlabs axis status.", ex);
            }
        }

        internal Int32 ReadEncoderPosition()
        {
            try
            {
                var packet = this.stageCom.SendAndReceive(
                    MGMSG.MGMSG_MOT_REQ_POSCOUNTER,
                    MGMSG.MGMSG_MOT_GET_POSCOUNTER,
                    this.bayID,
                    TimeSpan.FromMilliseconds(1000),
                    this.chanId);

                return BitConverter.ToInt32(packet.RawBytes, 8);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading Thorlabs axis position.", ex);
            }
        }

        internal async Task<Int32> ReadEncoderPositionAsync()
        {
            try
            {
                var packet = await this.stageCom.SendAndReceiveAsync(
                    MGMSG.MGMSG_MOT_REQ_POSCOUNTER,
                    MGMSG.MGMSG_MOT_GET_POSCOUNTER,
                    this.bayID,
                    TimeSpan.FromMilliseconds(1000),
                    this.chanId).ConfigureAwait(false);

                return BitConverter.ToInt32(packet.RawBytes, 8);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading Thorlabs axis position.", ex);
            }
        }

        internal void MoveToAbsoluteEncoderPosition(int enc)
        {
            var realRangeMin = this.config.RangeMin;
            var realRangeMax = this.config.RangeMax;
            var encMin = this.encoderMin;
            var encMax = this.encoderMax;

            // limit range

            enc = Math.Min(encMax, Math.Max(encMin, enc));

            this.commandedPos = enc;

            this.SendMoveCommand(MGMSG.MGMSG_MOT_MOVE_ABSOLUTE, enc);
        }

        internal void SendMoveCommand(MGMSG mgmsg, int encValue)
        {
            switch (mgmsg)
            {
                case MGMSG.MGMSG_MOT_MOVE_ABSOLUTE:
                case MGMSG.MGMSG_MOT_MOVE_RELATIVE:
                    break;

                default:
                    throw new ArgumentOutOfRangeException("mgmsg", mgmsg, "Incorrect usage of 'SendMoveCommand'");
            }

            this.stageCom.SendCommand(
                mgmsg,
                this.bayID,
                Convert.ToUInt16(this.chanId),
                Convert.ToInt32(encValue));
        }

        internal bool IsHomed()
        {
            try
            {
                if (this.IsDisabled)
                {
                    return false;
                }

                var status = this.ReadStatus();

                return 0 != (Status.ChannelIsEnabled & status)     // If these conditions are met
                    && 0 != (Status.Homed & status)                // we are ready to go, and the
                    && 0 == (Status.MotionError & status);         // initialize routine is not required.
            }
            catch (Exception ex)
            {
                throw new Exception("Error determining if axis is in the 'homed' state.", ex);
            }
        }

        internal void SetupPositionTriggers(
            double startPos_um,
            double increment_um,
            int count,
            Configuration.PulseParam syncPulse,
            bool biDirectional,
            int repeatCount)
        {
            if (0 >= count)
            {
                throw new ArgumentOutOfRangeException("count", count, "Position trigger pulse count may not be less than or equal to zero.");
            }

            if (0 >= repeatCount)
            {
                throw new ArgumentOutOfRangeException("repeat count", repeatCount, "Position trigger repeat count may not be less than or equal to zero.");
            }

            // calculate encoder positions for first pulse and last pulse
            var start_encf = this.ConvertAbsoluteUmToEncoderF(startPos_um);
            var end_encf = this.ConvertAbsoluteUmToEncoderF(startPos_um + (count - 1) * increment_um);
            var increment_encf = (end_encf - start_encf) / (count - 1);

            var uFSTRT = Convert.ToUInt32(Math.Round(start_encf));
            var INCR = Convert.ToInt32(Math.Round(increment_encf));

            // trigger generator must be disarmed to accept changes
            this.SetPositionTriggerEnableState(BBD30x.TrigOutState.Disarm);

            // program the trigger port
            this.ProgramTriggerPort(syncPulse.PulsePort, target: Target.GenericUSB);

            // program the trigger generator
            this.ProgramPositionTriggers(syncPulse, biDirectional, uFSTRT, INCR, Convert.ToUInt32(count), Convert.ToUInt32(repeatCount));

            // Arm the trigger generator
            this.SetPositionTriggerEnableState(BBD30x.TrigOutState.Arm);
        }

        internal void ClearPositionTriggers()
        {
            // disarm the trigger generator
            this.SetPositionTriggerEnableState(BBD30x.TrigOutState.Disarm);
        }

        internal void SetPositionTriggerEnableState(BBD30x.TrigOutState enState, Target? target = null, byte? chanId = null)
        {
            this.stageCom.SendCommand(
                MGMSG.MGMSG_MOT_SET_POSTRIGENSTATE,
                target ?? this.bayID,
                chanId ?? this.chanId,
                Convert.ToByte(enState));
        }

        internal BBD30x.TrigOutState GetPositionTriggerEnableState(Target? target = null, byte? chanId = null)
        {
            var p = this.stageCom.SendAndReceive(
                MGMSG.MGMSG_MOT_REQ_POSTRIGENSTATE,
                MGMSG.MGMSG_MOT_GET_POSTRIGENSTATE,
                target ?? this.bayID,
                TimeSpan.FromSeconds(1),
                chanId ?? this.chanId);

            var channelIdent = p.RawBytes[2];
            var state = p.RawBytes[3];

            return (BBD30x.TrigOutState)state;
        }

        internal void ProgramTriggerPort(BBD30x.IOPort port, Target? target = null)
        {
            this.stageCom.SendCommand(
                MGMSG.MGMSG_MOT_SET_IOCONFIG,
                target ?? Target.RackController,
                Convert.ToUInt16(port),
                Convert.ToUInt16(BBD30x.IOMode.DigOut),
                Convert.ToUInt16(this.config.Index));
        }

        internal BBD30x.IOPort GetTriggerPort(Target? target = null, byte? chanId = null)
        {
            var p = this.stageCom.SendAndReceive(
                MGMSG.MGMSG_MOT_REQ_IOCONFIG,
                MGMSG.MGMSG_MOT_GET_IOCONFIG,
                target ?? Target.RackController,
                TimeSpan.FromSeconds(1),
                chanId ?? this.chanId);

            var port = BitConverter.ToUInt16(p.RawBytes, 6);
            var mode = BitConverter.ToUInt16(p.RawBytes, 8);
            var outSource = BitConverter.ToUInt16(p.RawBytes, 10);

            return (BBD30x.IOPort)port;
        }

        internal void ProgramPositionTriggers(
            Configuration.PulseParam syncPulse,
            bool biDirectional,
            uint start_enc,
            int incr_enc,
            uint pulseCount,
            uint repeatCount,
            Target? target = null)
        {
            if (0 == incr_enc)
            {
                throw new ArgumentOutOfRangeException("increment", incr_enc, "Encoder increment may not be set to zero.");
            }

            if (0 == pulseCount)
            {
                throw new ArgumentOutOfRangeException("pulse count", pulseCount, "Position trigger pulse count may not be zero.");
            }

            ///////////////////////////////////////////////////////////////
            // declare the data types that are required for the data packet

            var channel = default(ushort);

            var trigInMode = default(ushort);
            var trigInPolarity = default(ushort);
            var trigInSource = default(ushort);

            var trigOutMode = default(ushort);
            var trigOutPolarity = default(ushort);

            var startPosFwd = default(uint);
            var intervalFwd = default(uint);
            var numPulsesFwd = default(uint);

            var startPosRev = default(uint);
            var intervalRev = default(uint);
            var numPulsesRev = default(uint);

            var pulseWidth = default(uint);
            var numCycles = default(uint);

            var reserved = default(ushort);


            ///////////////////////////
            // populate the packet data 

            channel = Convert.ToUInt16(this.config.Index);

            trigInMode = Convert.ToUInt16(BBD30x.TrigInMode.Disabled);
            trigInPolarity = Convert.ToUInt16(BBD30x.TrigPolarity.Low);
            trigInSource = Convert.ToUInt16(BBD30x.TrigInSource.Software);

            if (incr_enc > 0)
            {
                // we are performing a forwrd trigger scan

                trigOutMode = Convert.ToUInt16(BBD30x.TrigOutMode.AtPositionStepsFwd);

                startPosFwd = start_enc;
                intervalFwd = Convert.ToUInt32(incr_enc);

                startPosRev = startPosFwd + (pulseCount - 1) * intervalFwd;
                intervalRev = intervalFwd;
            }
            else
            {
                // we are performing a reverse trigger scan

                trigOutMode = Convert.ToUInt16(BBD30x.TrigOutMode.AtPositionStepsRev);

                startPosRev = start_enc;
                intervalRev = Convert.ToUInt32(-incr_enc);

                startPosFwd = startPosRev - (pulseCount - 1) * intervalRev;
                intervalFwd = intervalRev;
            }

            numPulsesFwd = pulseCount;
            numPulsesRev = pulseCount;

            if (biDirectional)
            {
                trigOutMode = Convert.ToUInt16(BBD30x.TrigOutMode.AtPositionStepsBoth);
            }
            trigOutPolarity = Convert.ToUInt16((Pulse.Edge.Low == syncPulse.PulseEdge) ? BBD30x.TrigPolarity.Low : BBD30x.TrigPolarity.High);

            pulseWidth = syncPulse.PulseWidth; // microseconds
            if (1 > pulseWidth || 1000000 < pulseWidth)
            {
                throw new ArgumentOutOfRangeException("pulse width", pulseWidth, "BBD30x trigger pulse width must be between 1 and 1000000");
            }

            numCycles = repeatCount;


            ///////////////////////////
            // Transmit the data packet

            this.stageCom.SendCommand(
                MGMSG.MGMSG_MOT_SET_MOTTRIGIOCONFIG,
                target ?? this.bayID,
                channel,
                trigInMode,
                trigInPolarity,
                trigInSource,
                trigOutMode,
                trigOutPolarity,
                startPosFwd,
                intervalFwd,
                numPulsesFwd,
                startPosRev,
                intervalRev,
                numPulsesRev,
                pulseWidth,
                numCycles,
                reserved);
        }

        internal void GetPositionTriggers(Target? target = null, byte? chanId = null)
        {
            var p = this.stageCom.SendAndReceive(
                MGMSG.MGMSG_MOT_REQ_MOTTRIGIOCONFIG,
                MGMSG.MGMSG_MOT_GET_MOTTRIGIOCONFIG,
                target ?? this.bayID,
                TimeSpan.FromSeconds(1),
                chanId ?? this.chanId);


            Console.WriteLine(p);

            var channel = BitConverter.ToUInt16(p.RawBytes, 6);

            var trigInMode = BitConverter.ToUInt16(p.RawBytes, 8);
            var trigInPolarity = BitConverter.ToUInt16(p.RawBytes, 10);
            var trigInSource = BitConverter.ToUInt16(p.RawBytes, 12);

            var trigOutMode = BitConverter.ToUInt16(p.RawBytes, 14);
            var trigOutPolarity = BitConverter.ToUInt16(p.RawBytes, 16);

            var startPosFwd = BitConverter.ToUInt32(p.RawBytes, 18);
            var intervalFwd = BitConverter.ToUInt32(p.RawBytes, 22);
            var numPulsesFwd = BitConverter.ToUInt32(p.RawBytes, 26);

            var startPosRev = BitConverter.ToUInt32(p.RawBytes, 30);
            var intervalRev = BitConverter.ToUInt32(p.RawBytes, 34);
            var numPulsesRev = BitConverter.ToUInt32(p.RawBytes, 38);

            var pulseWidth = BitConverter.ToUInt32(p.RawBytes, 42);
            var numCycles = BitConverter.ToUInt32(p.RawBytes, 46);
        }

        internal void CheckForErrorCondition(uint status)
        {
            try
            {
                if (0 != (Thorlabs.Status.MotionError & status))
                {
                    throw new Exception(string.Format("Stage at '{0}' is reporting an error condition.", this.bayID.ToString()));
                }
            }
            catch (Exception ex)
            {
                try
                {
                    this.Stop();
                }
                catch (Exception eeee)
                {
                    ex = new AggregateException(new Exception[] { ex, eeee });
                }

                throw ex;
            }
        }

        internal void CheckForErrorCondition()
        {
            if (this.IsDisabled)
            {
                return;
            }

            this.CheckForErrorCondition(this.ReadStatus());
        }

        internal async Task CheckForErrorConditionAsync()
        {
            if (this.IsDisabled)
            {
                return;
            }

            this.CheckForErrorCondition(await this.ReadStatusAsync());
        }

        private int _encoderPos = 0;
        private System.Timers.Timer ackTimer;
        private void RxUpdatePacket(StageComThorlabs.Packet packet)
        {
            this._encoderPos = BitConverter.ToInt32(packet.RawBytes, 8);
        }

        private void StartStatusUpdates()
        {
            this.KillAckTimer();

            if (this.stageCom?.IsConnected() ?? false)
            {
                this.stageCom?.Subscribe(MGMSG.MGMSG_MOT_GET_DCSTATUSUPDATE, this.DeviceIdent, this.RxUpdatePacket);
                this.stageCom?.SendCommand(MGMSG.MGMSG_HW_START_UPDATEMSGS, this.bayID);
                this.stageCom?.SendCommand(MGMSG.MGMSG_HW_START_UPDATEMSGS, Target.RackController);

                this.stageCom?.SendCommand(MGMSG.MGMSG_MOT_RESUME_ENDOFMOVEMSGS, this.bayID);
                this.stageCom?.SendCommand(MGMSG.MGMSG_MOT_RESUME_ENDOFMOVEMSGS, Target.RackController);
            }

            this.StartAckTimer();
        }

        private void StopStatusUpdates()
        {
            this.KillAckTimer();

            if (this.stageCom?.IsConnected() ?? false)
            {
                this.stageCom?.SendCommand(MGMSG.MGMSG_MOT_SUSPEND_ENDOFMOVEMSGS, this.bayID);
                this.stageCom?.SendCommand(MGMSG.MGMSG_MOT_SUSPEND_ENDOFMOVEMSGS, Target.RackController);

                this.stageCom?.SendCommand(MGMSG.MGMSG_HW_STOP_UPDATEMSGS, Target.RackController);
                this.stageCom?.SendCommand(MGMSG.MGMSG_HW_STOP_UPDATEMSGS, this.DeviceIdent);
                this.stageCom?.Unsubscribe(MGMSG.MGMSG_MOT_GET_DCSTATUSUPDATE, this.DeviceIdent, this.RxUpdatePacket);
            }
        }

        private void StartAckTimer()
        {
            this.ackTimer = new System.Timers.Timer();
            this.ackTimer.Elapsed += this.AckTimer_Elapsed;
            this.ackTimer.Interval = 1000;
            this.ackTimer.Enabled = true;
        }

        private void KillAckTimer()
        {
            if (null != this.ackTimer)
            {
                this.ackTimer.Stop();
                this.ackTimer.Elapsed -= this.AckTimer_Elapsed;
                this.ackTimer.Dispose();
                this.ackTimer = null;
            }
        }

        private void AckTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.stageCom.SendPacket(new StageComThorlabs.Packet(MGMSG.MGMSG_MOT_ACK_DCSTATUSUPDATE, this.DeviceIdent));
        }

        public virtual double LimitVelocity(double micronsPerSec)
        {
            double sign = micronsPerSec < 0 ? -1 : 1;
            double limited_velocity = Math.Max(this.config.SpeedMin, Math.Min(this.config.SpeedMax, Math.Abs(micronsPerSec)));

            return sign * limited_velocity;
        }

        #endregion
    }
}
