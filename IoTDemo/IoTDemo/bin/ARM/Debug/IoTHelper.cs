using System;

using System.Text;

using Microsoft.Azure.Devices.Client;

namespace IoTDemo
{
    class IoTHelper
    {
         static string iotHubUri = "TestManfre.azure-devices.net"; 
         static string deviceId = "MyDevice"; 
         static string deviceKey = "rNfdTGTTzeWAv0OA34C7uRxdbt9sAU9f/VChMpiog8A="; 

        public Message message= new Message();

        public IoTHelper()
        {
            
        }

        public async void SendDeviceToCloudMessagesAsync(double temp)
        {
            

                        var deviceClient = DeviceClient.Create(iotHubUri,
                    AuthenticationMethodFactory.
                        CreateAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey),
                    TransportType.Http1);

            var str = temp.ToString();
            var message = new Message(Encoding.ASCII.GetBytes(str));

            await deviceClient.SendEventAsync(message);
        }

        



    }
}
