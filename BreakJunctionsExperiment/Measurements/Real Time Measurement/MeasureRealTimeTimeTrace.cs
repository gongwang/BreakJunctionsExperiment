﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Agilent_U2542A_With_ExtensionBox.Classes;
using Agilent_U2542A_With_ExtensionBox.Interfaces;

using BreakJunctions.Motion;
using BreakJunctions.Events;
using System.Threading;

namespace BreakJunctions.Measurements
{
    class MeasureRealTimeTimeTrace : IDisposable
    {
        #region MeasureRealTimeTimeTrace settings

        #region Motion settings

        private double _StartPosition;
        /// <summary>
        /// Gets or sets the start potition of the motor (meters)
        /// </summary>
        public double StartPosition
        {
            get { return _StartPosition; }
            set 
            {
                _TimeTraceMotionController.StartPosition = value;
                _StartPosition = value; 
            }
        }

        private double _CurrentPosition = 0.0;
        /// <summary>
        /// Gets the current position of the motor (meters)
        /// </summary>
        public double CurrentPosition
        {
            get { return _CurrentPosition; }
        }

        private double _FinalDestination;
        /// <summary>
        /// Gets or sets the final position of the motor, to be
        /// reached in measurement process (meters)
        /// </summary>
        public double FinalDestination
        {
            get { return _FinalDestination; }
            set 
            {
                _TimeTraceMotionController.FinalDestination = value;
                _FinalDestination = value; 
            }
        }

        private double _VelosityMovingUp = 2.5;
        public double VelosityMovingUp
        {
            get { return _VelosityMovingUp; }
            set 
            {
                _VelosityMovingUp = value;
                _TimeTraceMotionController.VelosityMovingUp = value;
            }
        }

        private double _VelosityMovingDown = 3.0;
        public double VelosityMovingDown
        {
            get { return _VelosityMovingDown; }
            set 
            {
                _VelosityMovingDown = value;
                _TimeTraceMotionController.VelosityMovingDown = value;
            }
        }

        public MotionVelosityUnits MotionVelosityUnits_Value { get; set; }

        private bool _IsMeasurementInProcess;

        private IMotionFactory _IRealTimeMotionFactory;
        private MotionController _TimeTraceMotionController;

        #endregion

        #region Acquisition settings

        private int _PointsPerBlock = 200;
        /// <summary>
        /// The value of points per block for Agilent U2542A
        /// </summary>
        public int PointsPerBlock
        {
            get { return _PointsPerBlock; }
            set { _PointsPerBlock = value; }
        }

        private int _AcquistionRate = 1000;
        /// <summary>
        /// The value of points per second, generated by
        /// simultaneous data acquisition Agilent U2542A
        /// </summary>
        public int AcquistionRate
        {
            get { return _AcquistionRate; }
            set { _AcquistionRate = value; }
        }

        private bool _IsSample_01_MeasurementEnabled = true;
        /// <summary>
        /// Sample 01 measurement enable indicator
        /// </summary>
        public bool IsSample_01_MeasurementEnabled
        {
            get { return _IsSample_01_MeasurementEnabled; }
            set { _IsSample_01_MeasurementEnabled = value; }
        }

        private bool _IsSample_02_MeasurementEnabled = true;
        /// <summary>
        /// Sample 02 measurement enable indicator
        /// </summary>
        public bool IsSample_02_MeasurementEnabled
        {
            get { return _IsSample_02_MeasurementEnabled; }
            set { _IsSample_02_MeasurementEnabled = value; }
        }

        private AI_Channels _Channels;

        private IRealTime_TimeTrace_Factory _ITimeTraceControllerFactory;
        private RealTime_TimeTrace_Controller _TimeTraceMeasurementControler;

        private Thread StartAcquisitionThread;

        #endregion

        #endregion

        #region Constructor / Destructor

        public MeasureRealTimeTimeTrace()
        {
            _ITimeTraceControllerFactory = new RT_Agilent_U2542A_TimeTrace_Controller_Factory();
            _TimeTraceMeasurementControler = _ITimeTraceControllerFactory.GetRealTime_TimeTraceController();

            _IRealTimeMotionFactory = new FaulhaberMinimotor_SA_2036_U012V_K1155_RealTime_ControllerFactory("COM1"); // Modification here to establish correct serial port needed
            _TimeTraceMotionController = _IRealTimeMotionFactory.GetMotionController();

            AllEventsHandler.Instance.RealTime_TimeTraceMeasurementStateChanged += OnRealTime_TimeTraceMeasurement_StateChanged;

            AllEventsHandler.Instance.Motion_RealTime_StartPositionReached += OnMotion_RealTime_StartPositionReached;
            AllEventsHandler.Instance.Motion_RealTime_FinalDestinationReached += OnMotion_RealTime_FinalDestinationReached;
        }

