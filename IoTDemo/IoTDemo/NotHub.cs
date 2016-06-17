using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace IoTDemo
{
    class NotHub
    {


        string Endpoint = "";
        string SasKeyName = "";
        string SasKeyValue = "";

        public void ConnectionStringUtility(string connectionString)
        {
            //Parse Connectionstring
            char[] separator = { ';' };
            string[] parts = connectionString.Split(separator);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("Endpoint"))
                    Endpoint = "https" + parts[i].Substring(11);
                if (parts[i].StartsWith("SharedAccessKeyName"))
                    SasKeyName = parts[i].Substring(20);
                if (parts[i].StartsWith("SharedAccessKey"))
                    SasKeyValue = parts[i].Substring(16);
            }
        }


        public string getSaSToken(string uri, int minUntilExpire)
        {
            string targetUri = Uri.EscapeDataString(uri.ToLower()).ToLower();

            // Add an expiration in seconds to it.
            long expiresOnDate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            expiresOnDate += minUntilExpire * 60 * 1000;
            long expires_seconds = expiresOnDate / 1000;
            String toSign = targetUri + "\n" + expires_seconds;

            // Generate a HMAC-SHA256 hash or the uri and expiration using your secret key.
            MacAlgorithmProvider macAlgorithmProvider = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
            BinaryStringEncoding encoding = BinaryStringEncoding.Utf8;
            var messageBuffer = CryptographicBuffer.ConvertStringToBinary(toSign, encoding);
            IBuffer keyBuffer = CryptographicBuffer.ConvertStringToBinary(SasKeyValue, encoding);
            CryptographicKey hmacKey = macAlgorithmProvider.CreateKey(keyBuffer);
            IBuffer signedMessage = CryptographicEngine.Sign(hmacKey, messageBuffer);

            string signature = Uri.EscapeDataString(CryptographicBuffer.EncodeToBase64String(signedMessage));

            return "SharedAccessSignature sr=" + targetUri + "&sig=" + signature + "&se=" + expires_seconds + "&skn=" + SasKeyName;
        }


        public async void sendNotification()
        {
            //insert your HubFullAccess here (a string that can be copied from the Azure Portal by clicking Access Policies on the Settings blade for your notification hub)
            ConnectionStringUtility("Conncetion string");

            //replace YOURHUBNAME with whatever you named your notification hub in azure 
            var uri = Endpoint + "iotnothub" + "/messages/?api-version=2015-04";
            var toast = @"<toast launch=""app-defined-string"">     <visual>       <binding template = ""ToastGeneric""> <text> Door open </text> <text> Someone has just opened your door!</text> <image placement = ""AppLogoOverride"" src = ""ladro.png""/> </binding> </visual>  </toast>";


            //send an HTTP POST request


            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, uri);
                request.Content = new StringContent(toast);
                request.Headers.Add("Authorization", getSaSToken(uri, 1000));
                //request.Headers.Add("ServiceBusNotification-Tags", "TAG");
                request.Headers.Add("ServiceBusNotification-Format", "windows");
                request.Headers.Add("X-WNS-Type", "wns/toast");
                var response = await httpClient.SendAsync(request);
                await response.Content.ReadAsStringAsync();
            }

            

        }
    }
}
