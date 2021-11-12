using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UnicontaClient.Models;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.API.System;
using Uniconta.Common.Enums;
using Uniconta.Common.Utility;
using Localization = Uniconta.ClientTools.Localization;
using static UnicontaClient.Pages.CreateIntraStatFilePage;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreateIntraStatFilePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(IntrastatClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class CreateIntraStatFilePage : GridBasePage
    {
        private IntraHelper intraHelper;
        private bool compressed;
        static DateTime DefaultFromDate, DefaultToDate;
        static bool DefaultImp, DefaultExp;

        public override string NameOfControl
        {
            get { return TabControls.CreateIntraStatFilePage; }
        }

        public CreateIntraStatFilePage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        private void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgIntraStatGrid;
            SetRibbonControl(localMenu, dgIntraStatGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgIntraStatGrid.api = api;
            dgIntraStatGrid.BusyIndicator = busyIndicator;
            dgIntraStatGrid.ShowTotalSummary();

            txtDateFrm.DateTime = DefaultFromDate == DateTime.MinValue ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 01): DefaultFromDate;
            txtDateTo.DateTime = DefaultToDate == DateTime.MinValue ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)) : DefaultToDate;

            checkImport.IsChecked = DefaultImp;
            checkExport.IsChecked = DefaultExp;

            intraHelper = new IntraHelper(api);
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override async void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.InvGroup) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var listIntraStat = dgIntraStatGrid.GetVisibleRows() as IEnumerable<IntrastatClient>;
            var selectedItem = dgIntraStatGrid.SelectedItem as IntrastatClient;
            switch (ActionType)
            {
                case "Search":
                    if (listIntraStat != null)
                        btnSearch();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgIntraStatGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    break;
                case "Compress":
                    if (listIntraStat != null)
                        Compress();
                    break;
                case "Validate":
                    if (listIntraStat != null)
                        CallValidate(true);
                    break;
                case "ExportFile":
                    if (listIntraStat != null)
                        CreateFile();
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void Compress()
        {
            try
            {
                if (!CallPrevalidate())
                    return;

                var intralst = intraHelper.Compress((IEnumerable<IntrastatClient>)dgIntraStatGrid.GetVisibleRows());
                if (intralst == null || intralst.Count == 0)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoLinesFound"), Uniconta.ClientTools.Localization.lookup("Warning"));
                else
                {
                    InvoiceNumber.Visible = false;
                    DCAccount.Visible = false;
                    AccountName.Visible = false;
                    Item.Visible = false;
                    ItemName.Visible = false;
                    WeightPerPCS.Visible = false;
                    IntraUnitPerPCS.Visible = false;
                    Date.Visible = false;
                    DebtorRegNo.Visible = checkExport.IsChecked.Value;
                    fDebtorRegNo.Visible = checkExport.IsChecked.Value;
                    Compressed.Visible = true;
                    SystemInfo.Visible = true;

                    InvoiceQuantity.ReadOnly = true;
                    NetWeight.ReadOnly = true;

                    dgIntraStatGrid.ItemsSource = intralst;
                    dgIntraStatGrid.Visibility = Visibility.Visible;
                    dgIntraStatGrid.UpdateTotalSummary();

                    compressed = true;

                    CallValidate(true);
                }
            }
            catch (Exception e)
            {
                UnicontaMessageBox.Show(e, Uniconta.ClientTools.Localization.lookup("Exception"));
                throw;
            }
        }

        private Dictionary<string, int> MapColumnsToIndices()
        {
            var dictionaryColumnIndices = new Dictionary<string, int>(20);

            int idx = 1;
            string key = "CompanyRegNo";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 2;
            key = "Period";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 3;
            key = "ImportExport";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 4;
            key = "ItemCode";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 5;
            key = "TransType";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 6;
            key = "PartnerCountry";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 7;
            key = "NetWeight";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 8;
            key = "IntraUnit";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 9;
            key = "fInvAmount";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 10;
            key = "InternalRefNo";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 11;
            key = "fDebtorRegNo";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 12;
            key = "fCountryOfOrigin";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            return dictionaryColumnIndices;
        }
      
        private void CreateFile()
        {
            if (compressed == false)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CompressPosting"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            var intralst = CallValidate(false);
            if (intralst == null)
                return;
            
            var lstIntraStat = intralst.Where(s => s.SystemInfo == IntraHelper.VALIDATE_OK).ToArray();
          
            var countOk = lstIntraStat.Count();
            var countErr = intralst.Count() - countOk;

            if (countOk > 0)
            {
                var mappedItems = MapColumnsToIndices();

                var sfd = UtilDisplay.LoadSaveFileDialog;
                sfd.DefaultExt = "xlsx";
                sfd.Filter = "XLSX Files (*.xlsx)|*.xlsx";
                sfd.FilterIndex = 1;

                bool? userClickedSave = sfd.ShowDialog();
                if (userClickedSave != true)
                    return;

                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = string.Format(Localization.lookup("ExportingFile"), Localization.lookup("IntraStat"));

                Stream stream = null;
                try
                {
#if !SILVERLIGHT
                    stream = File.Create(sfd.FileName);
#endif
                    var cnt = ExportDataGrid(stream, (UnicontaBaseEntity[])lstIntraStat, mappedItems);

                    if (cnt > 0)
                        foreach (var rec in lstIntraStat) { rec.SystemInfo = Localization.lookup("Exported"); }

                    stream.Flush();
                    stream.Close();

                    busyIndicator.IsBusy = false;

                }
                catch (Exception ex)
                {
                    busyIndicator.IsBusy = false;
                    stream?.Dispose();
                    UnicontaMessageBox.Show(ex);
                }

                var msgTxt = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Exported"), countOk);
                if (countErr > 0)
                    msgTxt = string.Concat(msgTxt, string.Format("\n{0}: {1}", Uniconta.ClientTools.Localization.lookup("Error"), countErr));
                UnicontaMessageBox.Show(msgTxt, Uniconta.ClientTools.Localization.lookup("Message"));
            }
            else
            {
                UnicontaMessageBox.Show(string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Exported"), countOk), Uniconta.ClientTools.Localization.lookup("Message"));
            }
        }

        int ExportDataGrid(Stream stream, UnicontaBaseEntity[] corasauBaseEntity, Dictionary<string, int> mappedItems)
        {
            Type RecordType;
            if (corasauBaseEntity.Length > 0)
                RecordType = corasauBaseEntity[0].GetType();
            else
                return 0;

            var Props = new List<PropertyInfo>();
            var Headers = new List<string>();
            var sortedItems = (from l in mappedItems where l.Value > 0 orderby l.Value ascending select l).ToList();
            foreach (var strKey in sortedItems)
            {
                var pInfo = RecordType.GetProperty(strKey.Key);
                if (pInfo != null)
                {
                    Headers.Add(UtilFunctions.GetDisplayNameFromPropertyInfo(pInfo));
                    Props.Add(pInfo);
                }
            }
            int cnt = 0;
#if !SILVERLIGHT
            var writer = new StreamWriter(stream, Encoding.Default);
            cnt = CSVHelper.ExportDataGridToExcel(stream, Headers, corasauBaseEntity, Props, spreadSheet, ".xlsx", "Intrastat");
            writer.Flush();
#endif
            return cnt;
        }

        public async void GetInvoiceLinesToIntraStat(DateTime fromDate, DateTime toDate, bool includeImport, bool includeExport)
        {
            var prop = PropValuePair.GenereteWhereElements("Item", typeof(string), "!null");
            List<PropValuePair> propValPair = new List<PropValuePair>() { prop };

            if (fromDate != DateTime.MinValue || toDate != DateTime.MinValue)
            {
                string filter;
                if (fromDate != DateTime.MinValue)
                    filter = String.Format("{0:d}..", fromDate);
                else
                    filter = "..";
                if (toDate != DateTime.MinValue)
                    filter += String.Format("{0:d}", toDate);
                prop = PropValuePair.GenereteWhereElements("Date", typeof(DateTime), filter);
                propValPair.Add(prop);
            }

            var intraList = new List<IntrastatClient>();
           
            if (includeImport)
            {
                prop = PropValuePair.GenereteWhereElements("MovementType", typeof(int), "2");
                propValPair.Insert(0, prop);
                var listIntraStatCred = await api.Query<IntrastatClient>(propValPair);
                var listCred = UpdateValues(listIntraStatCred, ImportOrExportIntrastat.Import, 1);
                propValPair.RemoveAt(0); // remove it so we can reuse the list

                if (listCred != null || listCred?.Count > 0)
                    intraList.AddRange(listCred);
            }
            if (includeExport)
            {
                prop = PropValuePair.GenereteWhereElements("MovementType", typeof(int), "1");
                propValPair.Add(prop);
                var listIntraStatDeb = await api.Query<IntrastatClient>(propValPair);
                var listDeb = UpdateValues(listIntraStatDeb, ImportOrExportIntrastat.Export, -1);

                if (listDeb != null || listDeb?.Count > 0)
                    intraList.AddRange(listDeb);
            }

            if (intraList.Count <= 0)
                UnicontaMessageBox.Show(Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Warning"));

            dgIntraStatGrid.ItemsSource = intraList;
            dgIntraStatGrid.Visibility = Visibility.Visible;
        }

        List<IntrastatClient> UpdateValues(IntrastatClient[] listIntraStat, ImportOrExportIntrastat importOrExport, double factor)
        {
            if (listIntraStat == null || listIntraStat.Length == 0)
                return null;

            var intraList = new List<IntrastatClient>(listIntraStat.Length >> 4);  // div 8
            var companyCountry = api.CompanyEntity._CountryId;
            var companyRegNo = Regex.Replace(api.CompanyEntity._Id ?? string.Empty, "[^0-9]", "");

            foreach (var intraStat in listIntraStat)
            {
                intraStat.ImportOrExport = importOrExport;

                if (intraStat._NetAmount() == 0 || intraStat._Subtotal || intraStat._PartOfBOM || intraStat._DCAccount == null || intraStat._Item == null)
                    continue;

                var item = intraStat.InvItem;
                if (item == null)
                    continue;

                if (item?._ItemType == (byte)ItemType.Service)
                    continue;

                if (importOrExport == ImportOrExportIntrastat.Import)
                    intraStat.Country = intraStat.Creditor.Country;
                else // export
                {
                    var deb = intraStat.Debtor;
                    intraStat.Country = deb._DeliveryCountry == 0 ? deb._Country : deb._DeliveryCountry;
                }

                if (intraStat.Country == companyCountry)
                    continue;

                if (!Country2Language.IsEU(intraStat.Country) && intraStat.Country != CountryCode.Unknown)
                    continue;
                
                intraStat.DebtorRegNo = intraStat?.Debtor?._LegalIdent;

                var fdebtorCVR = intraStat.DebtorRegNo;
                if (fdebtorCVR != null)
                {
                    long value;
                    if (!long.TryParse(fdebtorCVR, out value))
                        fdebtorCVR = Regex.Replace(fdebtorCVR, @"[-/ ]", "");

                    var preCtry = StringBuilderReuse.Create().Append(fdebtorCVR).Truncate(2).ToStringAndRelease();
                    var pCountry = intraStat.PartnerCountry;
                    if (preCtry != pCountry)
                        fdebtorCVR = StringBuilderReuse.Create().Append(pCountry).Append(fdebtorCVR).ToStringAndRelease();

                    intraStat.fDebtorRegNo = fdebtorCVR;
                }
                else
                {
                    intraStat.DebtorRegNo = IntraHelper.UNKNOWN_CVRNO;
                    intraStat.fDebtorRegNo = IntraHelper.UNKNOWN_CVRNO;
                }


                intraStat.CompanyRegNo = companyRegNo;
                var salesAmount = intraStat._NetAmount() * factor;
                intraStat.TransType = salesAmount < 0 ? "21" : "11";
                intraStat.InvAmount = Math.Abs(salesAmount);

                intraStat.InvoiceQuantity = Math.Abs(intraStat._Qty);
                intraStat.CountryOfOrigin = item._CountryOfOrigin == CountryCode.Unknown ? item.InventoryGroup._CountryOfOrigin : item._CountryOfOrigin;

                intraStat.IntraUnitPerPCS = Math.Abs(item._IntraUnit);
                intraStat.WeightPerPCS = Math.Abs(item.Weight);
                intraStat.NetWeight = intraStat.WeightPerPCS * intraStat.InvoiceQuantity;
                intraStat.IntraUnit = item._IntraUnit * (int)Math.Round(intraStat.InvoiceQuantity,0);

                switch (item._Unit)
                {
                    case ItemUnit.Gram:
                        intraStat.WeightPerPCS /= 1000;
                        intraStat.NetWeight /= 1000;
                        break;
                    case ItemUnit.Milligram:
                        intraStat.WeightPerPCS /= 1000000;
                        intraStat.NetWeight /= 1000000;
                        break;
                }

                intraStat.Date = intraStat.Date == DateTime.MinValue ? DateTime.Now : intraStat.Date;
                
                var itmCod = item._TariffNumber ?? item.InventoryGroup?._TariffNumber;
                if (itmCod != null)
                {
                    itmCod = Regex.Replace(itmCod, "[^0-9]", "");
                    itmCod = itmCod != null && itmCod.Length > 8 ? itmCod.Substring(0, 8) : itmCod;
                    intraStat.ItemCode = itmCod;
                }

                intraList.Add(intraStat);
            }
            return intraList;
        }

        private bool CallPrevalidate()
        {
            return intraHelper.PreValidate();
        }

        private IEnumerable<IntrastatClient> CallValidate(bool onlyValidate)
        {
            if (!CallPrevalidate())
                return null;

            dgIntraStatGrid.Columns.GetColumnByName("SystemInfo").Visible = true;

            var intralst = (IEnumerable<IntrastatClient>)dgIntraStatGrid.GetVisibleRows();
            intraHelper.Validate(intralst, compressed, onlyValidate);

            if (onlyValidate)
            {
                var countErr = intralst.Count(s => s.SystemInfo != IntraHelper.VALIDATE_OK);
                if (countErr == 0)
#if SILVERLIGHT
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ValidateNoError"), Uniconta.ClientTools.Localization.lookup("Validate"), MessageBoxButton.OK);
#else
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ValidateNoError"), Uniconta.ClientTools.Localization.lookup("Validate"), MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                else
#if SILVERLIGHT
                    UnicontaMessageBox.Show(string.Format("{0} {1}", countErr, Localization.lookup("JournalFailedValidation")), Localization.lookup("Validate"), MessageBoxButton.OK);
#else
                      UnicontaMessageBox.Show(string.Format("{0} {1}", countErr, Localization.lookup("JournalFailedValidation")), Localization.lookup("Validate"), MessageBoxButton.OK, MessageBoxImage.Warning);
#endif
            }

            return intralst;
        }

        private void btnSearch()
        {
            DCAccount.Visible = true; 
            AccountName.Visible = true;
            Item.Visible = true;
            ItemName.Visible = true;
            DebtorRegNo.Visible = checkExport.IsChecked.Value;
            fDebtorRegNo.Visible = checkExport.IsChecked.Value;
            InvoiceNumber.Visible = true;
            WeightPerPCS.Visible = true;
            IntraUnitPerPCS.Visible = true;
            Compressed.Visible = false;
            SystemInfo.Visible = true;
            Date.Visible = true;

            InvoiceQuantity.ReadOnly = false;
            NetWeight.ReadOnly = false;

            compressed = false;

            DefaultFromDate = txtDateFrm.DateTime;
            DefaultToDate = txtDateTo.DateTime;

            DefaultImp = checkImport.IsChecked.Value;
            DefaultExp = checkExport.IsChecked.Value;

            GetInvoiceLinesToIntraStat(txtDateFrm.DateTime, txtDateTo.DateTime, checkImport.IsChecked.Value, checkExport.IsChecked.Value);
        }

        public override bool HandledOnClearFilter()
        {
            btnSearch();
            return true;
        }

        public enum ImportOrExportIntrastat
        {
            Import = 1,
            Export = 2
        };

    }

    public class IntrastatClassText
    {
        public static string CompanyRegNo { get { return Localization.lookup("VAT"); } }
        public static string RecNr { get { return Localization.lookup("Record"); } }
        public static string IntraField { get { return Localization.lookup("Field"); } }
        public static string Filler { get { return Localization.lookup("Filler"); } }  
        public static string TransType { get { return Localization.lookup("TransferType"); } }
        public static string ItemAmount { get { return Localization.lookup("Quantity"); } }
        public static string InternalRefNo { get { return Localization.lookup("ReferenceNr"); } }
        public static string ImportOrExport { get { return Localization.lookup("Direction"); } }
        public static string Date { get { return Localization.lookup("Date"); } }
        public static string Period { get { return Localization.lookup("Period"); } }
        public static string ItemCount { get { return Localization.lookup("ItemPostNr"); } }
        public static string Country { get { return Localization.lookup("Country"); } }
        public static string CountryOfOriginUNK { get { return string.Concat(Localization.lookup("CountryOfOrigin"), " (", Localization.lookup("Unknown").ToLower(),")"); } }
        public static string DebtorRegNo { get { return Localization.lookup("CompanyRegNo"); } }
        public static string fDebtorRegNo { get { return string.Concat(Localization.lookup("CompanyRegNo"), " (", Localization.lookup("File").ToLower(), ")"); } }
        public static string Zeroes { get { return Localization.lookup("ContainZero"); } }
        public static string ItemCode { get { return Localization.lookup("TariffNumber"); } }
        public static string WeightPerPCS { get { return Localization.lookup("Weight") + "/" + Uniconta.ClientTools.Localization.lookup("Pcs"); } }
        public static string IntraUnitPerPCS { get { return Localization.lookup("SupplementaryUnits") + "/" + Uniconta.ClientTools.Localization.lookup("Pcs"); } }
        public static string NetWeight { get { return Localization.lookup("Weight"); } }
        public static string InvAmount { get { return Localization.lookup("InvoiceAmount"); } }
        public static string SumInvoiceAmount { get { return Localization.lookup("SumInvoiceAmount"); } }
        public static string IsTriangularTrade { get { return Localization.lookup("TriangleTrade"); } }
        public static string SystemInfo { get { return Localization.lookup("SystemInfo"); } }
        public static string Compressed { get { return Uniconta.ClientTools.Localization.lookup("Compressed"); } }
    }

    [ClientTableAttribute(LabelKey = "Intrastat")]
    public class IntrastatClient : InvTransClient
    {
        private string _CompanyRegNo;
        [Display(Name = "CompanyRegNo", ResourceType = typeof(IntrastatClassText))]
        public string CompanyRegNo { get { return _CompanyRegNo; } set { _CompanyRegNo = value; NotifyPropertyChanged("CompanyRegNo"); } }

        private string _TransType;
        [Display(Name = "TransType", ResourceType = typeof(IntrastatClassText))]
        public string TransType { get { return _TransType; } set { _TransType = value; NotifyPropertyChanged("TransType"); } }

        private string _InternalRefNo;
        [Display(Name = "InternalRefNo", ResourceType = typeof(IntrastatClassText))]
        public string InternalRefNo { get { return _InternalRefNo; } set { _InternalRefNo = value; NotifyPropertyChanged("InterntRefNo"); } }

        private ImportOrExportIntrastat _ImportOrExport;
        [Display(Name = "ImportOrExport", ResourceType = typeof(IntrastatClassText)),]
        public ImportOrExportIntrastat ImportOrExport { get { return _ImportOrExport; } set { _ImportOrExport = value; NotifyPropertyChanged("ImportOrExport"); } }
        public int ImportExport { get { return (byte)ImportOrExport; } }

        private CountryCode _Country;
        [Display(Name = "Country", ResourceType = typeof(IntrastatClassText))]
        public CountryCode Country { get { return _Country; } set { _Country = value; NotifyPropertyChanged("Country"); } }
        public string PartnerCountry 
        { 
            get 
            {
                return _Country == CountryCode.UnitedKingdom ? "XU" :
                       _Country == CountryCode.Greece ? "EL" : ((CountryISOCode)_Country).ToString();

            } 
        }

        private CountryCode _CountryOfOrigin;
        [Display(Name = "CountryOfOrigin", ResourceType = typeof(InventoryText))]
        public CountryCode CountryOfOrigin 
        { 
            get { return _CountryOfOrigin; } 
            set 
            { 
                _CountryOfOrigin = value; 
                NotifyPropertyChanged("CountryOfOrigin");

                if (CountryOfOrigin != CountryCode.Unknown)
                {
                    _CountryOfOriginUNK = IntraUnknownCountry.None;
                    NotifyPropertyChanged("CountryOfOriginUNK");
                }
            }
        }
        public string fCountryOfOrigin 
        { 
            get 
            {
                if (CountryOfOrigin == CountryCode.Unknown)
                    return _CountryOfOriginUNK == IntraUnknownCountry.EUCountry ? "QV" :
                           _CountryOfOriginUNK == IntraUnknownCountry.ThirdCountry ? "QW" : "";
                else
                    return CountryOfOrigin == CountryCode.UnitedKingdom ? "XU" :
                           CountryOfOrigin == CountryCode.Greece ? "EL" : ((CountryISOCode)CountryOfOrigin).ToString();
            } 
        }

        public IntraUnknownCountry _CountryOfOriginUNK;
        [AppEnumAttribute(EnumName = "IntraUnknownCountry")]
        [Display(Name = "CountryOfOriginUNK", ResourceType = typeof(IntrastatClassText))]
        public string CountryOfOriginUNK
        {
            get { return AppEnums.IntraUnknownCountry.ToString((int)_CountryOfOriginUNK); }
            set
            {
                if (value == null) return;
                var _val = (IntraUnknownCountry)AppEnums.IntraUnknownCountry.IndexOf(value);
                if (_val != _CountryOfOriginUNK)
                {
                    _CountryOfOriginUNK = _val;
                    NotifyPropertyChanged("CountryOfOriginUNK");
                }
            }
        }

        private string _DebtorRegNo;
        [Display(Name = "DebtorRegNo", ResourceType = typeof(IntrastatClassText))]
        public string DebtorRegNo { get { return _DebtorRegNo; } set { _DebtorRegNo = value; NotifyPropertyChanged("DebtorRegNo"); } }


        public string _fDebtorRegNo;
        [Display(Name = "fDebtorRegNo", ResourceType = typeof(IntrastatClassText))]
        public string fDebtorRegNo { get { return _fDebtorRegNo; } set { _fDebtorRegNo = value; NotifyPropertyChanged("fDebtorRegNo"); } }

        private string _ItemCode;
        [Display(Name = "ItemCode", ResourceType = typeof(IntrastatClassText))]
        public string ItemCode { get { return _ItemCode; } set { _ItemCode = value; NotifyPropertyChanged("ItemCode"); } }

        private double _WeightPerPCS;
        [Price]
        [Display(Name = "WeightPerPCS", ResourceType = typeof(IntrastatClassText))]
        public double WeightPerPCS { get { return _WeightPerPCS; } set { _WeightPerPCS = value; NotifyPropertyChanged("WeightPerPCS"); } }

        private int _IntraUnitPerPCS;
        [Display(Name = "IntraUnitPerPCS", ResourceType = typeof(IntrastatClassText))]
        public int IntraUnitPerPCS { get { return _IntraUnitPerPCS; } set { _IntraUnitPerPCS = value; NotifyPropertyChanged("IntraUnitPerPCS"); } }

        private double _InvoiceQuantity;
        [Price]
        [Display(Name = "Qty", ResourceType = typeof(DCOrderText))]
        public double InvoiceQuantity { get { return _InvoiceQuantity; } set { _InvoiceQuantity = value; NotifyPropertyChanged("InvoiceQuantity"); } }

        private double _NetWeight;
        [Price]
        [Display(Name = "NetWeight", ResourceType = typeof(IntrastatClassText))]
        public double NetWeight
        {
            get
            {
                return _Compressed ? _NetWeight : WeightPerPCS * (InvoiceQuantity == 0 ? 1 : InvoiceQuantity);
            }
            set { _NetWeight = value; NotifyPropertyChanged("NetWeight"); }
        }

        private int _IntraUnit;
        [Display(Name = "IntraUnit", ResourceType = typeof(InventoryText))] 
        public int IntraUnit 
        { 
            get 
            {
                var qtyInv = InvoiceQuantity == 0 ? 1 : InvoiceQuantity;
                return _Compressed ? _IntraUnit : IntraUnitPerPCS * (int)Math.Round(qtyInv, 0); 
            } 
            set { _IntraUnit = value; NotifyPropertyChanged("IntraUnit"); }
        }

        private double _InvAmount;
        [Display(Name = "InvAmount", ResourceType = typeof(IntrastatClassText))]
        public double InvAmount { get { return _InvAmount; } set { _InvAmount = value; NotifyPropertyChanged("InvAmount"); } }
        public long fInvAmount { get { return (long)Math.Round(_InvAmount, 0); } }

        private bool _IsTriangularTrade;
        [Display(Name = "IsTriangularTrade", ResourceType = typeof(IntrastatClassText))]
        public bool IsTriangularTrade { get { return _IsTriangularTrade; } set { _IsTriangularTrade = value; NotifyPropertyChanged("IsTriangularTrade"); } }

        private string _SystemInfo;
        [Display(Name = "SystemInfo", ResourceType = typeof(IntrastatClassText))]
        public string SystemInfo { get { return _SystemInfo; } set { _SystemInfo = value; NotifyPropertyChanged("SystemInfo");} }

        [Display(Name = "Period", ResourceType = typeof(IntrastatClassText))]
        public string Period { get { return Date.ToString("yyyyMM"); } }

        private bool _Compressed;
        [Display(Name = "Compressed", ResourceType = typeof(IntrastatClassText))]
        public bool Compressed { get { return _Compressed; } set { _Compressed = value; NotifyPropertyChanged("Compressed"); } }
    }

    public enum IntraUnknownCountry : byte
    {
        None,
        EUCountry,
        ThirdCountry
    };
}
