using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using UnicontaClient.Pages.GL.Vat.UK.HMRCConnection.Model;
using UnicontaClient.Pages.GL.Vat.UK.HMRCConnection.OAuth2;
using Uniconta.ClientTools.Controls;
using Newtonsoft.Json;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection
{
    public class HMRCConnection
    {
        bool _isTest;
        HttpClient _client = new HttpClient();
        int _vrn;

        /// <summary>
        /// Creates new instance of <see cref="HMRCConnection"/> class.
        /// </summary>
        /// <param name="vrn">Vat registration number of company.</param>
        /// <param name="isTest">Determines whether this operation will send to HMRC's test endpoints. Defaults true.</param>
        public HMRCConnection(string vrn, bool isTest = true)
        {
            _isTest = isTest;
            //vat return number
            _vrn = CleanVRN(vrn);
            SetupClient();
        }

        /// <summary>
        /// Authenticates with HMRC via OAuth 2.0, then sends Vat return POST message.
        /// </summary>
        /// <param name="json"></param>
        /// <returns>See <see cref="Task"/></returns>
        public async Task Post(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("No items found to send to HMRC.");

            await ProcessAuth();
            await PostVatReturn(json);
        }
        /// <summary>
        /// Retrieves VAT obligations from HMRC, using OAuth 2.0.
        /// </summary>
        /// <param name="from">Start date.</param>
        /// <param name="to">End date.</param>
        /// <returns>JSON string with results if successful.</returns>
        public async Task<string> Get(string from, string to)
        {
            await ProcessAuth();
            return await GetObligations(from, to);
        }

        async Task ProcessAuth()
        {
            //if plugin was loaded before, no need to reauth unless access token needs refreshing, since the access token already exists
            if (string.IsNullOrWhiteSpace(Common.AccessToken))
            {
                OAuth2Authentication auth = new OAuth2Authentication(_client);
                Common.AuthCode = await auth.UserAuthorisation("write:vat read:vat");
                string tokenJson = await auth.GetAuthTokens();
                if (tokenJson == null)
                    return;
                TokenResponseModel tokenJsonDeserialised = JsonConvert.DeserializeObject<TokenResponseModel>(tokenJson);

                Common.AccessToken = tokenJsonDeserialised.access_token;
                Common.RefreshToken = tokenJsonDeserialised.refresh_token;
                Common.ExpiresIn = tokenJsonDeserialised.expires_in;
                Common.ExpireDate = DateTime.Now.AddSeconds(tokenJsonDeserialised.expires_in);
            }
        }

        async Task<string> GetObligations(string from, string to)
        {
            if (string.IsNullOrEmpty(Common.AccessToken))
                return null;
            //TODO: Add refresh token functionality
            //create a string for a GET HTTP query
            var query = HttpUtility.ParseQueryString(string.Empty);
            //example format = "?from=2017-01-01&to=2017-12-31
            query["from"] = from;
            query["to"] = to;
            string uriString = "organisations/vat/" + _vrn + "/obligations" + "?" + query.ToString();
            //add access token to header of http message
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Common.AccessToken);
            SetFraudPreventionHeaders();
            //get
            HttpResponseMessage response = await _client.GetAsync(uriString);

            if (!response.IsSuccessStatusCode)
                throw new WebException(await ProcessAPIError(response));

            UnicontaMessageBox.Show("Retrieved VAT obligations from HMRC", "Success");
            return await response.Content.ReadAsStringAsync();
        }

        async Task PostVatReturn(string json)
        {
            //TODO: Add refresh token functionality
            //3rd param for this is the Content-Type header for the message, refer to your API's doc for correct type.

            StringContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            //set auth header to be the OAuth 2.0 Access Token
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Common.AccessToken);
            SetFraudPreventionHeaders();
            HttpResponseMessage response = await _client.PostAsync("organisations/vat/" + _vrn + "/returns", content);

            if (!response.IsSuccessStatusCode)
                throw new WebException(await ProcessAPIError(response));
            else
            {
                //if success, change content of response from json string to data storage class
                var deserialised = JsonConvert.DeserializeObject<VatSuccessResponseModel>(await response.Content.ReadAsStringAsync());
                string successString = "Processing Date: " + deserialised.processingDate + Environment.NewLine
                    + "Form Bundle Number: " + deserialised.formBundleNUmber;
                if (string.IsNullOrWhiteSpace(deserialised.paymentIndicator))
                    successString += "Payment Indicator: " + deserialised.paymentIndicator + Environment.NewLine;
                if (string.IsNullOrWhiteSpace(deserialised.chargeRefNumber))
                    successString += "Charge Ref Number: " + deserialised.chargeRefNumber;
                _client.Dispose();
                UnicontaMessageBox.Show("VAT return sent to HMRC successfully" + successString , "Information");
            }

        }

        async Task<string> ProcessAPIError(HttpResponseMessage response)
        {
            APIErrorModel deserialised;
            try
            {
                deserialised = JsonConvert.DeserializeObject<APIErrorModel>(await response.Content.ReadAsStringAsync());
            }
            catch (JsonException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            return deserialised?.GetErrorsAsString();
        }

        void SetupClient()
        {
            //important that the accept header is set, each restapi might have a different required value
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));

            if (_isTest)
                _client.BaseAddress = new Uri("https://test-api.service.hmrc.gov.uk/");
            else
                _client.BaseAddress = new Uri("https://api.service.hmrc.gov.uk/");

        }

        int CleanVRN(string vrn)
        {
            //regex removes everything but number characters
            if (int.TryParse(Regex.Replace(vrn, @"[\D]", ""), out int output))
                return output;
            else
                throw new ArgumentException("Could not parse the VAT Registration Number provided.");
        }

        //unused at the moment
        /// <summary>
        /// checks that the vat return number provided is 9 digit number.
        /// </summary>
        /// <param name="vrn">vat return number for testing.</param>
        /// <returns>boolean value. True if number is 9 digit, false if not.</returns>
        bool CheckVRN(int vrn)
        {
            if (vrn > 999999999 || vrn < 100000000)
                return false;

            return true;
        }

        void SetFraudPreventionHeaders()
        {
            _client.DefaultRequestHeaders.Add("Gov-Client-Connection-Method", "DESKTOP_APP_DIRECT");
            _client.DefaultRequestHeaders.Add("Gov-Client-Device-ID", FraudPreventionInfo.ClientDeviceId);
            _client.DefaultRequestHeaders.Add("Gov-Client-User-IDs", FraudPreventionInfo.ClientUserIds);
            _client.DefaultRequestHeaders.Add("Gov-Client-Timezone", FraudPreventionInfo.ClientTimezone);
            _client.DefaultRequestHeaders.Add("Gov-Client-Local-IPs", FraudPreventionInfo.ClientLocalIps);
            _client.DefaultRequestHeaders.Add("Gov-Client-Screens", FraudPreventionInfo.ClientScreens);
            //_client.DefaultRequestHeaders.Add("Gov-Client-Window-Size", FraudPreventionInfo.WindowSize);
            _client.DefaultRequestHeaders.Add("Gov-Client-User-Agent", FraudPreventionInfo.UserAgent);
            _client.DefaultRequestHeaders.Add("Gov-Client-Vendor-Version", FraudPreventionInfo.VendorVersion);
            //_client.DefaultRequestHeaders.Add("Gov-Client-Window-Size", FraudPreventionInfo.WindowSize);
            _client.DefaultRequestHeaders.Add("Gov-Client-License-IDs", FraudPreventionInfo.LicenseId);
            _client.DefaultRequestHeaders.Add("Gov-Client-Mac-Addresses", FraudPreventionInfo.MacAddresses);
        }
    }

    class CreateTestOrg
    {
        private HttpClient _client = new HttpClient();
        private string _serverToken = "d18434e6413ba7cbbebe3baa34e839";
        private void Setup()
        {
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.hmrc.1.0+json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serverToken);
            _client.BaseAddress = new Uri("https://test-api.service.hmrc.gov.uk/");
        }
        internal async Task<TestOrganisationModel> CreateNewOrg()
        {
            Setup();
            string serviceNames = "{\"serviceNames\": [\"corporation-tax\",\"paye-for-employers\",\"submit-vat-returns\",\"mtd-vat\"]}";
            //string json = JsonConvert.SerializeObject(input);
            var content = new StringContent(serviceNames, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PostAsync("/create-test-user/organisations", content);
            if (!response.IsSuccessStatusCode)
                throw new WebException(await response.Content.ReadAsStringAsync());

            var message = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TestOrganisationModel>(message);
        }
    }
}
