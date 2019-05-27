using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Exception thrown during pre-validation of general settings, before generating the XML file.
    /// </summary>
    public class AltinnPrecheckError
    {
        private readonly string description;

        public AltinnPrecheckError(string description)
        {
            this.description = description;
        }

        public override string ToString()
        {
            return string.Format("{0}", description);
        }

        public string Description
        {
            get
            {
                return description;
            }
        }
    }
}
