using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#if !SILVERLIGHT
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
#else
using System.Windows;
#endif
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Creditor.Payments;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    class CreateEUSaleWithoutVATFile
    {
        static string cvrNummer;
        static double sumOfAllAmounts;

#if !SILVERLIGHT
        public static List<EUSaleWithoutVATClient> CreateEUSaleWithoutVATfile(List<EUSaleWithoutVATClient> listOfEUSaleWithoutVAT, CrudAPI api)
#else
        public static List<EUSaleWithoutVATClient> CreateEUSaleWithoutVATfile(List<EUSaleWithoutVATClient> listOfEUSaleWithoutVAT, CrudAPI api, System.Windows.Controls.SaveFileDialog sfd)
#endif
        {
            var cmp = new CompressCompare();
            var listOfResults = new Dictionary<EUSaleWithoutVATClient, EUSaleWithoutVATClient>(cmp);

            if (listOfEUSaleWithoutVAT.Count <= 0)
            {

                UnicontaMessageBox.Show(Localization.lookup("NoRecordExport"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return null;
            }

            foreach (var euSale in listOfEUSaleWithoutVAT)
            {
                cvrNummer = euSale.cvrNummer;

                if (euSale.isTriangularTrade)
                {
                    euSale.triangularTradeAmount = euSale.itemAmount;
                    euSale.itemAmount = 0;
                    euSale.serviceAmount = 0;
                }

                EUSaleWithoutVATClient found;

                if (listOfResults.TryGetValue(euSale, out found))
                {
                    found.itemAmount += euSale.itemAmount;
                    found.triangularTradeAmount += euSale.triangularTradeAmount;
                    found.serviceAmount += euSale.serviceAmount;
                    found._InvoiceNumber = 0;
                    found._DCAccount = null;
                    if (found.triangularTradeAmount == 0 && found.itemAmount == 0 && found.serviceAmount == 0)
                        found.systemInfo = Localization.lookup("NoValues");
                    else
                        found.systemInfo = Localization.lookup("Compressed");
                }
                else
                    listOfResults.Add(euSale, euSale);
            }
            if (listOfResults == null || listOfResults.Count <= 0)
                return null;

            var resultWithoutZeros = listOfResults.Values.ToList().Where(x => x.triangularTradeAmount != 0 || x.itemAmount != 0 || x.serviceAmount != 0);
            var result = resultWithoutZeros.ToList();

            sumOfAllAmounts = listOfResults.Sum(itemSum => itemSum.Value.ItemAmount) +
                              listOfResults.Sum(serviceSum => serviceSum.Value.serviceAmount) +
                              listOfResults.Sum(ttSum => ttSum.Value.triangularTradeAmount);
            try
            {
#if !SILVERLIGHT
                // Estonian XML generating
                if (api.CompanyEntity._CountryId == CountryCode.Estonia)
                {
                    result = result.Where(r => !String.IsNullOrEmpty(r.VATBuyerNummer)).ToList();
                    GenerateXmlReport(result, api);
                    return result;
                }

                var sfd = UtilDisplay.LoadSaveFileDialog;
                sfd.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.CSV);
                var userClickedSave = sfd.ShowDialog();
                if (userClickedSave != true)
                    return null;
#endif

#if !SILVERLIGHT
                using (var stream = File.Create(sfd.FileName))
#else
                using (var stream = sfd.OpenFile())
#endif
                {
#if !SILVERLIGHT
                    using (var sw = new StreamWriter(stream, Encoding.Default))
#else
                    using (var sw = new StreamWriter(stream))
#endif
                    {
                        CreateAndStreamFirstAndLast(result, sw, true, api, 0);
                        var sumOfAmount = StreamToFile(result, sw);
                        CreateAndStreamFirstAndLast(result, sw, false, api, sumOfAmount);
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {

                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));

                return null;
            }
            return listOfResults.Values.ToList();
        }

        public static long StreamToFile(List<EUSaleWithoutVATClient> listOfImportExport, StreamWriter sw)
        {
            long sumOfAmount = 0;
            var type = new EUSaleWithoutVATClient();
            var seperator = ';';

            var outputFields = new[]
            {
                "recNr", "interntRefNr", "euSaleDate", "cvrNummer", "euCountry", "vatBuyerNummer",
                "itemAmount", "triangularTradeAmount", "serviceAmount"
            };

            var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();

            foreach (var impAndExp in listOfImportExport)
            {
                bool firstColumn = true;

                foreach (var f in fields)
                {
                    if (!firstColumn)
                    {
                        sw.Write(seperator);
                    }
                    else
                    {
                        firstColumn = false;
                    }
                    var val = f.GetValue(impAndExp);
                    string value;

                    if (val is DateTime)
                    {
                        value = ((DateTime)val).ToString("yyyy-MM-dd");
                        var valueLength = value.Length;

                        if (valueLength != 10)
                            value = value.Remove(9, valueLength);
                    }
                    else if (f.Name == "euCountry")
                    {
                        var countryCode = (CountryCode)Enum.Parse(typeof(CountryCode),
                                        ((CreateIntraStatFilePage.EUCountries)val).ToString(), true);

                        if (countryCode == CountryCode.Greece)
                            value = "EL";
                        else
                        {
                            var iso = Enum.GetName(typeof(CountryISOCode), ((int)countryCode));
                            value = iso;
                        }
                    }
                    else if (val is double)
                    {
                        var doubleNumber = NumberConvert.ToLong((double)val);
                        sumOfAmount += doubleNumber;
                        value = doubleNumber.ToString();
                    }
                    else
                    {
                        value = Convert.ToString(val);
                    }
                    //value = Regex.Replace(value, "[\"\';]", " ");
                    sw.Write(value);
                }
                sw.WriteLine();
            }

            return sumOfAmount;
        }

        public static void CreateAndStreamFirstAndLast(List<EUSaleWithoutVATClient> listOfEUSale, StreamWriter sw, bool firstOrLast, CrudAPI api, long sumOfAmount)
        {
            var EUSaleWithoutVAT = new EUSaleWithoutVATClient();
            var streamFileList = new List<EUSaleWithoutVATClient>();
            if (firstOrLast)
            {
                EUSaleWithoutVAT.recNr = "0";
                EUSaleWithoutVAT.cvrNummer = cvrNummer;
                EUSaleWithoutVAT.systemField = "LISTE";
                EUSaleWithoutVAT.filler = new string(';', 5);
                streamFileList.Add(EUSaleWithoutVAT);
            }
            else
            {
                //var amountTotal = listOfEUSale.Sum(itemSum => itemSum.itemAmount) +
                //                  listOfEUSale.Sum(serviceSum => serviceSum.serviceAmount) +
                //                  listOfEUSale.Sum(ttSum => ttSum.triangularTradeAmount);
                EUSaleWithoutVAT.recNr = "10";
                EUSaleWithoutVAT.SalesCount = listOfEUSale.Count;
                EUSaleWithoutVAT.suminvoiceAmount = sumOfAmount;
                EUSaleWithoutVAT.filler = new string(';', 5);
                streamFileList.Add(EUSaleWithoutVAT);
            }

            var seperator = ';';
            var outputFields = new[]
            {
                "recNr", "cvrNummer", "systemField", "salesCount", "suminvoiceAmount", "filler"
            };

            var fields = outputFields.Select(fld => EUSaleWithoutVAT.GetType().GetField(fld)).ToList();

            foreach (var startEnd in streamFileList)
            {
                bool firstColumn = true;

                foreach (var f in fields)
                {
                    if (firstOrLast && (f.Name == "salesCount" || f.Name == "suminvoiceAmount"))
                        continue;
                    else if (!firstOrLast && (f.Name == "cvrNummer" || f.Name == "systemField"))
                        continue;

                    if (!firstColumn)
                    {
                        sw.Write(seperator);
                    }
                    else
                    {
                        firstColumn = false;
                    }


                    var val = f.GetValue(startEnd);
                    string value;

                    if (val is double)
                    {
                        var doubleNumber = NumberConvert.ToLong((double)val);
                        value = doubleNumber.ToString();
                    }
                    else
                        value = Convert.ToString(val);

                    //value = Regex.Replace(value, "[\"\';]", " ");
                    sw.Write(value);
                }
                sw.WriteLine();
            }
        }

#region Estonian VIES report to XML
        public static void CreateXmlFile(Stream sfd, VD_deklaratsioon_Type declaration)
        {
            XmlDocument doc = new XmlDocument();
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            XmlSerializer serializer = new XmlSerializer(typeof(VD_deklaratsioon_Type), String.Empty);
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8
            };
            StringBuilder sb = new StringBuilder();
            XmlWriter xmlWriter = XmlWriter.Create(sfd, xmlWriterSettings);
            serializer.Serialize(xmlWriter, declaration, ns);
            xmlWriter.Close();
            sfd.Close();
        }

        public static void CreateXml(VD_deklaratsioon_Type declaration)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "VIES_declaration_" + declaration.perioodAasta + declaration.perioodKuu + ".xml";
            sfd.Filter = "XML failid (*.xml)|*.xml";
            var savefile = sfd.ShowDialog();
            if (savefile == DialogResult.OK)
            {
                CreateXmlFile(sfd.OpenFile(), declaration);
            }
        }

        internal static void GenerateXmlReport(List<EUSaleWithoutVATClient> invStats, CrudAPI api)
        {
            var declaration = new VD_deklaratsioon_Type();
            declaration.deklareerijaKood = api.CompanyEntity._VatNumber;
            declaration.perioodAasta = invStats.FirstOrDefault().EuSaleDate.Year.ToString();
            declaration.perioodKuu = invStats.FirstOrDefault().EuSaleDate.Month.ToString();

            declaration.aruandeRead = GenerateReportLines(invStats);
            CreateXml(declaration);
        }

        private static aruandeRida_Type[] GenerateReportLines(List<EUSaleWithoutVATClient> invStats)
        {
            List<aruandeRida_Type> read = new List<aruandeRida_Type>();
            foreach (var invStat in invStats)
            {
                aruandeRida_Type uusRida = new aruandeRida_Type();
                uusRida.kmkrKood = new kmkrKood_Type();
                uusRida.kmkrKood.Value = invStat.VATBuyerNummer;

                var countryCode = (CountryCode)Enum.Parse(typeof(CountryCode),
                                        ((CreateIntraStatFilePage.EUCountries)invStat.EUCountry).ToString(), true);

                if (countryCode == CountryCode.Greece)
                    uusRida.kmkrKood.riik = "EL";
                else
                {
                    uusRida.kmkrKood.riik = Enum.GetName(typeof(CountryISOCode), ((int)countryCode));
                }

                uusRida.kaup = Math.Round(invStat.ItemAmount, 0).ToString();
                uusRida.kolmnurktehing = Math.Round(invStat.TriangularTradeAmount, 0).ToString();
                uusRida.teenusteMyyk = Math.Round(invStat.ServiceAmount, 0).ToString();
                read.Add(uusRida);
            }
            return read.ToArray();
        }
#endregion
    }

    public class EUSaleWithoutVAT : DebtorInvoiceLocal
    {
        public string recNr;
        public string cvrNummer;
        public string systemField;
        public string filler;
        public string interntRefNr;
        public DateTime euSaleDate;
        public int salesCount;
        public CreateIntraStatFilePage.EUCountries euCountry;
        public string vatBuyerNummer;
        public double weightPerPcs;
        public double invoiceQuantity;
        public double netWeight;
        public double itemAmount;
        public double triangularTradeAmount;
        public double serviceAmount;
        public double suminvoiceAmount;
        public bool isTriangularTrade;
        public string systemInfo;

        public void SetCompany(int CompanyId)
        {
            _CompanyId = CompanyId;
        }
    }

    public class EUSaleWithoutVATText
    {
        public static string CVRNummer { get { return Uniconta.ClientTools.Localization.lookup("CompanyRegNo"); } }
        public static string RecNr { get { return Uniconta.ClientTools.Localization.lookup("Record"); } }
        public static string InterntRefNr { get { return Uniconta.ClientTools.Localization.lookup("ReferenceNr"); } }
        public static string EuSaleDate { get { return Uniconta.ClientTools.Localization.lookup("Date"); } }
        public static string SalesCount { get { return Uniconta.ClientTools.Localization.lookup("Count"); } }
        public static string EUCountry { get { return Uniconta.ClientTools.Localization.lookup("Country"); } }
        public static string VATBuyerNummer { get { return Uniconta.ClientTools.Localization.lookup("VATNumber"); } }
        public static string ItemAmount { get { return Uniconta.ClientTools.Localization.lookup("ItemAmount"); } }
        public static string ServiceAmount { get { return Uniconta.ClientTools.Localization.lookup("ServiceAmount"); } }
        public static string IsTriangularTrade { get { return Uniconta.ClientTools.Localization.lookup("TriangularTrade"); } }
        public static string TriangularTradeAmount { get { return string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("Amount"), Uniconta.ClientTools.Localization.lookup("TriangularTrade")); } }
        public static string SystemInfoEUSale { get { return Uniconta.ClientTools.Localization.lookup("SystemInfo"); } }
    }

    [ClientTable(LabelKey = "EUSaleWithoutVAT")]
    public class EUSaleWithoutVATClient : EUSaleWithoutVAT, INotifyPropertyChanged
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        [Display(Name = "Account", ResourceType = typeof(DCOrderText))]
        public string AccountEUSale { get { return _DCAccount; } set { _DCAccount = value; NotifyPropertyChanged("AccountEUSale"); } }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string NameAccount { get { return _DCAccount; } set { _DCAccount = value; NotifyPropertyChanged("NameAccount"); } }

        [Display(Name = "CVRNummer", ResourceType = typeof(EUSaleWithoutVATText))]
        public string CVRNummer { get { return cvrNummer; } set { cvrNummer = value; NotifyPropertyChanged("CVRNummer"); } }

        [Display(Name = "VATBuyerNummer", ResourceType = typeof(EUSaleWithoutVATText))]
        public string VATBuyerNummer { get { return vatBuyerNummer; } set { vatBuyerNummer = value; NotifyPropertyChanged("VATBuyerNummer"); } }

        [Display(Name = "RecNr", ResourceType = typeof(EUSaleWithoutVATText))]
        public string RecNr { get { return recNr; } set { recNr = value; NotifyPropertyChanged("RecNr"); } }

        [Display(Name = "InterntRefNr", ResourceType = typeof(EUSaleWithoutVATText))]
        public string InterntRefNr { get { return interntRefNr; } set { interntRefNr = value; NotifyPropertyChanged("InterntRefNr"); } }

        [Display(Name = "EuSaleDate", ResourceType = typeof(EUSaleWithoutVATText))]
        public DateTime EuSaleDate { get { return euSaleDate; } set { euSaleDate = value; NotifyPropertyChanged("EuSaleDate"); } }

        [Display(Name = "SalesCount", ResourceType = typeof(EUSaleWithoutVATText))]
        public int SalesCount { get { return salesCount; } set { salesCount = value; NotifyPropertyChanged("SalesCount"); } }

        [Display(Name = "EUCountry", ResourceType = typeof(EUSaleWithoutVATText))]
        public CreateIntraStatFilePage.EUCountries EUCountry { get { return euCountry; } set { euCountry = value; NotifyPropertyChanged("EUCountry"); } }

        [Price]
        [Display(Name = "ItemAmount", ResourceType = typeof(EUSaleWithoutVATText))]
        public double ItemAmount { get { return itemAmount; } set { itemAmount = value; NotifyPropertyChanged("ItemAmount"); } }

        [Price]
        [Display(Name = "ServiceAmount", ResourceType = typeof(EUSaleWithoutVATText))]
        public double ServiceAmount { get { return serviceAmount; } set { serviceAmount = value; NotifyPropertyChanged("ServiceAmount"); } }

        [Display(Name = "IsTriangularTrade", ResourceType = typeof(IntrastatClassText))]
        public bool IsTriangularTrade { get { return isTriangularTrade; } set { isTriangularTrade = value; NotifyPropertyChanged("IsTriangularTrade"); } }

        [Price]
        [Display(Name = "TriangularTradeAmount", ResourceType = typeof(EUSaleWithoutVATText))]
        public double TriangularTradeAmount { get { return triangularTradeAmount; } set { triangularTradeAmount = value; NotifyPropertyChanged("TriangularTradeAmount"); } }

        [Display(Name = "SystemInfoEUSale", ResourceType = typeof(EUSaleWithoutVATText))]
        public string SystemInfoEUSale { get { return systemInfo; } set { systemInfo = value; NotifyPropertyChanged("SystemInfoEUSale"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    class CompressCompare : IEqualityComparer<EUSaleWithoutVATClient>
    {
        public bool Equals(EUSaleWithoutVATClient x, EUSaleWithoutVATClient y)
        {
            int c = x.euCountry - y.euCountry;
            if (c != 0)
                return false;
            c = string.Compare(x.vatBuyerNummer, y.vatBuyerNummer);
            if (c != 0)
                return false;
            return true;
        }
        public int GetHashCode(EUSaleWithoutVATClient x)
        {
            return (int)(x.euCountry + 1) * Util.GetHashCode(x.vatBuyerNummer);
        }
    }

    // Estonia VIES report XML schema class
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2046.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.emta.ee/VD/xsd/webimport/v1")]
    [System.Xml.Serialization.XmlRootAttribute("VD_deklaratsioon", Namespace = "http://www.emta.ee/VD/xsd/webimport/v1", IsNullable = false)]
    public partial class VD_deklaratsioon_Type
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string deklareerijaKood;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified, DataType = "integer")]
        public string perioodAasta;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified, DataType = "integer")]
        public string perioodKuu;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        [System.Xml.Serialization.XmlArrayItemAttribute("aruandeRida", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = false)]
        public aruandeRida_Type[] aruandeRead;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2046.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.emta.ee/VD/xsd/webimport/v1")]
    public partial class aruandeRida_Type
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public kmkrKood_Type kmkrKood;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified, DataType = "integer")]
        public string kaup;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified, DataType = "integer")]
        public string kolmnurktehing;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified, DataType = "integer")]
        public string teenusteMyyk;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.2046.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.emta.ee/VD/xsd/webimport/v1")]
    public partial class kmkrKood_Type
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string riik;

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value;
    }
}