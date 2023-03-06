using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.Models
{
    class VatObligationsModel
    {
        [JsonProperty("obligations")]
        public VatObligationsDetailsModel[] Obligations { get; set; }
    }
}
