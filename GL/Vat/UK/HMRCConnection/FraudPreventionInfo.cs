using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Uniconta.API.System;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection
{
    static class FraudPreventionInfo
    {
        public static string ClientDeviceId { get { return GetDeviceId(); } }
        public static string ClientUserIds { get { return GetUserId(); } }
        public static string ClientTimezone { get { return GetTimezone(); } }
        public static string ClientLocalIps { get { return GetLocalIps(); } }
        public static string ClientScreens { get { return GetClientScreens(); } }
        public static string WindowSize { get { return GetWindowSize(); } }
        public static string UserAgent { get { return GetUserAgent(); } }
        public static string VendorVersion { get { return GetVendorVersion(); } }
        public static string LicenseId { get { return GetLicenseId(); } }
        public static string MacAddresses { get { return GetMacAddresses(); } }

        static string GetDeviceId()
        {
            string moboString = GetWMIProperty("SELECT * FROM Win32_BaseBoard", "SerialNumber");
            string procId = GetWMIProperty("SELECT * FROM Win32_Processor", "ProcessorID");
            bool moboNull = string.IsNullOrWhiteSpace(moboString);
            bool procNull = string.IsNullOrWhiteSpace(procId);
            if (moboNull && procNull)
                return "";
            byte[] seedBytes = CreateGuidBytes(new string[2] { moboString, procId }).ToArray();
            if (seedBytes.Length != 16)
                return "";

            Guid output = new Guid(seedBytes);
            return output.ToString();
        }
        static string GetUserId()
        {
            string userName = Environment.UserName;
            string vendorAccount = BasePage.session.User._Name;
            StringBuilder sb = new StringBuilder();
            sb.Append("os=");
            sb.Append(userName);
            sb.Append("&uniconta-account=");
            sb.Append(vendorAccount);

            return sb.ToString();
        }

        static string GetTimezone()
        {
            TimeZone timeZone = TimeZone.CurrentTimeZone;
            DateTime currDate = DateTime.Now;
            TimeSpan utcOffset = timeZone.GetUtcOffset(currDate);
            string utcString = "";
            string symbol = "";
            if (utcOffset < TimeSpan.Zero)
                symbol = "-";
            else
                symbol = "+";

            utcString = "UTC" + symbol + utcOffset.ToString(@"mm\:ss");
            return utcString;
        }

        static string GetLocalIps()
        {
            List<string> addresses = new List<string>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (!ip.IsDnsEligible)
                            continue;
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            addresses.Add(ip.Address.ToString());

                        }
                    }
                }
            }
            return string.Join(",", addresses);
        }
        static string GetMacAddresses()
        {
            string[] macs = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(ni => ni.GetPhysicalAddress().ToString()).ToArray();
            return string.Join(",", macs);
        }

        static string GetClientScreens()
        {
            List<string> output = new List<string>();
            var screens = Screen.AllScreens;
            foreach (var screen in screens)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("width=");
                sb.Append(screen.Bounds.Width);
                sb.Append("&height=");
                sb.Append(screen.Bounds.Height);
                sb.Append("&scaling-factor=");
                //sb.Append();
                sb.Append("&colour-depth=");
                sb.Append(screen.BitsPerPixel);
                output.Add(sb.ToString());
            }
            return string.Join(",", output);
        }

        static string GetWMIProperty(string queryString, string propertyName)
        {
            string property = string.Empty;
            ManagementObjectSearcher mos = new ManagementObjectSearcher(queryString);
            foreach (ManagementObject mo in mos.Get())
            {
                property += mo[propertyName].ToString();
            }
            if (string.IsNullOrWhiteSpace(property))
                return "";
            else
                return property;
        }

        static string GetWindowSize()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("width=");
            sb.Append(600);
            sb.Append("&height=");
            sb.Append(400);
            return sb.ToString();
        }

        static IEnumerable<byte> CreateGuidBytes(IEnumerable<string> seed)
        {
            List<string> seedList = seed.ToList();
            List<byte> output = new List<byte>();
            foreach (var item in seedList)
            {
                if (string.IsNullOrEmpty(item))
                    seedList.Remove(item);

            }

            int seedCount = seedList.Count;
            bool withinBounds = seedCount > 0 && seedCount < 17;
            bool isOddNumber = (seedCount % 2) != 0;
            if (!withinBounds || isOddNumber)
                return null;

            int bytesPerSeed = 16 / seedCount;
            foreach (var item in seedList)
            {
                char[] charArray = item.ToArray();
                //removes non alphanumeric characters
                charArray = Array.FindAll<char>(charArray, x => (char.IsLetterOrDigit(x)));
                //takes an amount of chars from array, converts them to bytes and adds the result array to output
                output.AddRange(Encoding.UTF8.GetBytes(charArray.Take(bytesPerSeed).ToArray()));
            }
            return output;
        }

        static string GetUserAgent()
        {
            StringBuilder sb = new StringBuilder();
            //OS Family/OS Version+ (Device Manufacturer/Device Model+)
            OperatingSystem os = Environment.OSVersion;
            string osFamily = string.Empty;
            string osVersion = string.Empty;
            string manufacturer = string.Empty;
            string model = string.Empty;

            switch (os.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.WinCE:
                    osFamily = "Windows";
                    break;
                case PlatformID.Unix:
                    osFamily = "Unix";
                    break;
                case PlatformID.Xbox: //lol
                    osFamily = "Xbox";
                    break;
                case PlatformID.MacOSX:
                    osFamily = "Macintosh";
                    break;
            }
            switch (os.Version.Major)
            {
                case 5:
                    if (os.Version.Minor == 0)
                        osVersion = "2000";
                    else
                        osVersion = "XP";
                    break;
                case 6:
                    if (os.Version.Minor == 0)
                        osVersion = "Vista";
                    else if (os.Version.Minor == 1)
                        osVersion = "7";
                    else if (os.Version.Minor == 2)
                        osVersion = "8";
                    else
                        osVersion = "8.1";
                    break;
                case 10:
                    osVersion = "10";
                    break;
                default:
                    osVersion = "";
                    break;
            }

            sb.Append(osFamily);
            sb.Append("/");
            sb.Append(osVersion);
            sb.Append("(");
            sb.Append(manufacturer);
            sb.Append("/");
            sb.Append(model);
            sb.Append(")");

            return sb.ToString();
        }
        static string GetVendorVersion()
        {
            StringBuilder sb = new StringBuilder();
            int versionNum = BasePage.session.ProgramVersion;
            if (versionNum == 0)
                return "";
            sb.Append("uniconta=");
            sb.Append(versionNum);
            return sb.ToString();

        }
        static string GetLicenseId()
        {
            //TODO: Generate subscription Ids from user
            return "";
        }
    }
}
