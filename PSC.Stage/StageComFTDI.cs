using FTD2XX_NET;
using System;
using System.Threading;
using FT_EXCEPTION = FTD2XX_NET.FTDI.FT_EXCEPTION;
using FT_STATUS = FTD2XX_NET.FTDI.FT_STATUS;

namespace PSC.Stage
{
    public class StageComFTDI
    {
        #region Private fields

        protected FTDI ftdi;
        protected string _serialNumber;
        protected string _description;

        #endregion

        #region Constructors

        public StageComFTDI()
        {
            this.SyncObj = new object();
        }

        #endregion

        #region Public Properties

        public string SerialNumber
        {
            get
            {
                if (this.IsConnected())
                {
                    string sn;
                    this.ftdi.GetSerialNumber(out sn);
                    return sn;
                }
                if (!string.IsNullOrWhiteSpace(this._serialNumber))
                {
                    return this._serialNumber;
                }
                return "<unknown>";
            }
            set
            {
                if (this.IsConnected())
                {
                    throw new Exception("You may not set the SerialNumber property after opening an FTDI connection.");
                }

                this._serialNumber = value;
            }
        }

        public string Description
        {
            get
            {
                if (this.IsConnected())
                {
                    string desc;
                    this.ftdi.GetDescription(out desc);
                    return desc;
                }
                if (!string.IsNullOrWhiteSpace(this._description))
                {
                    return this._description;
                }
                return "<none>";
            }
            set
            {
                if (this.IsConnected())
                {
                    throw new Exception("You may not set the Description property after opening an FTDI connection.");
                }

                this._description = value;
            }
        }

        public object SyncObj
        {
            get;
            private set;
        }

        #endregion

        #region StageCom Overrides

        public virtual void Connect()
        {
            try
            {
                this.ftdi = new FTDI();

                if (!string.IsNullOrWhiteSpace(this._serialNumber))     // serial number has priority (if provided)
                {
                    this.ftdi.OpenBySerialNumber(this._serialNumber).ThrowOnError();
                }
                else if (!string.IsNullOrWhiteSpace(this._description)) // try description (if provided)
                {
                    this.ftdi.OpenByDescription(this._description).ThrowOnError();
                }
                else
                {
                    this.ftdi.OpenByIndex(0U).ThrowOnError();  // just go with the first one if nothing else specified.
                }


                ///////////////////////////////////////////////////////////////////////////////
                // TODO: Get the baud-rate, characteristics, etc parameters from elsewhere.  //
                //       For now, hard code this for the Thorlabs controllers.               //
                ///////////////////////////////////////////////////////////////////////////////



                // These are the Thorlabs connection instructions:
                //
                //
                //    // Set baud rate to 115200.
                //    ftStatus = FT_SetBaudRate(m_hFTDevice, (ULONG)uBaudRate);
                //    // 8 data bits, 1 stop bit, no parity
                //    ftStatus = FT_SetDataCharacteristics(m_hFTDevice, FT_BITS_8, FT_STOP_BITS_1, FT_PARITY_NONE);
                //    // Pre purge dwell 50ms.
                //    Sleep(uPrePurgeDwell);
                //    // Purge the device.
                //    ftStatus = FT_Purge(m_hFTDevice, FT_PURGE_RX | FT_PURGE_TX);
                //    // Post purge dwell 50ms.
                //    Sleep(uPostPurgeDwell);
                //
                //    // Reset device.
                //    ftStatus = FT_ResetDevice(m_hFTDevice);
                //    // Set flow control to RTS/CTS.
                //    ftStatus = FT_SetFlowControl(m_hFTDevice, FT_FLOW_RTS_CTS, 0, 0);
                //    // Set RTS.
                //    ftStatus = FT_SetRts(m_hFTDevice);
                //
                //

                // Set baud rate to 115200.
                this.ftdi.SetBaudRate(115200).ThrowOnError();

                // 8 data bits, 1 stop bit, no parity
                this.ftdi.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE).ThrowOnError();

                // Pre purge dwell 50ms.
                Thread.Sleep(TimeSpan.FromMilliseconds(50));

                // Purge the device.
                this.ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX).ThrowOnError();

                // Post purge dwell 50ms.
                Thread.Sleep(TimeSpan.FromMilliseconds(50));

                // Reset device.
                this.ftdi.ResetDevice().ThrowOnError();