        ~MeasureRealTimeTimeTrace()
        {
            this.Dispose();
        }

        #endregion

        #region MeasureRealTimeTimeTrace functionality

        private void _initDAC()
        {
            _Channels = AI_Channels.Instance;

            _Channels.DisableAllChannelsForContiniousDataAcquisition();
            _Channels.PointsPerBlock = this.PointsPerBlock;
            _Channels.ACQ_Rate = AcquistionRate;

            this.ReloadChannels();
        }

        private void ReloadChannels()
        {
            _Channels.DisableAllChannelsForContiniousDataAcquisition();

            if(_IsSample_01_MeasurementEnabled == true)
            {
                _Channels.ChannelArray[0].Enabled = true;
                _Channels.ChannelArray[1].Enabled = true;
            }
            else
            {
                _Channels.ChannelArray[0].Enabled = false;
                _Channels.ChannelArray[1].Enabled = false;
            }
            if (_IsSample_02_MeasurementEnabled == true)
            {
                _Channels.ChannelArray[2].Enabled = true;
                _Channels.ChannelArray[3].Enabled = true;
            }
            else
            {
                _Channels.ChannelArray[2].Enabled = false;
                _Channels.ChannelArray[3].Enabled = false;
            }
        }

        public void StartMeasurement(MotionKind __MotionKind, int __NumberOfRepetities = 1)
        {
            _initDAC();
            
            AllEventsHandler.Instance.OnRealTime_TimeTraceMeasurementStateChanged(this, new RealTime_TimeTraceMeasurementStateChanged_EventArgs(true));
            AllEventsHandler.Instance.OnRealTime_TimeTrace_ResetTimeShift(this, new RealTime_TimeTrace_ResetTimeShift_EventArgs());

            _TimeTraceMotionController.StartMotion(_StartPosition, _FinalDestination, __MotionKind, __NumberOfRepetities);
        }

        public void StopMeasurement()
        {
            AllEventsHandler.Instance.OnRealTime_TimeTraceMeasurementStateChanged(this, new RealTime_TimeTraceMeasurementStateChanged_EventArgs(false));
        }

        public void StartContiniousAcquisitionInThread()
        {
            _TimeTraceMeasurementControler.MeasurementInProcess = true;

            StartAcquisitionThread = new Thread(_TimeTraceMeasurementControler.ContiniousAcquisition);

            StartAcquisitionThread.Priority = ThreadPriority.Highest;
            StartAcquisitionThread.Start();
        }

        public void StopContiniousAcquisitionInThread()
        {
            _TimeTraceMeasurementControler.MeasurementInProcess = false;
        }

        private void OnMotion_RealTime_StartPositionReached(object sender, Motion_RealTime_StartPositionReached_EventArgs e)
        {
            if(_IsMeasurementInProcess == true)
                StartContiniousAcquisitionInThread();
        }

        private void OnMotion_RealTime_FinalDestinationReached(object sender, Motion_RealTime_FinalDestinationReached_EventArgs e)
        {
            StopContiniousAcquisitionInThread();
        }

        private void OnRealTime_TimeTraceMeasurement_StateChanged(object sender, RealTime_TimeTraceMeasurementStateChanged_EventArgs e)
        {
            _IsMeasurementInProcess = e.MeasurementInProcess;
            
            if(_IsMeasurementInProcess == false)
                _TimeTraceMeasurementControler.MeasurementInProcess = false;
        }

        #endregion

        #region Correctly disposing the instance

        public void Dispose()
        {
            AllEventsHandler.Instance.Motion_RealTime_StartPositionReached -= OnMotion_RealTime_StartPositionReached;
            AllEventsHandler.Instance.Motion_RealTime_FinalDestinationReached -= OnMotion_RealTime_FinalDestinationReached;

            if(_TimeTraceMeasurementControler != null)
                _TimeTraceMeasurementControler.Dispose();

            if (_TimeTraceMotionController != null)
                _TimeTraceMotionController.Dispose();
        }

        #endregion
    }
}
