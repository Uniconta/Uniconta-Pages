using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnicontaISO20022CreditTransfer
{
    /// <summary>
    /// Exception thrown during validation of an ISO20022 Credit Transfer document if an error is found.
    /// </summary>
    public class CheckError
    {
        private readonly string description;

        public CheckError(string description)
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

