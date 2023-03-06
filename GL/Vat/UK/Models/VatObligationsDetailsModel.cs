using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.Models
{
    class VatObligationsDetailsModel
    {
        [JsonProperty("status")]
        string status;
        public string DisplayProperty { get { return Start.ToString("dd MMM y") + " to " + End.ToString("dd MMM y") + ", " + Status; } }
       
        [JsonProperty("periodKey")]
        public string PeriodKey { get; set; }

        [JsonProperty("start")]
        public DateTime Start { get; set; }

        [JsonProperty("end")]
        public DateTime End { get; set; }

        [JsonProperty("due")]
        public DateTime Due { get; set; }
        public string Status
        {
            get
            {
                if (status == "O")
                    return "Open";
                if (status == "F")
                    return "Fulfilled";
                else
                    return "Unknown";
            }
            set
            {
                status = value;
            }
        }

        [JsonProperty("received")]
        public DateTime Received { get; set; }
    }
}
