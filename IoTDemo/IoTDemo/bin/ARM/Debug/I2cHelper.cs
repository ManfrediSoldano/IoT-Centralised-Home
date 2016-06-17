using System;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.UI.Xaml;

namespace IoTDemo
{

    public class I2cHelper
    {
        private static string AQS;
        private static DeviceInformationCollection DIS;
        public byte[] received_data = new byte[2];
        private bool active = false;
        private byte[] buffer = new byte[100];
        private DispatcherTimer timer;
        byte last = new byte();
       
        public I2cHelper (){
            timer = new Windows.UI.Xaml.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        public static async System.Threading.Tasks.Task<byte[]> WriteRead_OneByte(byte ByteToBeSend)
        {
            byte[] ReceivedData = new byte[2];

            /* Arduino Nano's I2C SLAVE address */
            int SlaveAddress = 64;              // 0x40

            try
            {
                // Initialize I2C
                var Settings = new I2cConnectionSettings(SlaveAddress);
                Settings.BusSpeed = I2cBusSpeed.StandardMode;

                if (AQS == null || DIS == null)
                {
                    AQS = I2cDevice.GetDeviceSelector("I2C1");
                    DIS = await DeviceInformation.FindAllAsync(AQS);
                }


                using (I2cDevice Device = await I2cDevice.FromIdAsync(DIS[0].Id, Settings))
                {
                    /* Send byte to Arduino Nano */
                    Device.Write(new byte[] { ByteToBeSend });

                    /* Read byte from Arduino Nano */
                    Device.Read(ReceivedData);
                }
            }
            catch (Exception)
            {
                
            }

            /* Return received data or ZERO on error */
            return ReceivedData;
        }

        public async System.Threading.Tasks.Task<byte[]> Send(byte DataToBeSend)
        {
            active = true;
            byte[] ReceivedData;
            ReceivedData = await I2cHelper.WriteRead_OneByte(DataToBeSend);
            received_data = ReceivedData;
            active = false;
            return ReceivedData;
          
        }


        public async void SendWaiting(byte DataToBeSend)
        {
            if (!active)
            {
               await Send(DataToBeSend);
               last = 100;
            }

            if (active)
            {
                last = DataToBeSend;
            }

        }

        private void Timer_Tick(object sender, object e)
        {
           if (last!= 100)
            {
                SendWaiting(last);
                
            }
        }
    } 
}

