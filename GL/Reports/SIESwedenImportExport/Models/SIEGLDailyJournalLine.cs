using System;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.Models
{
    /// <summary>
    /// Journal line created from a SIE #TRANS row. Keeps the series and number of the voucher for the
    /// series-to-journal mapping and the account name of the file for accounts not yet in Uniconta.
    /// </summary>
    public class SIEGLDailyJournalLine : GLDailyJournalLineClient
    {
        /// <summary>
        /// Uniconta account name, or the name in the SIE file when the account does not exist yet
        /// </summary>
        [Display(Name = nameof(DisplayAccountName), ResourceType = typeof(SIEJournalLineText))]
        public string DisplayAccountName
        {
            get
            {
                var accountName = AccountName;
                if (String.IsNullOrEmpty(accountName))
                    accountName = SIEAccountName;

                return accountName;
            }
        }

        public string SIEAccountName { get; set; }
        public string Number { get; set; }
        public string Series { get; set; }
    }

    public static class SIEJournalLineText
    {
        public static string DisplayAccountName { get { return Localization.lookup("AccountName"); } }
    }
}
