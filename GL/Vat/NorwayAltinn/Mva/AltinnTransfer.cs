using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows.Controls;
using System.Xml;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    class AltinnTransfer
    {
        #region Member Constants
        protected const string ALTINNURL_TEST = "https://tt02.altinn.no";
        protected const string ALTINNURL_LIVE = "https://www.altinn.no";
        #endregion

        #region Properties
        private bool AltinnTestEnvironment { get; set; }
        private string SystemUserName { get; set; }
        private string UserSSN { get; set; }
        private string UserPassword { get; set; }
        private string AuthMethod { get; set; }
        private string MvaXml { get; set; }

        private string AltinnUrl { get; set; }
        #endregion


        #region Contructors
        public AltinnTransfer(string mvaXml, bool altinnTest = false)
        {
            MvaXml = mvaXml;
            AltinnTestEnvironment = altinnTest;
        }

        public AltinnTransfer(string systemUserName, string userSSN, string userPassword, string authMethod, bool altinnTest = false)
        {
            SystemUserName = systemUserName;
            UserSSN = userSSN;
            UserPassword = userPassword;
            AuthMethod = authMethod;
            AltinnTestEnvironment = altinnTest;
        }
        #endregion

        public string MVAMelding()
        {
            AltinnUrl = AltinnTestEnvironment ? ALTINNURL_TEST : ALTINNURL_LIVE;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                var adr = string.Format(@"{0}/IntermediaryExternal/IntermediaryInboundBasic.svc", AltinnUrl);
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("POST"), adr);
                request.Headers.Add("SOAPAction", "http://www.altinn.no/services/Intermediary/Shipment/IntermediaryInbound/2009/10/IIntermediaryInboundExternalBasic/SubmitFormTaskBasic");
                request.Headers.Add("accept", "text/xml");
                request.Content = new StringContent(MvaXml, Encoding.UTF8, "text/xml");

                HttpResponseMessage response = client.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        public string SendAuthenticationRequest()
        {
            AltinnUrl = AltinnTestEnvironment ? ALTINNURL_TEST : ALTINNURL_LIVE;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                var adr = string.Format(@"{0}/AuthenticationExternal/SystemAuthentication.svc", AltinnUrl);
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("POST"), adr);
                request.Headers.Add("SOAPAction", "http://www.altinn.no/services/Authentication/SystemAuthentication/2009/10/ISystemAuthenticationExternal/GetAuthenticationChallenge");
                request.Headers.Add("accept", "text/xml");
                var data = GetAuthentication();
                request.Content = new StringContent(data, Encoding.UTF8, "text/xml");

                HttpResponseMessage response = client.SendAsync(request).Result;
                string t = response.Content.ReadAsStringAsync().Result;

                return t;
            }
        }

        private string GetAuthentication()
        {
            string systemUserName = SystemUserName;
            string userSSN = UserSSN;
            string authMethod = AuthMethod;
            string userPassword = UserPassword;

            string str = "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ns";
            str = str + "=\"http://www.altinn.no/services/Authentication/SystemAuthentication/2009/10\" xmlns:ns1";
            str = str + "=\"http://schemas.altinn.no/services/Authentication/2009/10\">";
            str = str + "<soapenv:Header/><soapenv:Body><ns:GetAuthenticationChallenge><ns:challengeRequest>";
            str = str + "<ns1:AuthMethod>" + authMethod + "</ns1:AuthMethod><ns1:SystemUserName>" + systemUserName + "</ns1:SystemUserName>";
            str = str + "<ns1:UserPassword>" + userPassword + "</ns1:UserPassword><ns1:UserSSN>" + userSSN + "</ns1:UserSSN></ns:challengeRequest>";
            str = str + "</ns:GetAuthenticationChallenge></soapenv:Body></soapenv:Envelope>";
            return str;
        }
    }
}
