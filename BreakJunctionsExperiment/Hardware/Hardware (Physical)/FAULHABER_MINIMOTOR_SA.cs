﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.IO.Ports;

using System.Threading;
using System.Windows;
using System.Windows.Threading;

using BreakJunctions.Events;

namespace Hardware
{
    class FAULHABER_MINIMOTOR_SA : IMotion, IDisposable
    {
        public static readonly double _metersPerSecond = 0.000003389831;

        #region COM Port settings

        private SerialPort _MotionSerialPort;

        private string _comPort;
        private int _baud;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;
        private string _returnToken;

        #endregion

        #region Motion settings

        private double _CurrentTime = 0.0;

        private double _CurrentPosition = 0.0;
        public double CurrentPosition
        {
            get { return _CurrentPosition; }
            set { _CurrentPosition = value; }
        }

        private double _StartPosition = 0.0;
        public double StartPosition
        {
            get { return _StartPosition; }
            set { _StartPosition = value; }
        }

        private double _FinalDestination = 0.0;
        public double FinalDestination
        {
            get { return _FinalDestination; }
            set { _FinalDestination = value; }
        }

        private int _CurrentIteration = 0;
        private int _NumberRepetities = 0;
        public int NumberRepetities
        {
            get { return _NumberRepetities; }
            set { _NumberRepetities = value; }
        }

        private DispatcherTimer _MotionSingleMeasurementTimer;
        private DispatcherTimer _MotionRepetitiveMeasurementTimer;

        private MotionDirection _CurrentDirection;

        #endregion

        public FAULHABER_MINIMOTOR_SA()
        {
            _MotionSingleMeasurementTimer = new DispatcherTimer();
            _MotionSingleMeasurementTimer.Interval = TimeSpan.FromMilliseconds(5);
            _MotionSingleMeasurementTimer.Tick += new EventHandler(_MotionSingleMeasurementTimer_Tick);

            _MotionRepetitiveMeasurementTimer = new DispatcherTimer();
            _MotionRepetitiveMeasurementTimer.Interval = TimeSpan.FromMilliseconds(5);
            _MotionRepetitiveMeasurementTimer.Tick += new EventHandler(_MotionRepettiiveMeasurementTimer_Tick);
        }

        ~FAULHABER_MINIMOTOR_SA()
        {
            Dispose();
        }

        public void SetSerialPort(string comPort = "COM1", int baud = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One, string returnToken = ">")
        {
            this._comPort = comPort;
            this._baud = baud;
            this._parity = parity;
            this._dataBits = dataBits;
            this._stopBits = stopBits;
            this._returnToken = returnToken;
        }

        public bool InitDevice()
        {
            try
            {
                _MotionSerialPort = new SerialPort(_comPort, _baud, _parity);

                _MotionSerialPort.NewLine = _returnToken;
                _MotionSerialPort.ReadTimeout = 1000;
                _MotionSerialPort.RtsEnable = true;
                _MotionSerialPort.DtrEnable = true;

                _MotionSerialPort.Open();

                _MotionSerialPort.WriteLine("EN");
                Thread.Sleep(100);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void StartMotion(double StartPosition, double FinalDestination, MotionKind motionKind, int numberOfRepetities = 1)
        {
            _StartPosition = StartPosition;
            _CurrentPosition = StartPosition;
            _FinalDestination = FinalDestination;
            _NumberRepetities = numberOfRepetities;

            switch (motionKind)
            {
                case MotionKind.Single:
                    {
                        _MotionSingleMeasurementTimer.Start();
                    } break;
                case MotionKind.Repetitive:
                    {
                        _MotionRepetitiveMeasurementTimer.Start();
                    } break;
                default:
                    break;
            }
        }

        public void StartMotion(TimeSpan FinalTime)
        {
            throw new NotImplementedException();
        }

        public void StartMotion(double FixedR)
        {
            throw new NotImplementedException();
        }

        public void StopMotion()
        {
            if (_MotionSingleMeasurementTimer.IsEnabled == true)
                _MotionSingleMeasurementTimer.Stop();
            if (_MotionRepetitiveMeasurementTimer.IsEnabled == true)
                _MotionRepetitiveMeasurementTimer.Stop();

            AllEventsHandler.Instance.OnTimeTraceMeasurementsStateChanged(null, new TimeTraceMeasurementStateChanged_EventArgs(false));
        }

        public void SetVelosity(double VelosityValue, MotionVelosityUnits VelosityUnits)
        {
            throw new NotImplementedException();
        }

        public void SetDirection(MotionDirection motionDirection)
        {
            if (_CurrentDirection != motionDirection)
            {
                _CurrentDirection = motionDirection;
                ++_CurrentIteration;
            }

            switch (motionDirection)
            {
                case MotionDirection.Up:
                    {
                    } break;
                case MotionDirection.Down:
                    {
                    } break;
                default:
                    break;
            }
        }

        void _MotionSingleMeasurementTimer_Tick(object sender, EventArgs e)
        {
            _CurrentTime += _MotionSingleMeasurementTimer.Interval.Milliseconds;

            var positionPerTick = _MotionSingleMeasurementTimer.Interval.Milliseconds / 1000 * _metersPerSecond;

            if (_CurrentPosition <= _FinalDestination)
            {
                this.SetDirection(MotionDirection.Up);
                _CurrentPosition += positionPerTick;
                if (_CurrentPosition >= _FinalDestination)
                    StopMotion();
            }
            else
            {
                this.SetDirection(MotionDirection.Down);
                _CurrentPosition -= positionPerTick;
                if (_CurrentPosition <= _FinalDestination)
                    StopMotion();
            }

            AllEventsHandler.Instance.OnMotion(null, new Motion_EventArgs(_CurrentPosition));
        }

        void _MotionRepettiiveMeasurementTimer_Tick(object sender, EventArgs e)
        {
            _CurrentTime += _MotionSingleMeasurementTimer.Interval.Milliseconds;

            //Checking if measurement is completed
            if (_CurrentIteration >= _NumberRepetities)
                this.StopMotion();

            var positionPerTick = _MotionSingleMeasurementTimer.Interval.Milliseconds / 1000 * _metersPerSecond;

            if (_CurrentPosition >= _FinalDestination - positionPerTick)
            {
                this.SetDirection(MotionDirection.Down);
            }
            else if (_CurrentPosition <= _StartPosition + positionPerTick)
            {
                this.SetDirection(MotionDirection.Up);
            }

            _CurrentPosition += (_CurrentDirection == MotionDirection.Up ? 1 : -1) * positionPerTick;

            AllEventsHandler.Instance.OnMotion(null, new Motion_EventArgs(_CurrentTime / 1000));
        }

        public void Dispose()
        {
            if (_MotionSerialPort != null)
            {
                if (_MotionSerialPort.IsOpen == true)
                {
                    _MotionSerialPort.WriteLine("DI");
                    Thread.Sleep(100);

                    _MotionSerialPort.Close();
                }
            }
        }
    }
}
