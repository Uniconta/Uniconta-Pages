using System;
using System.Text;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.Helpers;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    /// <summary>
    /// This is a C# implementation of the CRC algorithm published in the SIE specification (chapter 10).
    /// The control total always uses the character values of codepage 437 (IBM PC-8), regardless of the
    /// encoding the file was read or written with.
    /// </summary>
    public class SIECRC32
    {
        public bool Started = false;
        private const UInt32 CRC32_POLYNOMIAL = 0xEDB88320;

        private readonly UInt32[] _CRCTable = new UInt32[256];
        private UInt32 _crc; // Global variabel för att ackumulera CRC
        private readonly Encoding _encoding;

        public SIECRC32()
        {
            CRC_skapa_tabell();
            _encoding = SIEEncodingHelpers.GetChecksumEncoding();
        }

        // Denna rutin skall anropas för att initiera lookup-tabellen
        // innan beräkningen påbörjas
        private void CRC_skapa_tabell()
        {
            UInt32 i;
            UInt32 j;
            UInt32 crc;
            for (i = 0; i <= 255; i++)
            {
                crc = i;
                for (j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                    {
                        crc = (crc >> 1) ^ CRC32_POLYNOMIAL;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
                _CRCTable[i] = crc;
            }
        }

        // Denna rutin påbörjar CRC-beräkning
        public void Start()
        {
            // Påbörjar CRC-beräkning genom att sätta den globala
            // CRC-ackumulatorn till prekonditioneringsvärdet.
            _crc = 0xFFFFFFFF;
            Started = true;
        }

        /// <summary>
        /// Adds an item to the control total. Brace lines, empty lines and #KSUMMA items are not part of the total.
        /// </summary>
        public void AddData(SIEDataItem item)
        {
            if (!IsChecksummed(item.ItemType)) return;

            CRC_ackumulera(_encoding.GetBytes(item.GetChecksumText()));
        }

        public static bool IsChecksummed(string itemType)
        {
            return itemType != "#KSUMMA" && itemType != "{" && itemType != "}" && itemType.Length > 0;
        }

        public UInt32 Checksum()
        {
            return (_crc ^ 0xFFFFFFFF);
        }

        // Denna rutin anropas för varje textdel som ska ingå
        // i kontrollsumman
        private void CRC_ackumulera(byte[] buffer)
        {
            UInt32 temp1;
            UInt32 temp2;
            foreach (byte p in buffer)
            {
                temp1 = (_crc >> 8) & 0x00FFFFFF;
                temp2 = _CRCTable[((int)_crc ^ p) & 0xff];
                _crc = temp1 ^ temp2;
            }
        }
    }
}
