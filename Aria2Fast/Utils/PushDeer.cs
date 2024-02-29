using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Aria2Fast.Service;

namespace Aria2Fast.Utils
{
    internal class PushDeer
    {

        public static async Task SendPushDeer(string msg = "")
        {

            if (string.IsNullOrEmpty(msg) || string.IsNullOrEmpty(AppConfig.Instance.ConfigData.PushDeerKey))
            {
                return;
            }

            Debug.WriteLine($"SendPushDeer {msg}");


            HttpClient client = new HttpClient();

            try
            {
                var url = "https://api2.pushdeer.com/message/push";

                var postData = new FormUrlEncodedContent(new Dictionary<string, string>()
                    {
                        {"pushkey" , AppConfig.Instance.ConfigData.PushDeerKey },
                        {"text" , title },
                        {"desp" , msg },
                    });

                var ret = await client.PostAsync(url, postData);

                ret.EnsureSuccessStatusCode();
                Console.WriteLine(await ret.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
    }
}
