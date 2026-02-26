using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Uniconta.ClientTools.Page;
using UnicontaClient;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection
{
    static class FraudPreventionInfo
    {
        public static string ClientDeviceId { get { return GetDeviceId(); } }
        public static string ClientUserIds { get { return GetUserId(); } }
        public static string ClientTimezone { get { return GetTimezone(); } }
        public static string ClientLocalIps { get { return GetLocalIps(); } }
        public static string ClientLocalIpsTimestamp { get { return GetLocalIpsTimestamp(); } }

        public static string ClientScreens { get { return GetClientScreens(); } }
        public static string WindowSize { get { return GetWindowSize(); } }
        public static string UserAgent { get { return GetUserAgent(); } }
        public static string VendorName { get { return GetVendorName(); } }

        public static string VendorVersion { get { return GetVendorVersion(); } }
        public static string LicenseId { get { return GetLicenseId(); } }
        public static string MacAddresses { get { return GetMacAddresses(); } }

        static string GetDeviceId()
        {
            try
            {
                string moboString = GetWMIProperty("SELECT * FROM Win32_BaseBoard", "SerialNumber");

                //Alternative method of getting the serial number, if the first one did not work
                if (String.IsNullOrEmpty(moboString))
                    moboString = GetWMIProperty("SELECT * FROM win32_bios", "SerialNumber");

                string procId = GetWMIProperty("SELECT * FROM Win32_Processor", "ProcessorID");
                bool moboNull = string.IsNullOrWhiteSpace(moboString);
                bool procNull = string.IsNullOrWhiteSpace(procId);
                if (moboNull && procNull)
                    return GetOrGenerateAndSaveDeviceId();
                byte[] seedBytes = CreateGuidBytes(new string[2] { moboString, procId }).ToArray();
                if (seedBytes.Length != 16)
                    return GetOrGenerateAndSaveDeviceId();

                Guid output = new Guid(seedBytes);
                return output.ToString();
            }
            catch
            {
                return GetOrGenerateAndSaveDeviceId();
            }
        }

        static string GetUserId()
        {
            string userName = Environment.UserName;
            string vendorAccount = BasePage.session.User._Name;

            //Remove lithuanian diacritics from windows name and username
            var lithuanianDiacritics = new Dictionary<string, string>()
            {
                { "ą", "a" },
                { "č", "c" },
                { "ę", "e" },
                { "ė", "e" },
                { "į", "i" },
                { "š", "s" },
                { "ų", "u" },
                { "ū", "u" },
                { "ž", "z" },
                { "Ą", "A" },
                { "Č", "C" },
                { "Ę", "E" },
                { "Ė", "E" },
                { "Į", "I" },
                { "Š", "S" },
                { "Ų", "U" },
                { "Ū", "U" },
                { "Ž", "Z" }
            };

            foreach (var symbol in lithuanianDiacritics)
            {
                userName = userName.Replace(symbol.Key, symbol.Value);
                vendorAccount = vendorAccount.Replace(symbol.Key, symbol.Value);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("os=");
            sb.Append(Uri.EscapeDataString(userName));
            sb.Append("&uniconta-account=");
            sb.Append(Uri.EscapeDataString(vendorAccount));

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
                            addresses.Add(Uri.EscapeDataString(ip.Address.ToString()));
                        }
                    }
                }
            }
            return string.Join(",", addresses);
        }

        static string GetLocalIpsTimestamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        static string GetMacAddresses()
        {
            var macs = NetworkInterface.GetAllNetworkInterfaces()
                                       .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                                       .Select(ni => ni.GetPhysicalAddress().ToString())
                                       .Where(x => !String.IsNullOrEmpty(x))
                                       .Select(x => Uri.EscapeDataString(x))
                                       .ToArray();

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

                var dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                EnumDisplaySettings(screen.DeviceName, -1, ref dm);

                var scalingFactor = Math.Round(Decimal.Divide(dm.dmPelsWidth, screen.Bounds.Width), 0);

                sb.Append("&scaling-factor=");
                sb.Append(scalingFactor);


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
                property += mo[propertyName]?.ToString();
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
            sb.Append(Math.Round(App.Current.MainWindow.Width, 0).ToString());

            sb.Append("&height=");
            sb.Append(Math.Round(App.Current.MainWindow.Height, 0).ToString());

            return sb.ToString();
        }

        static IEnumerable<byte> CreateGuidBytes(IEnumerable<string> seed)
        {
            List<string> seedList = seed.Where(x => !String.IsNullOrEmpty(x)).ToList();
            List<byte> output = new List<byte>();

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

        static string GetOrGenerateAndSaveDeviceId()
        {
            if (System.IO.File.Exists("UKVATDeviceId"))
                return System.IO.File.ReadAllText("UKVATDeviceId");

            var deviceId = Guid.NewGuid().ToString();

            System.IO.File.WriteAllText("UKVATDeviceId", deviceId);

            return deviceId;
        }

        static string GetUserAgent()
        {
            StringBuilder sb = new StringBuilder();
            //OS Family/OS Version+ (Device Manufacturer/Device Model+)
            var os = Environment.OSVersion;



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

            try
            {
                manufacturer = GetWMIProperty("SELECT * FROM Win32_ComputerSystem", "Manufacturer");
                model = GetWMIProperty("SELECT * FROM Win32_ComputerSystem", "Model");
            }
            catch { }

            sb.Append("os-family=");
            sb.Append(Uri.EscapeDataString(osFamily));

            sb.Append("&os-version=");
            sb.Append(Uri.EscapeDataString(osVersion));

            sb.Append("&device-manufacturer=");
            sb.Append(Uri.EscapeDataString(manufacturer));

            sb.Append("&device-model=");
            sb.Append(Uri.EscapeDataString(model));

            return sb.ToString();
        }

        static string GetVendorName()
        {
            return "Uniconta";
        }

        static string GetVendorVersion()
        {
            StringBuilder sb = new StringBuilder();
            int versionNum = BasePage.session.ServerVersion;
            if (versionNum == 0)
                return "";

            sb.Append("uniconta=");
            sb.Append(versionNum);
            return sb.ToString();
        }

        static string GetLicenseId()
        {
            return String.Empty;
        }


        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 0x20;
        private const int CCHFORMNAME = 0x20;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public ScreenOrientation dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }
}