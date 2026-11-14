using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.Helpers
{
    /// <summary>
    /// The SRU code (#SRU) is kept in a user defined text field named "SRU" on the general ledger account.
    /// The field is read through the company's field definitions, so it works on any account row and does
    /// not need a compiled user type. Companies without the field simply have no SRU codes.
    /// </summary>
    internal class SIEAccountFieldsHelpers
    {
        public const string SRUField = "SRU";

        /// <summary>
        /// Requires the company of the account to be set (SetMaster)
        /// </summary>
        public static bool HasSRUField(GLAccountClient account)
        {
            return account.GetUserFieldIndex(SRUField) >= 0;
        }

        public static string GetSRU(GLAccountClient account)
        {
            var idx = account.GetUserFieldIndex(SRUField);
            return idx >= 0 ? account.GetUserFieldString(idx) : null;
        }

        /// <returns>false when the company has no SRU field</returns>
        public static bool SetSRU(GLAccountClient account, string sru)
        {
            var idx = account.GetUserFieldIndex(SRUField);
            if (idx < 0)
                return false;

            account.SetUserFieldString(idx, sru);
            return true;
        }
    }
}
