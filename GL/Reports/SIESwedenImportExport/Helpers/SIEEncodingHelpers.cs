using System.Text;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.Helpers
{
    internal static class SIEEncodingHelpers
    {
        public const int PC8CodePage = 437;

        private static Encoding _pc8;

        /// <summary>
        /// The encoding prescribed by the SIE standard: IBM PC 8-bit extended ASCII (codepage 437).
        /// </summary>
        public static Encoding GetDefault()
        {
            if (_pc8 != null)
                return _pc8;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            try
            {
                _pc8 = Encoding.GetEncoding(PC8CodePage);
            }
            catch
            {
                _pc8 = Encoding.GetEncoding(28591); //ISO-8859-1 (single byte fallback)
            }

            return _pc8;
        }

        /// <summary>
        /// The encoding used for the control total. The standard requires codepage 437.
        /// </summary>
        public static Encoding GetChecksumEncoding()
        {
            return GetDefault();
        }
    }
}
