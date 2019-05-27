using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnicontaISO20022CreditTransfer
{
    /// <summary>
    /// Exception thrown during pre-validation of general settings, before generating the payment file.
    /// </summary>
    public class PreCheckError
    {
        private readonly string description;

        public PreCheckError(string description)
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