                // Set flow control to RTS/CTS.
                this.ftdi.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0, 0).ThrowOnError();

                // Set RTS.
                this.ftdi.SetRTS(true).ThrowOnError();
            }
            catch (Exception ex)
            {
                throw new Exception("Error connecting to FTDI device.", ex);
            }
        }

        public virtual void Disconnect()
        {
            try
            {
                this.ftdi.Close().ThrowOnError();
            }
            catch (Exception ex)
            {
                throw new Exception("Error disconnecting from FTDI device.", ex);
            }
        }

        public virtual bool IsConnected() => this.ftdi?.IsOpen ?? false;

        public virtual void Dispose()
        {
            try
            {
                if (this.ftdi?.IsOpen ?? false)
                {
                    this.ftdi.Close();
                }

                this.ftdi = null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error disposing of FTDI device connection.", ex);
            }
        }

        #endregion
    }

    #region HELPER_METHODS

    public static class FTDIExtensions
    {
        public static void ThrowOnError(this FT_STATUS ftStatus)
        {
            if (ftStatus != FT_STATUS.FT_OK)
            {
                // Check FT_STATUS values returned from FTD2XX DLL calls
                switch (ftStatus)
                {
                    case FT_STATUS.FT_DEVICE_NOT_FOUND:
                        {
                            throw new FT_EXCEPTION("FTDI device not found.");
                        }
                    case FT_STATUS.FT_DEVICE_NOT_OPENED:
                        {
                            throw new FT_EXCEPTION("FTDI device not opened.");
                        }
                    case FT_STATUS.FT_DEVICE_NOT_OPENED_FOR_ERASE:
                        {
                            throw new FT_EXCEPTION("FTDI device not opened for erase.");
                        }
                    case FT_STATUS.FT_DEVICE_NOT_OPENED_FOR_WRITE:
                        {
                            throw new FT_EXCEPTION("FTDI device not opened for write.");
                        }
                    case FT_STATUS.FT_EEPROM_ERASE_FAILED:
                        {
                            throw new FT_EXCEPTION("Failed to erase FTDI device EEPROM.");
                        }
                    case FT_STATUS.FT_EEPROM_NOT_PRESENT:
                        {
                            throw new FT_EXCEPTION("No EEPROM fitted to FTDI device.");
                        }
                    case FT_STATUS.FT_EEPROM_NOT_PROGRAMMED:
                        {
                            throw new FT_EXCEPTION("FTDI device EEPROM not programmed.");
                        }
                    case FT_STATUS.FT_EEPROM_READ_FAILED:
                        {
                            throw new FT_EXCEPTION("Failed to read FTDI device EEPROM.");
                        }
                    case FT_STATUS.FT_EEPROM_WRITE_FAILED:
                        {
                            throw new FT_EXCEPTION("Failed to write FTDI device EEPROM.");
                        }
                    case FT_STATUS.FT_FAILED_TO_WRITE_DEVICE:
                        {
                            throw new FT_EXCEPTION("Failed to write to FTDI device.");
                        }
                    case FT_STATUS.FT_INSUFFICIENT_RESOURCES:
                        {
                            throw new FT_EXCEPTION("Insufficient resources.");
                        }
                    case FT_STATUS.FT_INVALID_ARGS:
                        {
                            throw new FT_EXCEPTION("Invalid arguments for FTD2XX function call.");
                        }
                    case FT_STATUS.FT_INVALID_BAUD_RATE:
                        {
                            throw new FT_EXCEPTION("Invalid Baud rate for FTDI device.");
                        }
                    case FT_STATUS.FT_INVALID_HANDLE:
                        {
                            throw new FT_EXCEPTION("Invalid handle for FTDI device.");
                        }
                    case FT_STATUS.FT_INVALID_PARAMETER:
                        {
                            throw new FT_EXCEPTION("Invalid parameter for FTD2XX function call.");
                        }
                    case FT_STATUS.FT_IO_ERROR:
                        {
                            throw new FT_EXCEPTION("FTDI device IO error.");
                        }
                    case FT_STATUS.FT_OTHER_ERROR:
                        {
                            throw new FT_EXCEPTION("An unexpected error has occurred when trying to communicate with the FTDI device.");
                        }
                    default:
                        break;
                }
            }
        }
    }

    #endregion
}
