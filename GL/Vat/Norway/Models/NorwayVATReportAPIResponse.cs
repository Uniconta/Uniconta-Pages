using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.Norway.Models
{
    public class NorwayVATReportAPIResponse
    {
        public bool IsSuccess { get; private set; }
        public string Error { get; private set; }

        /// <summary>HTTP status of the failed call, when the failure came from an HTTP response</summary>
        public System.Net.HttpStatusCode? StatusCode { get; set; }

        public NorwayVATReportAPIResponse(bool isSuccess, string error = "")
        {
            IsSuccess = isSuccess;
            Error = error;
        }
    }

    public class NorwayVATAPIResponse<T> : NorwayVATReportAPIResponse
    {
        public T Result { get; set; }

        public NorwayVATAPIResponse(T result) : base(true)
        {
            Result = result;
        }

        public NorwayVATAPIResponse(string error) : base(false, error)
        {

        }
    }
}
