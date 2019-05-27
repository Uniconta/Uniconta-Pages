using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AltinnResponse
    {
        private readonly string responseText;
        private readonly bool error;

        public AltinnResponse(string responseText, bool error = false)
        {
            this.responseText = responseText;
            this.error = error;
        }

        public override string ToString()
        {
            return string.Format("{0}\n{1}\n", responseText, error);
        }

        public bool Error
        {
            get
            {
                return error;
            }
        }

        public string ResponseText
        {
            get
            {
                return responseText;
            }
        }

    }
}
