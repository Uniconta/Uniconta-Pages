using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection
{
    internal static class Common
    {
        internal static string TokenAddress { get; } = "oauth/token";
        internal static string AuthAddress { get; } = "oauth/authorize";
        internal static string RedirectUri { get; } = "urn:ietf:wg:oauth:2.0:oob";
        /// <summary>
        ///used to request access and refresh token.
        /// </summary>
        internal static string AuthCode { get; set; }
        /// <summary>
        /// Refreshes access token if necessary.
        /// </summary>
        internal static string RefreshToken { get; set; }
        /// <summary>
        /// Grants access to HMRC resources. Expires after <see cref="ExpiresIn"/> seconds.
        /// </summary>        
        internal static string AccessToken { get; set; }
        /// <summary>
        /// Time in seconds until access token has to be refreshed.
        /// </summary>
        internal static long ExpiresIn { get; set; }
        internal static DateTime ExpireDate { get; set; }

        internal static string ClientId { get; set; } = "1TWU14RfvFTasUpZ_rLZ3gXOBdca";
        internal static string ClientSecret { get; set; } = "7a2cd451-dc3c-4661-a9e6-885d8560c12c";


    }
}
