using System;
using System.Collections.Generic;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    /// <summary>
    /// #KONTO
    /// </summary>
    [Serializable]
    public class SIEAccount
    {
        public SIEAccount()
        {
            SRU = new HashSet<string>();

            Name = "";
        }

        public string Number { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }

        /// <summary>
        /// #KTYP: T (asset), S (debt), K (cost) or I (income)
        /// </summary>
        public string Type { get; set; }
        public HashSet<string> SRU { get; set; }

        /// <summary>
        /// True when the account was declared with #KONTO, false when it was only referenced by a balance or transaction item.
        /// </summary>
        public bool IsDeclared { get; set; } = true;
    }
}
