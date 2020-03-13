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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            RemoveMenuItem();
            localMenu.dataGrid = dgIntraStatGrid;
            SetRibbonControl(localMenu, dgIntraStatGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgIntraStatGrid.api = api;
            dgIntraStatGrid.BusyIndicator = busyIndicator;
            txtDateFrm.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 01);
            txtDateTo.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            dgIntraStatGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.InvGroup) });
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as IntrastatClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

            var selectedItem = e.NewItem as IntrastatClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
        }

        bool noChange = false;

        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var selectedItem = dgIntraStatGrid.SelectedItem as IntrastatClient;

            switch (e.PropertyName)
            {
                case "WeightPerPCS":
                case "InvoiceQuantity":
                    if (noChange)
                        break;

                    if (selectedItem.weightPerPcs != 0 && selectedItem.invoiceQuantity != 0)
                    {
                        noChange = true;
                        selectedItem.NetWeight = selectedItem.WeightPerPCS * selectedItem.InvoiceQuantity;
                        noChange = false;

                    }
                    else if (selectedItem.invoiceQuantity == 0)
                    {
                        noChange = true;
                        selectedItem.NetWeight = selectedItem.WeightPerPCS;
                        noChange = false;

                    }
                    break;
                case "ItemIntra":
                    if (string.IsNullOrWhiteSpace(selectedItem.ItemIntra))
                        selectedItem.ItemNameIntra = null;
                    else
                        selectedItem.ItemNameIntra = selectedItem.InvItem?.Name;
                    break;
                case "NetWeight":
                    if (noChange)
                        break;

                    noChange = true;
                    selectedItem.WeightPerPCS = 0;
                    selectedItem.InvoiceQuantity = 0;
                    noChange = false;
                    break;
                default:
                    selectedItem.systemInfo = null;
                    break;
            }

            if (!noChange)
            {
                var prop = selectedItem.GetType().GetProperty(e.PropertyName);

                var val = prop.GetValue(selectedItem, null);
                if (val != null && val is double)
                {
                    noChange = true;
                    val = Math.Abs((double)val);
                    prop.SetValue(selectedItem, (double)val, null);
                    noChange = false;
                }
            }
            selectedItem.systemInfo = null;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var listIntraStat = dgIntraStatGrid.GetVisibleRows() as IEnumerable<IntrastatClient>;
            var selectedItem = dgIntraStatGrid.SelectedItem as IntrastatClient;
            List<IntrastatClient> listOfFiles = null;
            switch (ActionType)
            {
                case "AddRow":
                    var row = dgIntraStatGrid.AddRow() as IntrastatClient;
                    if (row != null)
                    {
                        row.SetCompany(api.CompanyId);
                        row.recNr = "03";
                        row.zeroes = 0;
                        row.filler = new string(' ', 9);
                        row.monthAndYearOfDate = txtDateTo.DateTime;
                        row.euCountry = EUCountries.Unknown;
                    }
                    break;

                case "DeleteRow":
                    if (selectedItem != null)
                        dgIntraStatGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    break;
                case "Compress":
                    if (listIntraStat != null)
                        try
                        {
                            listOfFiles = CompressIntraStatItems(listIntraStat.ToList());

                            if (listOfFiles != null && listOfFiles.Count > 0 && listOfFiles.Count < listIntraStat.Count())
                            {
                                DCAccount.Visible = false;
                                AccountName.Visible = false;
                                ItemIntra.Visible = false;
                                ItemNameIntra.Visible = false;
                                dgIntraStatGrid.ItemsSource = listOfFiles;
                                dgIntraStatGrid.Visibility = Visibility.Visible;
                            }
                        }
                        catch (Exception e)
                        {
                            UnicontaMessageBox.Show(e, Uniconta.ClientTools.Localization.lookup("Exception"));
                            throw;
                        }
                    break;
                case "Validate":
                    if (listIntraStat != null)
                    {
                        listOfFiles = ValidateIntraStatItems(listIntraStat.ToList(), true, false);
                        if (listOfFiles == null && listOfFiles.Count <= 0)
                        {
                            UnicontaMessageBox.Show(Localization.lookup("ValidateFail"), Uniconta.ClientTools.Localization.lookup("Error"));
                            break;
                        }
                    }
                    break;
                case "ImportFile":
#if SILVERLIGHT
                    var sfd = new System.Windows.Controls.SaveFileDialog
                    {
                        Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.TXT)
                    };
                    var userClickedSave = sfd.ShowDialog();
