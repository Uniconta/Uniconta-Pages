using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.Controls;
using Newtonsoft.Json;
using static Straumur.CreditCard.Settlement.Straumur_CreditCardClient;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.DailyJournal
{
    public class Iceland_RESTCli : IDisposable
    {
        private HttpClient CreditCardClient { get; set; }
        private string EndpointUri;
        private string username { get; set; }
        private string password { get; set; }
        private string base64EncodedAuthenticationString = "";
        public Status statuscode;

        public Iceland_RESTCli(string baseurl, string _username, string _password)
        {
            CreditCardClient = new HttpClient();
            CreditCardClient.DefaultRequestHeaders.Clear();
            CreditCardClient.DefaultRequestHeaders.ConnectionClose = true;
            EndpointUri = baseurl;
            username = _username;
            password = _password;
            var authenticationString = $"{username}:{password}";
            base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
        }

        public async Task<string> api(string apicommand)
        {
            var res = "";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(EndpointUri + apicommand));
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            try
            {
                res = await CreditCardClient.SendAsync(requestMessage).Result.Content.ReadAsStringAsync();
                if (res == "")
                    throw new Exception("Trúlegast rangur aðgangur.");
                else
                    return res;
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.ToString(), "");
                return "";
            }
        }

        public T data<T>(string resultstring)
        {
            try
            {
                statuscode = null;
                if ((!string.IsNullOrEmpty(resultstring)) && resultstring[0] == '{')
                {
                    var jsondeserialized = JsonConvert.DeserializeObject<T>(resultstring);
                    if (jsondeserialized != null && !resultstring.Contains("StatusCode"))
                        return (T)Convert.ChangeType(jsondeserialized, typeof(T));
                    if (jsondeserialized != null && resultstring.Contains("StatusCode"))
                    {
                        statuscode = JsonConvert.DeserializeObject<Status>(resultstring);
                        return default(T);
                    }
                }
                else if (!string.IsNullOrEmpty(resultstring))
                {
                    statuscode = new Status() { StatusCode = 999, Message = resultstring };
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.ToString(), "");
            }
            return default(T);
        }

        public void Dispose()
        {
            CreditCardClient = null;
        }

        public class Status
        {
            public string Message { get; set; }
            public int StatusCode { get; set; }
        }
    }
}
