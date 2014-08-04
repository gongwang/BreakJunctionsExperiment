﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hardware
{
    //Represents avaliable connections to the device

    enum DeviceConnections { GPIB_Connection }

    /*     Interface to implement basic work with device     */

    public interface IExperimentalDevice
    {
        //Initializing the experimental device

        bool InitDevice();

        //Sends command request to the device

        bool SendCommandRequest(string RequestString);

        //Recieves the device ansver

        string ReceiveDeviceAnswer();
    }
}
