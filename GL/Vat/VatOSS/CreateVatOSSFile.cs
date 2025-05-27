using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Enums;
using Uniconta.Common.Utility;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    class CreateVatOSSFile
    {
        #region Constants
        public const string VALIDATE_OK = "Ok";
        public const string VATTYPE_MOSS = "moss";

        public const string MOSSTYPE_001 = "001"; //(EU-ordningen) Salg af varer fra MSID
        public const string MOSSTYPE_002 = "002"; //(EU-ordningen) Salg af ydelser fra MSID
        public const string MOSSTYPE_003 = "003"; //(EU-ordningen) Salg af varer fra fast forretningssted
        public const string MOSSTYPE_004 = "004"; //(EU-ordningen) Salg af ydelser fra fast forretningssted
        public const string MOSSTYPE_005 = "005"; //(EU-ordningen) Salg af varer fra afsendelsesforretningssted
        public const string MOSSTYPE_006 = "006"; //(EU-ordningen) Salg fra land uden registrering
        public const string MOSSTYPE_007 = "007"; //(EU-ordningen) Intet salg fra et fast forretningssted / afsendelsesforretningssted
        public const string MOSSTYPE_008 = "008"; //(EU-ordningen) Nulindberetning
        public const string MOSSTYPE_009 = "009"; //(EU-ordningen) Rettelser
        public const string MOSSTYPE_020 = "020"; //(Importordningen) Import af varer fra MSID
        public const string MOSSTYPE_021 = "021"; //(Importordningen) Nulindberetning
        public const string MOSSTYPE_030 = "030"; //(Ikke-EU-ordningen) Salg af ydelser fra MSID 
        public const string MOSSTYPE_031 = "031"; //(Ikke-EU-ordningen) Nulindberetning
        #endregion

        #region Variables
        private CrudAPI api;
        #endregion

        public CreateVatOSSFile(CrudAPI api)
        {
            this.api = api;
        }

        public bool CreateFile(IEnumerable<VatOSSTable> listVatOSS)
        {
            var result = listVatOSS.Where(s => s.SystemInfo == VALIDATE_OK).ToList();


            if (result.Count > 0)
            {
                var sort = new VatOSSTableTypeSort();
                result.Sort(sort);

                var sfd = UtilDisplay.LoadSaveFileDialog;
                sfd.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.CSV);
                var userClickedSave = sfd.ShowDialog();
                if (userClickedSave != true)
                    return false;

                try
                {
                    using (var stream = File.Create(sfd.FileName))
                    {
                        var sw = new StreamWriter(stream, Encoding.Default);
                        var sumOfAmount = StreamToFile(result, sw);
                        sw.Flush();
                    }
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                    return false;
                }
            }
            return true;
        }

        public List<VatOSSTable> CompressVatOSS(IEnumerable<VatOSSTable> listVatOSS)
        {
            var dictVatOSS =  new Dictionary<VatOSSTable, VatOSSTable>(new VatOSSTableSort());
            var vatOSSTableLst = new List<VatOSSTable>();

            foreach (var rec in listVatOSS)
            {
                if (rec.MOSSType == MOSSTYPE_007 || rec.MOSSType == MOSSTYPE_008)
                {
                    vatOSSTableLst.Add(rec);
                }
                else
                {
                    VatOSSTable found;
                    if (dictVatOSS.TryGetValue(rec, out found))
                    {
                        found._Amount += rec._Amount;
                        found._VatAmount += rec._VatAmount;
                        found._InvoiceNumber = 0;
                        found._Item = null;
                    }
                    else
                    {
                        dictVatOSS.Add(rec, rec);
                    }
                }
                rec.Compressed = true;
            }

            if (vatOSSTableLst.Count == 0 && dictVatOSS.Count == 0)
                return null;

            vatOSSTableLst.AddRange(dictVatOSS.Values.ToList());

            return vatOSSTableLst;
        }

        public bool PreValidate()
        {
            if (!Country2Language.IsEU(api.CompanyEntity._CountryId))
            {
                UnicontaMessageBox.Show(Localization.lookup("AccountCountryNotEu"), Localization.lookup("Warning"));
                return false;
            }
            return true;
        }

        public void Validate(IEnumerable<VatOSSTable> listVatOSS, int reportType, bool compressed, bool onlyValidate)
        {
            var countErr = 0;
            var errText = StringBuilderReuse.Create();
            foreach (var rec in listVatOSS)
            {
                var debtor = rec.DebtorRef;
                rec.SystemInfo = VALIDATE_OK;
                errText.Clear();

                if (compressed && rec.Compressed == false)
                    errText.Append(Localization.lookup("CompressPosting"));

                if (reportType == 0 && rec.MOSSType != MOSSTYPE_001 && rec.MOSSType != MOSSTYPE_002 && rec.MOSSType != MOSSTYPE_003 && rec.MOSSType != MOSSTYPE_004 &&
                    rec.MOSSType != MOSSTYPE_005 && rec.MOSSType != MOSSTYPE_006 && rec.MOSSType != MOSSTYPE_007 && rec.MOSSType != MOSSTYPE_008 && rec.MOSSType != MOSSTYPE_009)
                    errText.Append(string.Format(Localization.lookup("ActionNotAllowedObj"), Localization.lookup("MOSSType"), Localization.lookup("UnionSchemeOSS")));
                else if (reportType == 1 && rec.MOSSType != MOSSTYPE_020 && rec.MOSSType != MOSSTYPE_021)
                    errText.Append(string.Format(Localization.lookup("ActionNotAllowedObj"), Localization.lookup("MOSSType"), Localization.lookup("NonUnionSchemeOSS")));
                else if (reportType == 2 && rec.MOSSType != MOSSTYPE_030 && rec.MOSSType != MOSSTYPE_031)
                    errText.Append(string.Format(Localization.lookup("ActionNotAllowedObj"), Localization.lookup("MOSSType"), Localization.lookup("ImportSchemeOSS")));

                if (errText.Length == 0 && rec.MOSSType == null)
                    errText.Append(fieldCannotBeEmpty("VATMOSSType"));

                if (rec.MOSSType == MOSSTYPE_007)
                {
                    if (errText.Length == 0)
                    {
                        if (rec.ShipmentCountry == null && rec.BusinessCountry == null)
                            errText.Append(string.Format("{0} {1}/{2}", Localization.lookup("FieldCannotBeEmpty"), Localization.lookup("BusinessCountry"), Localization.lookup("ShipmentCountry")));

                        if (errText.Length == 0 && rec.Id == null)
                            errText.Append(fieldCannotBeEmpty("Id"));
                    }
                }
                else if (rec.MOSSType != MOSSTYPE_008 && rec.MOSSType == MOSSTYPE_021 && rec.MOSSType == MOSSTYPE_031)
                {
                    if (errText.Length == 0 && debtor?.Country == null)
                        errText.Append(Localization.lookup("CountryNotSet"));

                    if (errText.Length == 0 && rec.Country == null)
                        errText.Append(fieldCannotBeEmpty("VatCountry"));
                }

                if (errText.Length == 0 && rec.BusinessCountry != null && rec.ShipmentCountry != null)
                    errText.Append(string.Format("{0}/{1} {2}", Localization.lookup("BusinessCountry"), Localization.lookup("ShipmentCountry"), Localization.lookup("CombinationNotAllowed").ToLower()));

                if (errText.Length == 0 && (rec.MOSSType == MOSSTYPE_003 || rec.MOSSType == MOSSTYPE_004))
                {
                    if (rec.BusinessCountry == null)
                        errText.Append(fieldCannotBeEmpty("BusinessCountry"));

                    if (errText.Length == 0 && rec.Id == null)
                        errText.Append(fieldCannotBeEmpty("Id"));
                }

                if (errText.Length == 0 && rec.MOSSType == MOSSTYPE_005)
                {
                    if (rec.ShipmentCountry == null)
                        errText.Append(fieldCannotBeEmpty("ShipmentCountry"));
                    if (errText.Length == 0 && rec.Id == null)
                        errText.Append(fieldCannotBeEmpty("Id"));
                }

                if (errText.Length > 0)
                {
                    rec.SystemInfo = errText.ToString();
                    countErr++;
                }
            }
            errText.Release();
        }

        private long StreamToFile(List<VatOSSTable> listOfImportExport, StreamWriter sw)
        {
            long sumOfAmount = 0;

            foreach (var rec in listOfImportExport)
            {
                rec.SystemInfo = Localization.lookup("Exported");

                sw.Write(rec.MOSSType); sw.Write(';');

                if (rec.MOSSType == MOSSTYPE_001 || rec.MOSSType == MOSSTYPE_002 || rec.MOSSType == MOSSTYPE_020 || rec.MOSSType == MOSSTYPE_030)
                {
                    sw.Write(rec.fCountry); sw.Write(';');
                    sw.Write(rec.RateType); sw.Write(';');
                }

                if (rec.MOSSType == MOSSTYPE_003 || rec.MOSSType == MOSSTYPE_004 || rec.MOSSType == MOSSTYPE_005)
                {
                    if (rec.MOSSType == MOSSTYPE_005)
                    {
                        if (rec._ShipmentCountry != 0)
                            sw.Write(rec.fShipmentCountry);
                    }
                    else
                    {
                        if (rec._BusinessCountry != 0)
                            sw.Write(rec.fBusinessCountry); 
                    }
                    sw.Write(';');

                    sw.Write(rec.Id); sw.Write(';');
                    sw.Write(rec.fCountry); sw.Write(';');
                    sw.Write(rec.RateType); sw.Write(';');
                }

                if (rec.MOSSType == MOSSTYPE_006)
                {
                    sw.Write(rec.fVatCountry); sw.Write(';');
                    sw.Write(rec.fCountry); sw.Write(';');
                    sw.Write(rec.RateType); sw.Write(';');
                }

                if (rec.MOSSType == MOSSTYPE_007)
                {
                    if (rec._BusinessCountry != 0)
                        sw.Write(rec.fBusinessCountry);
                    else if (rec._ShipmentCountry != 0)
                        sw.Write(rec.fShipmentCountry);
                    sw.Write(';');

                    sw.Write(rec.Id);
                }
                else if (rec.MOSSType == MOSSTYPE_008 || rec.MOSSType == MOSSTYPE_021 || rec.MOSSType == MOSSTYPE_031)
                {
                    var companyRegNo = Regex.Replace(api.CompanyEntity._Id ?? string.Empty, "[^0-9]", "");
                    sw.Write(companyRegNo);
                }
                else
                {
                    sw.Write(rec.Rate.ToString("F")); sw.Write('%'); sw.Write(';');
                    sw.Write(rec.Amount.ToString("F")); sw.Write(';');
                    sw.Write(rec.VatAmount.ToString("F"));
                }
                sw.WriteLine();
            }

            return sumOfAmount;
        }

       
        static string fieldCannotBeEmpty(string field)
        {
            return String.Format("{0} : {1}",
                    Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"),
                    Uniconta.ClientTools.Localization.lookup(field));
        }
    }

    public class VatOSSTableSort : IEqualityComparer<VatOSSTable>
    {
        public bool Equals(VatOSSTable x, VatOSSTable y)
        {
            int c = string.Compare(x.MOSSType, y.MOSSType);
            if (c != 0)
                return false;
            c = string.Compare(x.Vat, y.Vat);
            if (c != 0)
                return false;
            c = (int)x.Country.GetValueOrDefault() - (int)y.Country.GetValueOrDefault();
            if (c != 0)
                return false;
            c = (int)x._BusinessCountry - (int)y._BusinessCountry;
            if (c != 0)
                return false;
            c = (int)x._ShipmentCountry - (int)y._ShipmentCountry;
            if (c != 0)
                return false;
            return true;
        }

        public int GetHashCode(VatOSSTable obj)
        {
            return Util.GetHashCode(obj.MOSSType) * Util.GetHashCode(obj.Vat) * Util.GetHashCode((int)obj.Country) * Util.GetHashCode((int)obj._BusinessCountry) * Util.GetHashCode((int)obj._BusinessCountry);
        }
    }

    public class VatOSSTableVatSort : IComparer<VatOSSTable>, IComparer
    {
        public int Compare(VatOSSTable _x, VatOSSTable _y)
        {
            return string.Compare(_x.Vat, _y.Vat);
        }
        public int Compare(object _x, object _y) { return Compare((VatOSSTable)_x, (VatOSSTable)_y); }
    }

    class VatOSSTableTypeSort : IComparer<VatOSSTable>
    {
        public int Compare(VatOSSTable _x, VatOSSTable _y)
        {
            int c = _x._MOSSType - _y._MOSSType;
            if (c != 0)
                return c;
            c = _x.Country == null || _y.Country == null ? 0 : (int)_x.Country - (int)_y.Country;
            if (c != 0)
                return c;
            var v = _x.Rate - _y.Rate;
            if (v > 0.001d)
                return 1;
            if (v < -0.001d)
                return -1;

            return 0;
        }
    }

   

    public class VatOSSTableText
    {
        public static string Amount { get { return Uniconta.ClientTools.Localization.lookup("Amount"); } }
        public static string Date { get { return Uniconta.ClientTools.Localization.lookup("Date"); } }
        public static string Compressed { get { return Uniconta.ClientTools.Localization.lookup("Compressed"); } }
        public static string MOSSTypeName { get { return string.Format("{0} {1}", Localization.lookup("MOSSType"), Localization.lookup("Description").ToLower()); } }
    }

    [ClientTable(LabelKey = "VatOSS")] 
    public class VatOSSTable: INotifyPropertyChanged, UnicontaBaseEntity
    {
        const string COUNTRYCODE_GREECE = "EL";
        public int _CompanyId;

        GLVatClient _glVatRef;
        GLVatClient vatRef { get { return _glVatRef ?? (_glVatRef = (GLVatClient)ClientHelper.GetRef(_CompanyId, typeof(GLVatClient), _Vat)); } }

        public string _Account;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Debtor))]
        [Display(Name = "Account", ResourceType = typeof(DCOrderText))]
        public string Account { get { return _Account; } set { _Account = value; NotifyPropertyChanged("Account"); } }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string DebtorName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.Debtor), Account); } }

        [Display(Name = "Country", ResourceType = typeof(DCAccountText))]
        public CountryCode? Country { get { return DebtorRef._Country != CountryCode.Unknown ? DebtorRef._Country : (CountryCode?)null; } }
        public string fCountry { get { if (Country == CountryCode.Greece) return COUNTRYCODE_GREECE; else return Enum.GetName(typeof(CountryISOCode), (int)Country); } }

        public DateTime _Date;
        [Display(Name = "Date", ResourceType = typeof(VatOSSTableText))]
        public DateTime Date { get { return _Date; } set { _Date = value; NotifyPropertyChanged("Date"); } }

        public long _InvoiceNumber;
        [Display(Name = "InvoiceNumber", ResourceType = typeof(DCInvoiceText))]
        public long InvoiceNumber { get { return _InvoiceNumber; } }

        private bool _Compressed;
        [Display(Name = "Compressed", ResourceType = typeof(VatOSSTableText))]
        public bool Compressed { get { return _Compressed; } set { _Compressed = value; NotifyPropertyChanged("Compressed"); } }

        public string _Item;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvItem))]
        [Display(Name = "Item", ResourceType = typeof(InventoryText))]
        public string Item { get { return _Item; } }

        [Display(Name = "ItemName", ResourceType = typeof(InvTransText))]
        [NoSQL]
        public string ItemName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.InvItem), Item); } }

        private string _SystemInfo;
        [Display(Name = "SystemInfo", ResourceType = typeof(DCTransText))]
        public string SystemInfo { get { return _SystemInfo; } set { _SystemInfo = value; NotifyPropertyChanged("SystemInfo"); } }

        public double _Amount;
        [Display(Name = "Amount", ResourceType = typeof(VatOSSTableText))]
        public double Amount { get { return _Amount; } }

        public double _VatAmount;
        [Display(Name = "VatAmount", ResourceType = typeof(DCTransText))]
        public double VatAmount { get { return _VatAmount; } }

        public string _Vat;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLVat))]
        [Display(Name = "Vat", ResourceType = typeof(GLDailyJournalText)), Key]
        public string Vat { get { return _Vat; } }

        [Display(Name = "Rate", ResourceType = typeof(GLVatText))]
        public double Rate { get { return vatRef != null ? vatRef._Rate : 0; } }

        public string _Id;
        [Display(Name = "Id")]
        public string Id { get { return _Id; }  set { _Id = value; } }

        public CountryCode _VatCountry;
        [Display(Name = "VatCountry", ResourceType = typeof(GLVatText))]
        public CountryCode? VatCountry { get { return _VatCountry != 0 ? (CountryCode?)_VatCountry : (CountryCode?)null; } set { _VatCountry = value ?? 0; NotifyPropertyChanged("VatCountry"); } }
        public string fVatCountry { get { if (_VatCountry == CountryCode.Greece) return COUNTRYCODE_GREECE; else return Enum.GetName(typeof(CountryISOCode), (int)_VatCountry); } }

        public CountryCode _BusinessCountry;
        [Display(Name = "BusinessCountry", ResourceType = typeof(GLVatText))]
        public CountryCode? BusinessCountry { get { return _BusinessCountry != 0 ? (CountryCode?)_BusinessCountry : (CountryCode?)null; } set { _BusinessCountry = value ?? 0; NotifyPropertyChanged("BusinessCountry"); } }
        public string fBusinessCountry { get { if (_BusinessCountry == CountryCode.Greece) return COUNTRYCODE_GREECE; else return Enum.GetName(typeof(CountryISOCode), (int)_BusinessCountry); } }

        public CountryCode _ShipmentCountry;
        [Display(Name = "ShipmentCountry", ResourceType = typeof(GLVatText))]
        public CountryCode? ShipmentCountry { get { return _ShipmentCountry != 0 ? (CountryCode?)_ShipmentCountry : (CountryCode?)null; } set { _ShipmentCountry = value ?? 0; NotifyPropertyChanged("ShipmentCountry"); } }
        public string fShipmentCountry { get { if (_ShipmentCountry == CountryCode.Greece) return COUNTRYCODE_GREECE; else return Enum.GetName(typeof(CountryISOCode), (int)_ShipmentCountry); } }

        [AppEnumAttribute(EnumName = "VATRateType")]
        [Display(Name = "RateType", ResourceType = typeof(GLVatText))]
        public string RateType { get { return vatRef != null ? AppEnums.VATRateType.ToString((int)vatRef._RateType) : null; } }

        public int _MOSSType;
        [AppEnumAttribute(EnumName = "VATMOSSType")]
        [Display(Name = "MOSSType", ResourceType = typeof(GLVatText))]
        public string MOSSType { get { return AppEnums.VATMOSSType.ToString((int)_MOSSType); } set { if (value == null) return; _MOSSType = (byte)AppEnums.VATMOSSType.IndexOf(value); NotifyPropertyChanged("MOSSType"); } }

        public string _MOSSTypeName;
        [Display(Name = "MOSSTypeName", ResourceType = typeof(VatOSSTableText))]
        public string MOSSTypeName { get { return _MOSSTypeName; } }

        [ReportingAttribute]
        public DebtorClient DebtorRef
        {
            get
            {
                return ClientHelper.GetRefClient<Uniconta.ClientTools.DataModel.DebtorClient>(_CompanyId, typeof(Uniconta.DataModel.Debtor), _Account);
            }
        }

        [ReportingAttribute]
        public InvItemClient InvItem
        {
            get
            {
                return ClientHelper.GetRefClient<InvItemClient>(_CompanyId, typeof(Uniconta.DataModel.InvItem), _Item);
            }
        }

        [ReportingAttribute]
        public GLVatClient VatRef
        {
            get
            {
                return vatRef;
            }
        }

        [ReportingAttribute]
        public CompanyClient CompanyRef { get { return Global.CompanyRef(_CompanyId); } }

        public int CompanyId { get { return _CompanyId; } set { _CompanyId = value; } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Type BaseEntityType() { return GetType(); }
        public void loadFields(CustomReader r, int SavedWithVersion) { }
        public void saveFields(CustomWriter w, int SaveVersion) { }
        public int Version(int ClientAPIVersion) { return 1; }
        public int ClassId() { return 37789; }
    }
}