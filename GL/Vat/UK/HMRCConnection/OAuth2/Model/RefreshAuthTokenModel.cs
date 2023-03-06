using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection.OAuth2.Model
{
    class RefreshAuthTokenModel
    {
       // [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }
       // [JsonProperty("client_id")]
        public string ClientId { get; set; }
        //[JsonProperty("grant_type")]
        public string GrantType { get; set; }
       // [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
