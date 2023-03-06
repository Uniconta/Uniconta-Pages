using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection.Model
{
    class APIErrorsListModel
    {
        [JsonProperty("code")]
        public string Code { get; set; }
       [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
