using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection.OAuth2.Model
{
    class GetAuthTokenModel
    {
        public string client_secret { get; set; }
        public string client_id { get; set; }
        public string grant_type { get; } = "authorization_code";
        public string redirect_uri { get; set; }
        public string code { get; set; }
    }
}
