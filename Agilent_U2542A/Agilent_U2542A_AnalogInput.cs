﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Agilent_U2542A
{
    public class Agilent_U2542A_AnalogInput
    {
        #region Agilent_U2542A_AnalogInput settings

        private AgilentUSB_Device _Device;

        #endregion

        #region Constructor

        public Agilent_U2542A_AnalogInput()
        {
            _Device = AgilentUSB_Device.Instance;

            if (!_Device.IsAlive)
                _Device.InitDevice();
            if (!_Device.IsAlive)
                throw new Exception("Device Not Connected");
        }

        #endregion

        #region Agilent U2542A functionality

        #region Checking Aquistion status

        /// <summary>
        /// Checks the status of the device.
        /// </summary>
        /// <returns>
        /// true, if data for reading is avaliable and false otherwise.
        /// If overload appears, the exception is thrown
        /// </returns>
        public bool CheckAcquisitionStatus()
        {
            string status = _Device.RequestQuery("WAV:STAT?");

            if (status == "OVER")
                throw new Exception("Device buffer overload");
            if (status == "DATA") 
                return true;

            return false;
        }

        /// <summary>
        /// Checks the status of the device. in single shot mode
        /// </summary>
        /// <returns>
        /// true, if the data for reading is avaliable and false otherwise
        /// </returns>
        public bool CheckSingleShotAcquisitionStatus()
        {
            string status = _Device.RequestQuery("WAV:COMP?");

            if (status == "NO") 
                return false;
            if (status == "YES") 
                return true;

            return false;
        }

        #endregion

        #region Read / Write operations

        public void tryToWriteString(string WhatToWrite)
        {
            try { _Device.SendCommandRequest(WhatToWrite); }
            catch (Exception e) { throw e; }
        }

        public string tryToQueryString(string WhatToWrite)
        {
            try
            {
                return _Device.RequestQuery(WhatToWrite);
            }
            catch (Exception e) 
            {
                var mes = e.Message;
                return null; 
            }
        }

        /// <summary>
        /// Requests raw ADC data
        /// </summary>
        /// <returns></returns>
        public string AcquireRawADC_Data()
        {
            return tryToQueryString("WAV:DATA?");//_Device.RequestQuery("WAV:DATA?");
        }

        #endregion

        #endregion
    }
}
