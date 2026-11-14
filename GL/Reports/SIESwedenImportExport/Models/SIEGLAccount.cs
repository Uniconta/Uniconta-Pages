using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.Models
{
    /// <summary>
    /// Account of a SIE file as shown on the import page. New accounts are inserted as they are;
    /// the SRU code is copied to the company's SRU field before inserting.
    /// </summary>
    public class SIEGLAccount : GLAccountClient
    {
        [Display(Name = nameof(SRU), ResourceType = typeof(SIEGLAccountText))]
        public string SRU { get; set; }

        [Display(Name = nameof(IsNew), ResourceType = typeof(SIEGLAccountText))]
        public bool IsNew { get; set; }

        /// <summary>
        /// Name of the account in Uniconta when it already exists and the name in the SIE file differs.
        /// The standard asks the importing program to make the user aware of such differences.
        /// </summary>
        [Display(Name = nameof(ExistingName), ResourceType = typeof(SIEGLAccountText))]
        public string ExistingName { get; set; }
    }

    public static class SIEGLAccountText
    {
        public static string SRU { get { return Localization.lookup("SIESRU"); } }
        public static string IsNew { get { return Localization.lookup("New"); } }
        public static string ExistingName { get { return Uniconta.Common.Utility.Util.ConcatParenthesis(Localization.lookup("Name"), "Uniconta"); } }
    }
}