#else
                    var userClickedSave = true;
#endif
                    if (listIntraStat != null)
                    {
                        try
                        {

                            var result = ValidateIntraStatItems(listIntraStat.ToList(), false, false);

                            if (result != null && result.Count > 0)
                            {

                                if (userClickedSave != true)
                                    listOfFiles = result;
                                else
                                {
#if SILVERLIGHT
                                    listOfFiles = CreateFileForIntra.CreateIntraStatfile(result, api, sfd);
#else
                                    listOfFiles = CreateFileForIntra.CreateIntraStatfile(result, api);
#endif
                                }
                            }
                            else
                            {
                                UnicontaMessageBox.Show(Localization.lookup("ValidateFail"), Uniconta.ClientTools.Localization.lookup("Error"));
                                break;
                            }
                            if (listOfFiles != null && listOfFiles.Count > 0)
                            {
                                dgIntraStatGrid.ItemsSource = listOfFiles;
                                dgIntraStatGrid.Visibility = Visibility.Visible;
                                var msg = string.Format("{0}. {1}", string.Format(Uniconta.ClientTools.Localization.lookup("MaxLines"), (999).ToString()), Uniconta.ClientTools.Localization.lookup("RepeatProcess"));
                                UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                            }
                        }
                        catch (Exception e)
                        {
                            UnicontaMessageBox.Show(e, Uniconta.ClientTools.Localization.lookup("Exception"));
                            throw;
                        }
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
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

        List<IntrastatClient> UpdateValues(IEnumerable<IntrastatClient> listIntraStat, CreateIntraStatFilePage.ImportOrExportIntrastat importOrExport, double factor)
        {
            if (listIntraStat == null || listIntraStat.Count() <= 0)
                return null;

            var intraList = new List<IntrastatClient>();
            var companyCountry = api.CompanyEntity._CountryId;

            foreach (var intraStat in listIntraStat)
            {
                intraStat.importOrExport = importOrExport;

                if (intraStat._DCAccount == null || intraStat._Item == null)
                    continue;

                var item = intraStat.InvItem;
                if (item == null)
                    continue;

                if (item?._ItemType == (byte)ItemType.Service)
                    continue;

                CountryCode country;

                if (importOrExport == CreateIntraStatFilePage.ImportOrExportIntrastat.Import)
                    country = intraStat.Creditor.Country;
                else // export
                {
                    var deb = intraStat.Debtor;
                    country = deb._DeliveryCountry == 0 ? deb._Country : deb._DeliveryCountry; //TODO: FAR skal lave en reference til DeliveryAccount på InvTrans
                }

                if (country == companyCountry)
                    continue;

                if (Country2Language.IsEU(country))
                    intraStat.euCountry = (EUCountries)Enum.Parse(typeof(EUCountries), country.ToString(), true);
                else if (country == CountryCode.Unknown)
                    intraStat.euCountry = EUCountries.Unknown;
                else
                    continue;

                intraStat.recNr = "03";
                intraStat.itemIntra = item._Item;
                intraStat.itemNameIntra = item.Name;
                intraStat.zeroes = 0;
                intraStat.filler = new string(' ', 9);

                var salesAmount = intraStat._NetAmount() * factor;
                intraStat.transType = salesAmount < 0 ? "21" : "11";
                intraStat.invoiceAmount = Math.Abs(salesAmount);

                intraStat.invoiceQuantity = Math.Abs(intraStat._Qty);
                //if (intraStat.invoiceQuantity == 0)
                //    intraStat.invoiceQuantity = 1;

                intraStat.weightPerPcs = Math.Abs(item.Weight);
                intraStat.netWeight = intraStat.weightPerPcs * intraStat.invoiceQuantity;

                switch (item._Unit)
                {
                    case ItemUnit.Gram:
                        intraStat.weightPerPcs /= 1000;
                        intraStat.netWeight /= 1000;
                        break;
                    case ItemUnit.Milligram:
                        intraStat.weightPerPcs /= 1000000;
                        intraStat.netWeight /= 1000000;
                        break;
                }

                intraStat.monthAndYearOfDate = intraStat.Date == DateTime.MinValue ? DateTime.Now : intraStat.Date;
                
                var itmCod = item._TariffNumber ?? item.InventoryGroup?._TariffNumber;
                if (itmCod != null)
                {
                    itmCod = Regex.Replace(itmCod, "[^0-9]", "");
                    itmCod = itmCod != null && itmCod.Length > 8 ? itmCod.Substring(0, 8) : itmCod;
                    intraStat.itemCode = itmCod;
                }

                intraList.Add(intraStat);
            }
            return intraList;
        }

        public List<IntrastatClient> ValidateIntraStatItems(List<IntrastatClient> listOfIntraStat, bool isOnlyValidate, bool isCompress)
        {
            if (!Country2Language.IsEU(api.CompanyEntity._CountryId))
            {
                UnicontaMessageBox.Show(Localization.lookup("AccountCountryNotEu"), Uniconta.ClientTools.Localization.lookup("Error"));
                return null;
            }

            var companyCountry = (EUCountries)Enum.Parse(typeof(EUCountries), api.CompanyEntity._CountryId.ToString(), true);
            var notValidated = new List<IntrastatClient>();
            var ikkeMedtaget = 0;

            foreach (var intra in listOfIntraStat)
            {
                var hasErrors = false;                
                intra.systemInfo = "";

                if (intra.IsTriangularTrade == true)
                {
                    intra.systemInfo = intra.systemInfo + Localization.lookup("TriangularTrade");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(intra.itemCode))
                {
                    hasErrors = true;
                    intra.systemInfo = intra.systemInfo + string.Format(Localization.lookup("OBJisEmpty"), Localization.lookup("CommodityCode")) + ".\n";
                }
                else
                    intra.itemCode = Regex.Replace(intra.itemCode, "[^0-9]", "");

                if (!string.IsNullOrWhiteSpace(intra.itemCode) && intra.itemCode.Length != 8)
                {
                    hasErrors = true;
                    intra.systemInfo = intra.systemInfo + string.Format(Localization.lookup("InvalidValue"), Localization.lookup("CommodityCode"), intra.itemCode) + ".\n";
                }          
                if (intra.EUCountry == EUCountries.Unknown)
                {
                    hasErrors = true;
                    intra.systemInfo = intra.systemInfo + Localization.lookup("CountryNotSet") + ".\n";
                }
                if (intra.EUCountry == companyCountry)
                {
                    hasErrors = true;
                    intra.systemInfo = intra.systemInfo + Localization.lookup("OwnCountryProblem") + ".\n";
                }
                if (intra.invoiceAmount == 0)
                {
                    hasErrors = true;
                    intra.systemInfo = intra.systemInfo + string.Format(Localization.lookup("MissingOBJ"), Localization.lookup("ImportExport")) + ".\n";
                }
                if (intra.ImportOrExport == 0)
                {
                    hasErrors = true; 
                    intra.systemInfo = intra.systemInfo + Localization.lookup("MissingImportExport") + ".\n";
                }              
                if(intra.netWeight == 0 && intra.additionalAmount == 0 && (string.IsNullOrWhiteSpace(intra.itemCode) || !intra.itemCode.Contains("99500000")))
                {
                    hasErrors = true;
                    intra.systemInfo = intra.systemInfo + string.Format(Localization.lookup("MissingOBJ"), Localization.lookup("Weight")) + ".\n";
                }
                if (string.IsNullOrWhiteSpace(intra.transType))
                {
                    hasErrors = true;
                    intra.systemInfo = intra.systemInfo + Localization.lookup("EmptyTransferType") + ".\n";
                }
                if (!string.IsNullOrWhiteSpace(intra.transType) && intra.transType.Length != 2)
                {
                    hasErrors = true;
                    intra.systemInfo = intra.systemInfo + string.Format(Localization.lookup("InvalidValue"), Localization.lookup("TransferType"), intra.transType) + ".\n"; 
                }

                if (string.IsNullOrWhiteSpace(intra.systemInfo))
                {
                    hasErrors = false;
                    intra.systemInfo = Localization.lookup("Ok");
                }

                if (hasErrors && !intra.isTriangularTrade)
                    ikkeMedtaget++;
            }
            
            if (ikkeMedtaget <= 0)
            {
                if (!isCompress)
                    listOfIntraStat = CheckListIsTooLong(listOfIntraStat);

                dgIntraStatGrid.ItemsSource = listOfIntraStat;
                dgIntraStatGrid.Visibility = Visibility.Visible;
                return listOfIntraStat;
            }

            if (!isCompress)
                listOfIntraStat = CheckListIsTooLong(listOfIntraStat);

            if (isOnlyValidate)
            {
                UnicontaMessageBox.Show(string.Format(Localization.lookup("ValidateFailInLines"), ikkeMedtaget), Uniconta.ClientTools.Localization.lookup("Error"));
            }
            else if(!isCompress && !isOnlyValidate)
            {
                var messageOfNotIncluded = string.Format(Localization.lookup("SkipNotValidatedLines"), ikkeMedtaget);
#if !SILVERLIGHT
                var result = UnicontaMessageBox.Show(messageOfNotIncluded, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.YesNo);
                if (MessageBoxResult.No == result)
                    return null;
#else
                var result = UnicontaMessageBox.Show(messageOfNotIncluded, Uniconta.ClientTools.Localization.lookup("Problem"), MessageBoxButton.OKCancel);
                 if (MessageBoxResult.Cancel == result)
                    return null;
#endif
            }
            dgIntraStatGrid.ItemsSource = listOfIntraStat;
            dgIntraStatGrid.Visibility = Visibility.Visible;
            
            return listOfIntraStat.Count > 0 ? listOfIntraStat : null;
        }

        public List<IntrastatClient> CheckListIsTooLong(List<IntrastatClient> listOfIntraStat)
        {
            var compList = new List<IntrastatClient>();

            var expCount = listOfIntraStat.Where(a => a.ImportOrExport == ImportOrExportIntrastat.Export).Count();
            var impCount = listOfIntraStat.Where(a => a.ImportOrExport == ImportOrExportIntrastat.Import).Count();

            if (expCount > 999 || impCount > 999)
            {
                var messageComp = string.Format(Localization.lookup("MaxLines") + ". " + Localization.lookup("Compress") + "?", (999).ToString());
#if !SILVERLIGHT
                var result = UnicontaMessageBox.Show(messageComp, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
#else
                var result = UnicontaMessageBox.Show(messageComp, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
#endif
                {
                    compList = CompressIntraStatItems(listOfIntraStat);
                    DCAccount.Visible = false;
                    AccountName.Visible = false;
                    ItemIntra.Visible = false;
                    ItemNameIntra.Visible = false;
                }
                else
                {
                    compList = listOfIntraStat;
                }
            }
            else
            {
                compList = listOfIntraStat;
            }
            return compList;
        }

        class CompressCompare : IEqualityComparer<IntrastatClient>
        {
            public bool Equals(IntrastatClient x, IntrastatClient y)
            {
                int c = x.euCountry - y.euCountry;
                if (c != 0)
                    return false;
                c = x.importOrExport - y.importOrExport;
                if (c != 0)
                    return false;
                c = string.Compare(x.itemCode, y.itemCode);
                if (c != 0)
                    return false;
                c = string.Compare(x.transType, y.transType);
                if (c != 0)
                    return false;
                if (x.isTriangularTrade != y.isTriangularTrade)
                    return false;
                return true;
            }
            public int GetHashCode(IntrastatClient x)
            {
                return (int)(x.euCountry + 1) * ((int)x.importOrExport + 1) * Util.GetHashCode(x.itemCode);
            }
        }

        public List<IntrastatClient> CompressIntraStatItems(List<IntrastatClient> listOfIntraStat)
        {
            var validatedList = ValidateIntraStatItems(listOfIntraStat, false, true);
            var resultList = new List<IntrastatClient>();

            if (validatedList == null || validatedList.Count <= 0)
                resultList = listOfIntraStat;
            else
                resultList = validatedList;
            var okLoc = Localization.lookup("Ok");
            var compLoc = Localization.lookup("Compressed");

            var cmp = new CompressCompare();
            var compressedIntraStat = new Dictionary<IntrastatClient, IntrastatClient>(cmp);
            var doesCompress = false;

            List<IntrastatClient> noOk = new List<IntrastatClient>();
            foreach (var rec in resultList)
            {       
                var valueMatch = (CountryCode)Enum.Parse(typeof(CountryCode), rec.EUCountry.ToString(), true);

                if (rec.systemInfo == okLoc || rec.systemInfo == compLoc)
                {
                    IntrastatClient found;

                    if (compressedIntraStat.TryGetValue(rec, out found))
                    {
                        doesCompress = true;
                        found.invoiceAmount += rec.invoiceAmount;
                        found.netWeight += rec.netWeight;
                        found.invoiceQuantity += rec.invoiceQuantity;
                        found.additionalAmount += rec.additionalAmount;
                        found.monthAndYearOfDate = rec.monthAndYearOfDate;
                        found.weightPerPcs = 0;
                        found._InvoiceNumber = 0;
                        found.systemInfo = Localization.lookup("Compressed");
                    }
                    else
                        compressedIntraStat.Add(rec, rec);
                }
                else
                    noOk.Add(rec);
            }
            if (doesCompress == false)
                UnicontaMessageBox.Show(Localization.lookup("NoLinesFound"), Uniconta.ClientTools.Localization.lookup("Warning"));

            noOk.AddRange(compressedIntraStat.Values);
            return noOk;
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var Comp = api.CompanyEntity;
            if (!Comp.SerialBatchNumbers)
                UtilDisplay.RemoveMenuCommand(rb, "SeriesBatch" );
        }
        private void BtnSearch_OnClick(object sender, RoutedEventArgs e)
        {
            DCAccount.Visible = true;
            AccountName.Visible = true;
            ItemIntra.Visible = true;
            ItemNameIntra.Visible = true;
            GetInvoiceLinesToIntraStat(txtDateFrm.DateTime, txtDateTo.DateTime, checkImport.IsChecked.Value, checkExport.IsChecked.Value);
        }

        public override bool HandledOnClearFilter()
        {
            BtnSearch_OnClick(null, null);
            return true;
        }

        public enum EUCountries
        {
            Austria,
            Belgium,
            Bulgaria,
            Croatia,
            Cyprus,
            CzechRepublic,
            Denmark,
            Estonia,
            Finland,
            France,
            Germany,
            Greece,
            Hungary,
            Ireland,
            Italy,
            Latvia,
            Lithuania,
            Luxembourg,
            Malta,
            Monaco,
            Netherlands,
            Poland,
            Portugal,
            Romania,
            Slovakia,
            Slovenia,
            Spain,
            Sweden,
            UnitedKingdom,
            Unknown
        };

        public enum ImportOrExportIntrastat
        {
            Import = 1,
            Export = 2
        };
    }

    public class Intrastat : InvTransClient
    {
        public string recNr;
        public string itemIntra;
        public string itemNameIntra;
        public string companyRegNr;
        public string intraField;
        public string filler;
        public string transType;
        public string itemAmount;
        public string interntRefNrForAll;
        public string interntRefNr;
        public ImportOrExportIntrastat importOrExport;
        public DateTime monthAndYearOfDate;
        public int itemCount;
        public EUCountries euCountry;
        public int zeroes;
        public string itemCode;
        public double weightPerPcs;
        public double invoiceQuantity;
        public double netWeight;
        public double additionalAmount;
        public double invoiceAmount;
        public double suminvoiceAmount;
        public bool isTriangularTrade;
        public string systemInfo;

        public void SetCompany(int CompanyId)
        {
            _CompanyId = CompanyId;
        }
    }

    public class IntrastatClassText
    {
        public static string ItemIntra { get { return Uniconta.ClientTools.Localization.lookup("Item"); } }
        public static string ItemNameIntra { get { return Uniconta.ClientTools.Localization.lookup("ItemName"); } }
        public static string CompanyRegNr { get { return Uniconta.ClientTools.Localization.lookup("VAT"); } }
        public static string RecNr { get { return Uniconta.ClientTools.Localization.lookup("Record"); } }
        public static string IntraField { get { return Uniconta.ClientTools.Localization.lookup("Field"); } }
        public static string Filler { get { return Uniconta.ClientTools.Localization.lookup("Filler"); } }  
        public static string TransType { get { return Uniconta.ClientTools.Localization.lookup("TransferType"); } }
        public static string ItemAmount { get { return Uniconta.ClientTools.Localization.lookup("Quantity"); } }
        public static string InterntRefNrAll { get { return Uniconta.ClientTools.Localization.lookup("ReferenceNr"); } }
        public static string InterntRefNr { get { return Uniconta.ClientTools.Localization.lookup("ReferenceNr"); } }
        public static string ImportOrExport { get { return Uniconta.ClientTools.Localization.lookup("Direction"); } }
        public static string MonthAndYearOfDate { get { return Uniconta.ClientTools.Localization.lookup("Date"); } }
        public static string ItemCount { get { return Uniconta.ClientTools.Localization.lookup("ItemPostNr"); } }
        public static string EUCountry { get { return Uniconta.ClientTools.Localization.lookup("Country"); } }
        public static string Zeroes { get { return Uniconta.ClientTools.Localization.lookup("ContainZero"); } }
        public static string ItemCode { get { return Uniconta.ClientTools.Localization.lookup("CommodityCode"); } }
        public static string WeightPerPCS { get { return Uniconta.ClientTools.Localization.lookup("Weight") + "/" + Uniconta.ClientTools.Localization.lookup("Pcs"); } }
        public static string InvoiceQuantity { get { return Uniconta.ClientTools.Localization.lookup("Pcs"); } }
        public static string NetWeight { get { return Uniconta.ClientTools.Localization.lookup("Weight"); } }
        public static string AdditionalAmount { get { return Uniconta.ClientTools.Localization.lookup("SupplementaryUnits"); } }
        public static string InvoiceAmount { get { return Uniconta.ClientTools.Localization.lookup("InvoiceAmount"); } }
        public static string SumInvoiceAmount { get { return Uniconta.ClientTools.Localization.lookup("SumInvoiceAmount"); } }
        public static string IsTriangularTrade { get { return Uniconta.ClientTools.Localization.lookup("TriangularTrade"); } }
        public static string SystemInfo { get { return Uniconta.ClientTools.Localization.lookup("SystemInfo"); } }

    }

    [ClientTableAttribute(LabelKey = "Intrastat")]
    public class IntrastatClient : Intrastat, INotifyPropertyChanged
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvItem))]
        [Display(Name = "Item", ResourceType = typeof(InventoryText))]
        public string ItemIntra { get { return _Item; } set { _Item = value; NotifyPropertyChanged("ItemIntra"); } }

        [Display(Name = "CompanyRegNr", ResourceType = typeof(IntrastatClassText))]
        public string CompanyRegNr { get { return companyRegNr; } set { companyRegNr = value; NotifyPropertyChanged("CompanyRegNr"); } }

        [Display(Name = "TransType", ResourceType = typeof(IntrastatClassText))]
        public string TransType { get { return transType; } set { transType = value; NotifyPropertyChanged("TransType"); } }

        [Display(Name = "ItemNameIntra", ResourceType = typeof(IntrastatClassText))]
        public string ItemNameIntra { get { return itemNameIntra; } set { itemNameIntra = value; NotifyPropertyChanged("ItemNameIntra"); } }

        [Display(Name = "RecNr", ResourceType = typeof(IntrastatClassText))]
        public string RecNr { get { return recNr; } set { recNr = value; NotifyPropertyChanged("RecNr"); } }

        [Display(Name = "InterntRefNr", ResourceType = typeof(IntrastatClassText))]
        public string InterntRefNr { get { return interntRefNr; } set { interntRefNr = value; NotifyPropertyChanged("InterntRefNr"); } }

        [Display(Name = "ItemAmount", ResourceType = typeof(IntrastatClassText))]
        public string ItemAmount { get { return itemAmount; } set { itemAmount = value; NotifyPropertyChanged("ItemAmount"); } }

        [Display(Name = "InterntRefNrAll", ResourceType = typeof(IntrastatClassText))]
        public string InterntRefNrAll { get { return interntRefNrForAll; } set { interntRefNrForAll = value; NotifyPropertyChanged("InterntRefNrAll"); } }

        [Display(Name = "ImportOrExport", ResourceType = typeof(IntrastatClassText)),]
        public ImportOrExportIntrastat ImportOrExport { get { return importOrExport; } set { importOrExport = value; NotifyPropertyChanged("ImportOrExport"); } } 

        [Display(Name = "MonthAndYearOfDate", ResourceType = typeof(IntrastatClassText))]
        public DateTime MonthAndYearOfDate { get { return monthAndYearOfDate; } set { monthAndYearOfDate = value; NotifyPropertyChanged("MonthAndYearOfDate"); } }

        [Display(Name = "ItemCount", ResourceType = typeof(IntrastatClassText))]
        public int ItemCount { get { return itemCount; } set { itemCount = value; NotifyPropertyChanged("ItemCount"); } }

        [Display(Name = "EUCountry", ResourceType = typeof(IntrastatClassText))]
        public EUCountries EUCountry { get { return euCountry; } set { euCountry = value; NotifyPropertyChanged("EUCountry"); } }

        [Display(Name = "ItemCode", ResourceType = typeof(IntrastatClassText))]
        public string ItemCode { get { return itemCode; } set { itemCode = value; NotifyPropertyChanged("ItemCode"); } }

        [Price]
        [Display(Name = "WeightPerPCS", ResourceType = typeof(IntrastatClassText))]
        public double WeightPerPCS { get { return weightPerPcs; } set { weightPerPcs = value; NotifyPropertyChanged("WeightPerPCS"); } }

        [Price]
        [Display(Name = "InvoiceQuantity", ResourceType = typeof(IntrastatClassText))]
        public double InvoiceQuantity { get { return invoiceQuantity; } set { invoiceQuantity = value; NotifyPropertyChanged("InvoiceQuantity"); } }

        [Price]
        [Display(Name = "NetWeight", ResourceType = typeof(IntrastatClassText))]
        public double NetWeight { get { return netWeight; } set { netWeight = value; NotifyPropertyChanged("NetWeight"); } }

        [Price]
        [Display(Name = "AdditionalAmount", ResourceType = typeof(IntrastatClassText))]
        public double AdditionalAmount { get { return additionalAmount; } set { additionalAmount = value; NotifyPropertyChanged("AdditionalAmount"); } }

        [Price]
        [Display(Name = "InvoiceAmount", ResourceType = typeof(IntrastatClassText))]
        public double InvoiceAmount { get { return invoiceAmount; } set { invoiceAmount = value; NotifyPropertyChanged("InvoiceAmount"); } }

        [Display(Name = "IsTriangularTrade", ResourceType = typeof(IntrastatClassText))]
        public bool IsTriangularTrade { get { return isTriangularTrade; } set { isTriangularTrade = value; NotifyPropertyChanged("IsTriangularTrade"); } }

        [Display(Name = "SystemInfo", ResourceType = typeof(IntrastatClassText))]
        public string SystemInfo { get { return systemInfo; } set { systemInfo = value; NotifyPropertyChanged("SystemInfo");} } 
    }

    public class IntrastatClientSort : IComparer<IntrastatClient>, IComparer
    {
        static int Cmp(string x, string y)
        {
            if (x == null && y != null)
                return -1;
            if (x != null && y == null)
                return 1;
            return string.Compare(x, y);
        }
        public int Compare(IntrastatClient _x, IntrastatClient _y)
        {
            int c = Cmp(_x._Item, _y._Item);
            if (c != 0)
                return c;
            c = Cmp(_x.itemCode, _y.itemCode);
            if (c != 0)
                return c;
            if (_x.monthAndYearOfDate == DateTime.MinValue && _y.monthAndYearOfDate != DateTime.MinValue)
                return -1;
            if (_x.invoiceAmount < _y.invoiceAmount)
                return -1;

            return 0;
        }
        public int Compare(object _x, object _y) { return Compare((IntrastatClient)_x, (IntrastatClient)_y); }
    }
}
