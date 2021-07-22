using System;
using System.Linq;
using System.Threading;
using LibUsbDotNet.Main;
using UnityEngine;
using MonoLibUsb;
using MonoLibUsb.Descriptors;
using MonoLibUsb.Profile;

namespace Unity.GamecubeControllerSupport.Runtime
{
    public static class GamecubeControllerAdapter
    {
        public const short VendorID = 0x057e;
        public const short ProductID = 0x0337;

        public static bool IsReading { get; private set; }

        private static GamecubeController[] ports;

        private static MonoUsbSessionHandle _sessionHandle;
        private static MonoUsbDeviceHandle _deviceHandle = null;
        private static byte endpoint_in;
        private static byte endpoint_out;

        /// <summary>
        /// Starts Polling the Adapter.
        /// partly derrived from Example code:
        /// http://libusbdotnet.sourceforge.net/V2/html/476f208c-6b00-48ea-b829-29988d214678.htm
        /// </summary>
        public static void Start()
        {
            ports = new GamecubeController[4];
            for (int i = 0; i < 4; i++)
                ports[i] = new GamecubeController(i + 1);

            _sessionHandle = new MonoUsbSessionHandle();
            if (_sessionHandle.IsInvalid)
                throw new Exception(
                    $"Failed intializing libusb context.\n{MonoUsbSessionHandle.LastErrorCode}:{MonoUsbSessionHandle.LastErrorString}");

            try
            {
                MonoUsbProfileList profileList = new MonoUsbProfileList();
                profileList.Refresh(_sessionHandle);
                MonoUsbProfile myProfile = profileList.GetList().Find(IsWiiUAdapter);

                if (myProfile == null) return;

                _deviceHandle = myProfile.OpenDeviceHandle();
                if (_deviceHandle.IsInvalid)
                    throw new Exception(
                        $"Failed opening device handle.\n{MonoUsbDeviceHandle.LastErrorCode}:{MonoUsbDeviceHandle.LastErrorString}");

                MonoUsbApi.GetConfigDescriptor(myProfile.ProfileHandle, 0, out MonoUsbConfigHandle configHandle);
                MonoUsbConfigDescriptor configDescriptor = new MonoUsbConfigDescriptor(configHandle);
                MonoUsbApi.ClaimInterface(_deviceHandle, 0);

                foreach (MonoUsbEndpointDescriptor usbEndpoint in configDescriptor.InterfaceList
                        .SelectMany(usbInterface => usbInterface.AltInterfaceList
                        .SelectMany(usbAltInterface => usbAltInterface.EndpointList)))
                {
                    if (usbEndpoint.bEndpointAddress.CompareTo((byte) UsbEndpointDirection.EndpointIn) > 0)
                        endpoint_in = usbEndpoint.bEndpointAddress;
                    else
                        endpoint_out = usbEndpoint.bEndpointAddress;
                }

                byte[] payload = {0x13};
                MonoUsbApi.InterruptTransfer(_deviceHandle, endpoint_out, payload, payload.Length, out int _, 1000);

                Thread pollThread = new Thread(Poll);
                pollThread.Start();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Stops polling
        /// </summary>
        public static void Stop()
        {
            IsReading = false;
            MonoUsbApi.ReleaseInterface(_deviceHandle, 0);
            _deviceHandle.Close();
        }

        /// <summary>
        /// Polls data from the adapter.
        /// </summary>
        private static void Poll()
        {
            IsReading = true;
            byte[] data = new byte[37];
            int ret = 0;
            while (ret == 0 && IsReading)
            {
                //data gets modified by this call even though theres no out or ref keyword used.
                ret = MonoUsbApi.InterruptTransfer(_deviceHandle, endpoint_in, data, data.Length, out int _, 150);
                ProcessData(data);
            }

            Thread.CurrentThread.Abort();
            Stop();
        }

        /// <summary>
        /// Assigns the new data to each controller
        /// </summary>
        /// <param name="data"> Full raw data of the adapter.</param>
        private static void ProcessData(byte[] data)
        {
            byte[] port1 = new byte [8];
            byte[] port2 = new byte [8];
            byte[] port3 = new byte [8];
            byte[] port4 = new byte [8];

            Array.Copy(data, 2, port1, 0, 8);
            Array.Copy(data, 11, port2, 0, 8);
            Array.Copy(data, 20, port3, 0, 8);
            Array.Copy(data, 29, port4, 0, 8);

            ports[0].Data = port1;
            ports[1].Data = port2;
            ports[2].Data = port3;
            ports[3].Data = port4;
        }

        private static bool IsWiiUAdapter(MonoUsbProfile profile) =>
            profile.DeviceDescriptor.VendorID == 0x057e && profile.DeviceDescriptor.ProductID == 0x0337;

        /// <summary>
        /// Returns the GamecubeController object of the indicated port.
        /// </summary>
        /// <param name="portIndex">index of the port (0 => first port)</param>
        /// <returns> GamecubeController object</returns>
        public static GamecubeController GetController(int portIndex)
        {
            if (portIndex < 0 || portIndex > 3) throw new Exception("Port index out of bounds.");
            return ports[portIndex];
        }
    }

}