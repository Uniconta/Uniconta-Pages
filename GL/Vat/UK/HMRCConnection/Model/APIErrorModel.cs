using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection.Model
{
    class APIErrorModel
    {
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
       [JsonProperty("reactivationTimestamp")]
        public string ReactivationTimestamp { get; set; }
        [JsonProperty("errors")]
        public APIErrorsListModel[] Errors { get; set; }

        /// <summary>
        /// Converts object into human readable list of errors.
        /// </summary>
        /// <returns>Formatted string.</returns>
        public string GetErrorsAsString()
        {
            StringBuilder sb = new StringBuilder();
            if (Errors != null)
            {
                sb.AppendLine("Details:");
                foreach (var error in Errors)
                {
                    sb.AppendLine("Code: " + error?.Code);
                    sb.AppendLine("Message: " + error?.Message);
                    sb.AppendLine("Path: " + error?.Path);
                    sb.AppendLine();
                }
            }

            sb.AppendLine("An error has occured during communication with HMRC.");
            sb.AppendLine("Error code: " + Code);
            sb.AppendLine("Error message: " + Message);
            sb.AppendLine(ReactivationTimestamp ?? "");

            return sb.ToString();
        }
    }
}
