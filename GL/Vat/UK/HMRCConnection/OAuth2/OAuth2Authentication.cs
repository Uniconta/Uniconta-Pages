using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
//using UnicontaClient.HMRCConnection.OAuth2.Views;
using UnicontaClient.Pages.GL.Vat.UK.HMRCConnection.Model;
using UnicontaClient.Pages.GL.Vat.UK.HMRCConnection.OAuth2.Model;
using UnicontaClient.Pages.GL.Vat.UK.HMRCConnection.OAuth2.View;
using static DevExpress.Mvvm.Native.TaskLinq;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection.OAuth2
{
    class OAuth2Authentication
    {
        HttpClient _client;
        public OAuth2Authentication(HttpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Launches browser to get the authcode required for next step of OAuth 2.0.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns>returns Auth Code for token retrieval.</returns>
        internal async Task<string> UserAuthorisation(string scope)
        {
            //opens browser to HMRC page where user will authenticate themselves
            Uri authSiteUri = new Uri(_client.BaseAddress + Common.AuthAddress + "?response_type=code" + "&client_id=" + Common.ClientId + "&scope=" + scope + "&redirect_uri=" + Common.RedirectUri);
            Process browser = Process.Start(authSiteUri.AbsoluteUri);
            //opens form for pasting in OAuth2.0 Authorisation Code 
            var authCode = string.Empty;
            AuthCodeView cwForm = new AuthCodeView("Green");
            cwForm.Closing +=  delegate
            {
                if (cwForm.DialogResult == true)
                {
                    if (cwForm.DialogResult == true)
                        authCode= cwForm.AuthCode;
                    else
                        throw new TaskCanceledException();
                }
            };
            cwForm.ShowDialog();

            return authCode;
        }

        async Task<HttpResponseMessage> PostAuth(string json)
        {
            if (json == null)
                throw new ArgumentNullException("json");
            StringContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await _client.PostAsync(Common.TokenAddress, content);
        }

        /// <summary>
        /// Retrieves Access and refresh tokens from HMRC.
        /// </summary>
        /// <returns>Response from token request in JSON.</returns>
        internal async Task<string> GetAuthTokens()
        {
            if (string.IsNullOrEmpty(Common.AuthCode))
                return null;
            var postParams = new GetAuthTokenModel()
            {
                client_secret = Common.ClientSecret,
                client_id = Common.ClientId,
                redirect_uri = "urn:ietf:wg:oauth:2.0:oob",
                code = Common.AuthCode
            };
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(postParams);
            StringContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PostAsync(Common.TokenAddress, content);
            if (!response.IsSuccessStatusCode)
                throw new WebException("Error while completing authentication process with HMRC. Error message: " + response.Content.ReadAsStringAsync().Result);

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Refreshes Access token from HMRC
        /// </summary>
        /// <returns> Response from token request in JSON.</returns>
        internal async Task<string> RefreshToken()
        {
            var postParams = new RefreshAuthTokenModel()
            {
                ClientSecret = Common.ClientSecret,
                ClientId = Common.ClientId,
                GrantType = "refresh_token",
                RefreshToken = Common.RefreshToken
            };
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(postParams);
            HttpResponseMessage response = await PostAuth(json);
            if (!response.IsSuccessStatusCode)
                throw new WebException("Error while refreshing authentication token with HMRC. Error message: " + response.Content);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
