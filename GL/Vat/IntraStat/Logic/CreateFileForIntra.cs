using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using UnicontaClient.Creditor.Payments;
using Localization = Uniconta.ClientTools.Localization;
using Uniconta.ClientTools.Controls;
using Uniconta.Common.Enums;
using static UnicontaClient.Pages.CreateIntraStatFilePage;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    class IntraHelper
    {
        #region Constants
        public const string VALIDATE_OK = "Ok";
        #endregion

        #region Variables
        private CrudAPI api;
        private string companyRegNo;
        private CountryCode companyCountryId;
        #endregion

        public IntraHelper(CrudAPI api)
        {
            companyRegNo = Regex.Replace(api.CompanyEntity._Id ?? string.Empty, "[^0-9]", "");
            companyCountryId = api.CompanyEntity._CountryId;
        }

        public bool PreValidate()
        {
            if (!Country2Language.IsEU(companyCountryId))
            {
                UnicontaMessageBox.Show(Localization.lookup("AccountCountryNotEu"), Localization.lookup("Warning"));
                return false;
            }

            if (string.IsNullOrWhiteSpace(companyRegNo))
            {
                UnicontaMessageBox.Show(string.Format(Localization.lookup("MissingOBJ"), Localization.lookup("CompanyRegNo")), Localization.lookup("Warning"));
                return false;
            }

            return true;
        }

        public void Validate(IEnumerable<IntrastatClient> intralst, bool compressed, bool onlyValidate)
        {
            var countErr = 0;

            foreach (var intra in intralst)
            {
                var hasErrors = false;
                intra.SystemInfo = null;

                if (compressed && intra.Compressed == false)
                {
                    hasErrors = true;
                    if (intra.SystemInfo == VALIDATE_OK)
                        intra.SystemInfo = Localization.lookup("CompressPosting");
                    else
                        intra.SystemInfo += Environment.NewLine + Localization.lookup("CompressPosting");
                }

                if (intra.IsTriangularTrade)
                {
                    intra.SystemInfo = Localization.lookup("TriangleTrade"); 
                    continue;
                }

                if (intra.ItemCode == null)
                {
                    hasErrors = true;
                    intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                    intra.SystemInfo += string.Format(Localization.lookup("OBJisEmpty"), Localization.lookup("TariffNumber"));
                }
                else
                    intra.ItemCode = Regex.Replace(intra.ItemCode, "[^0-9]", "");

                if (intra.ItemCode != null && intra.ItemCode.Length != 8)
                {
                    hasErrors = true;
                    intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                    intra.SystemInfo += string.Format(Localization.lookup("InvalidValue"), Localization.lookup("TariffNumber"), intra.ItemCode);
                }

                if (intra.Country == CountryCode.Unknown)
                {
                    hasErrors = true;
                    intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                    intra.SystemInfo += intra.SystemInfo + Localization.lookup("CountryNotSet");
                }
                if (intra.Country == companyCountryId)
                {
                    hasErrors = true;
                    intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                    intra.SystemInfo += intra.SystemInfo + Localization.lookup("OwnCountryProblem");
                }
                if (!Country2Language.IsEU(intra.Country))
                {
                    hasErrors = true;
                    intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                    intra.SystemInfo += Localization.lookup("NonEUCountry");
                }

                if (intra.CountryOfOrigin == CountryCode.Unknown && intra._CountryOfOriginUNK == IntraUnknownCountry.None)
                {
                    hasErrors = true;
                    intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                    intra.SystemInfo += string.Format(Localization.lookup("MissingOBJ"), Localization.lookup("CountryOfOrigin"));
                }
                
                if (intra.InvAmount == 0)
                {
                    hasErrors = true;
                    intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                    intra.SystemInfo += Localization.lookup("NoValues");
                }

                if (intra.NetWeight == 0 && intra.IntraUnit == 0 && (string.IsNullOrWhiteSpace(intra.ItemCode) || !intra.ItemCode.Contains("99500000")))
                {
                    hasErrors = true;
                    intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                    intra.SystemInfo += string.Format(Localization.lookup("MissingOBJ"), Localization.lookup("Weight"));
                }

                if (intra.TransType == null)
                {
                    hasErrors = true;
                    intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                    intra.SystemInfo += Localization.lookup("EmptyTransferType");
                }

                if (!string.IsNullOrWhiteSpace(intra.TransType) && intra.TransType.Length != 2)
                {
                    hasErrors = true;
                    intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                    intra.SystemInfo += string.Format(Localization.lookup("InvalidValue"), Localization.lookup("TransferType"), intra.TransType);
                }

                if (intra.ImportOrExport == ImportOrExportIntrastat.Export)
                {
                    if (string.IsNullOrWhiteSpace(intra.fDebtorRegNo))
                    {
                        hasErrors = true;
                        intra.SystemInfo += intra.SystemInfo != null ? Environment.NewLine : null;
                        intra.SystemInfo += string.Format(Localization.lookup("MissingOBJ"), Localization.lookup("DebtorRegNo"));
                    }
                }

                if (hasErrors)
                    countErr++;
                else
                    intra.SystemInfo = VALIDATE_OK;
            }
        }

        class CompressCompare : IEqualityComparer<IntrastatClient>
        {
            public bool Equals(IntrastatClient x, IntrastatClient y)
            {
                int c = x.Country - y.Country;
                if (c != 0)
                    return false;
                c = x.CountryOfOrigin - y.CountryOfOrigin;
                if (c != 0)
                    return false;
                c = string.Compare(x.CountryOfOriginUNK, y.CountryOfOriginUNK);
                if (c != 0)
                    return false;
                c = string.Compare(x.Period, y.Period);
                if (c != 0)
                    return false;
                c = string.Compare(x.fDebtorRegNo, y.fDebtorRegNo);
                if (c != 0)
                    return false;
                c = x.ImportOrExport - y.ImportOrExport;
                if (c != 0)
                    return false;
                c = string.Compare(x.ItemCode, y.ItemCode);
                if (c != 0)
                    return false;
                c = string.Compare(x.TransType, y.TransType);
                if (c != 0)
                    return false;
                if (x.IsTriangularTrade != y.IsTriangularTrade)
                    return false;
                return true;
            }
            public int GetHashCode(IntrastatClient x)
            {
                return (int)(x.Country + 1) * ((int)x.ImportExport + 1) * Util.GetHashCode(x.ItemCode);
            }
        }

        public List<IntrastatClient> Compress(IEnumerable<IntrastatClient> intralst)
        {
            var dictIntra =  new Dictionary<IntrastatClient, IntrastatClient>(new CompressCompare());

            foreach (var intra in intralst)
            {
                intra.Compressed = true;

                IntrastatClient found;
                if (dictIntra.TryGetValue(intra, out found))
                {
                    found.InvAmount += intra.InvAmount;
                    found.NetWeight += intra.NetWeight;
                    found.InvoiceQuantity += intra.InvoiceQuantity;
                    found.IntraUnit += intra.IntraUnit;
                    found.Date = intra.Date;
                    found.WeightPerPCS = 0;
                    found.IntraUnitPerPCS = 0;
                    found._InvoiceNumber = 0;
                }
                else
                    dictIntra.Add(intra, intra);
            }

            if (dictIntra.Count == 0)
                return null;

            return dictIntra.Values.ToList();
        }

    }
}
