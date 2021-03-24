using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Web;

namespace MythServer
{
    public static class AuthManager
    {

        public delegate void LoginCompleteCallback(FacebookAuthResponse fbResponse);

        public static async void FacebookAuth(string deviceID, LoginCompleteCallback callback)
        {

            FacebookAuthResponse fbResponse = new FacebookAuthResponse();

            try
            {

                /* //Facebook Authentication

                var request = GetHttpRequest(HttpMethod.Get, accessToken);

                using (var client = new HttpClient())
                {
                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();

                        var facebookUser = JsonConvert.DeserializeObject<FacebookUser>(responseString);

                        fbResponse.success = true;
                        fbResponse.name = facebookUser.Name;
                        fbResponse.id = facebookUser.ID;

                    }
                    else
                    {

                        fbResponse.success = false;

                    }
                }

                */

                fbResponse.success = true;
                fbResponse.name = "Mario";
                fbResponse.id = deviceID;

            }
            catch (Exception ex)
            {

                Console.WriteLine("Error authenticating facebook user. " + ex);

            }

            callback(fbResponse);

        }

        public static HttpRequestMessage GetHttpRequest(HttpMethod method, string accessToken)
        {
            var baseUrl = $"{FacebookAPIConfig.Url}/{FacebookAPIConfig.Version}/me?fields=id,name&access_token={accessToken}";

            var request = new HttpRequestMessage(method, baseUrl);
            request.Headers.Add("Accept", "application/json");

            return request;

        }

    }

    public class FacebookAuthResponse
    {

        public bool success;
        public string id, name;

    }

    public static class FacebookAPIConfig
    {
        public static string Url = "https://graph.facebook.com";

        public static string Version = "v9.0";

    }
    public class FacebookUser
    {
        public string ID { get; set; }

        public string Name { get; set; }

    }
}
