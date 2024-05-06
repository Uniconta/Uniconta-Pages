using Uniconta.ClientTools.Page;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Models;
using Uniconta.Common;
using Uniconta.API.GeneralLedger;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using Uniconta.Common.Utility;
using System.Collections;
using Uniconta.Client.Pages;
using UnicontaClient.Pages.Reports;
using Localization = Uniconta.ClientTools.Localization;
using System.Windows.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VatSettlementsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLVatReportedClient); } }
        public override IComparer GridSorting => new Sort();
        class Sort : IComparer
        {
            public int Compare(object x, object y)
            {
                return DateTime.Compare(((GLVatReported)y)._ToDate, ((GLVatReported)x)._ToDate);
            }
        }
    }

    public partial class VatSettlements : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.VatSettlements; } }

        public VatSettlements(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            this.DataContext = this;
            dgVatVatSettlements.api = api;
            SetRibbonControl(localMenu, dgVatVatSettlements);
            localMenu.dataGrid = dgVatVatSettlements;
            gridControl.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var countryId = api.CompanyEntity._CountryId;
            if (countryId != CountryCode.Denmark && countryId != CountryCode.Greenland && countryId != CountryCode.FaroeIslands)
            {
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, "ApproveVAT");
                if (countryId != CountryCode.Iceland)
                {
                    UtilDisplay.RemoveMenuCommand(rb, "Upload");
                    UtilDisplay.RemoveMenuCommand(rb, "View");
                }
            }
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new[] { typeof(Uniconta.DataModel.GLVatType), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLAccount) });
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgVatVatSettlements.SelectedItem as GLVatReportedClient;
            switch (ActionType)
            {
                case "DeleteRow":
                    if (selectedItem != null && dgVatVatSettlements.SelectedItem == dgVatVatSettlements.VisibleItems[0])
                    {
                        if (UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), selectedItem._ToDate.ToShortDateString()), Uniconta.ClientTools.Localization.lookup("Confirmation"),
                                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            DeleteRow(selectedItem);
                    }
                    break;
                case "VatReport":
                    if (selectedItem != null)
                        AddDockItem(TabControls.VATReport, new object[] { selectedItem }, string.Format("{0}:{1}-{2}", Uniconta.ClientTools.Localization.lookup("VatReport"), selectedItem.FromDate.ToShortDateString(), selectedItem.ToDate.ToShortDateString()), null, closeIfOpened: true);
                    else if (dgVatVatSettlements.ItemsSource == null || (dgVatVatSettlements.ItemsSource as IList).Count == 0)
                        Create();
                    break;
                case "VatSettlementReport":
                    if (selectedItem != null)
                        AddDockItem(TabControls.VATSettlementReport, new object[] { selectedItem, new double[18], new double[18], new string[6] }, Uniconta.ClientTools.Localization.lookup("VatDeclaration"), null, closeIfOpened: true);
                    break;
                case "Show":
                    if (selectedItem != null)
                        Post(selectedItem, false);
                    break;
                case "Post":
                    if (selectedItem != null && selectedItem._JournalPostedId == 0)
                        Post(selectedItem, true);
                    break;
                case "PostedBy":
                    if (selectedItem != null && selectedItem._JournalPostedId != 0)
                        JournalPosted(selectedItem);
                    break;
                case "Trans":
                    if (selectedItem != null && selectedItem._JournalPostedId != 0)
                        AddDockItem(TabControls.AccountsTransaction, selectedItem, Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._JournalPostedId));
                    break;
                case "PostedTransaction":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PostedTransactions, selectedItem, string.Concat(Uniconta.ClientTools.Localization.lookup("PostedTransactions"), " / ", NumberConvert.ToString(selectedItem._JournalPostedId)));
                    break;
                case "Upload":
                    if (selectedItem != null && dgVatVatSettlements.SelectedItem == dgVatVatSettlements.VisibleItems[0])
                    {
                        if (selectedItem.UploadedAt > DateTime.MinValue)
                        {
                            UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("UploadedAt") + ": {0}", selectedItem.UploadedAt.ToShortDateString()), Uniconta.ClientTools.Localization.lookup("Information"));
                            return;
                        }

                        if (api.CompanyEntity._CountryId == CountryCode.Iceland)
                        {
                            var cwTextControl = new CWTextControl(Localization.lookup("ReportTo") + " " + Localization.lookup("CustomsService"), Uniconta.ClientTools.Localization.lookup("Key"));
                            cwTextControl.Closed += CwTextControl_Closed;
                            cwTextControl.Show();
                        }
                        else
                        {
                            var cwConfirmationBox = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("UploadVATPermissionMsg"), 
                                                        Uniconta.ClientTools.Localization.lookup("Confirmation"), false, null,
                                                        true, Uniconta.ClientTools.Localization.lookup("GrantPermission"), Uniconta.ClientTools.Localization.lookup("Continue"));
                            cwConfirmationBox.Closed += delegate
                            {
                                if (cwConfirmationBox.DialogResult == true)
                                {
                                    if (cwConfirmationBox.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                                        System.Diagnostics.Process.Start("https://pdcs.skat.dk/dcs-atn-gateway/nemlogin?targetUrl=aHR0cHM6Ly9udHNlLnNrYXQuZGsvbnRzZS1mcm9udC9jb250ZW50P2lkPWZyYW1lOjJGQUF1dG9yaXNlcnJldmlzb3JtZmw=");
                                    else if (cwConfirmationBox.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.No)
                                        UploadSettlements(selectedItem, null);
                                }
                            };
                            cwConfirmationBox.Show();
                        }
                    }
                    break;
                case "View":
                    if (selectedItem != null)
                        View(selectedItem);
                    break;
                case "Create":
                    Create();
                    break;
                case "ApproveVAT":
                    if (selectedItem?.UploadedAt > DateTime.MinValue)
                    {
                        var deeplink = string.Format("https://ntse.skat.dk/ntse-front/dk/skat/RSU-Moms?periodStartDate={0}&periodSlutDate={1}", selectedItem._FromDate.ToString("yyyy-MM-dd"), selectedItem._ToDate.ToString("yyyy-MM-dd"));
                        System.Diagnostics.Process.Start(deeplink);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void Create()
        {
            var lst = dgVatVatSettlements.ItemsSource as ICollection<GLVatReportedClient>;
            DateTime fromdate = lst != null && lst.Count > 0 ? lst.Select(v => v._ToDate).Max() : DateTime.MinValue;
            if (fromdate != DateTime.MinValue)
                fromdate = fromdate.AddDays(1);
            else
                fromdate = new DateTime(DateTime.Now.Year, 1, 1);
            int Month;
            if (api.CompanyEntity._VatPeriod <= 4)
                Month = Math.Max(1, (int)api.CompanyEntity._VatPeriod);
            else if (api.CompanyEntity._VatPeriod == 5)
                Month = 6;
            else
                Month = 12;
            var dialog = new CWCalculateCommission(api, fromdate, fromdate.AddMonths(Month).AddDays(-1));
            dialog.SetTitleAndButton(Uniconta.ClientTools.Localization.lookup("Create"), Uniconta.ClientTools.Localization.lookup("OK"));
            dialog.Closed += delegate
            {
                if (dialog.DialogResult == true)
                {
                    var vatReported = new GLVatReportedClient
                    {
                        CompanyId = api.CompanyId,
                        _FromDate = dialog.FromDateTime,
                        _ToDate = dialog.ToDateTime
                    };
                    AddDockItem(TabControls.VATReport, new object[] { vatReported }, string.Format("{0}:{1}-{2}", Uniconta.ClientTools.Localization.lookup("VatReport"), vatReported.FromDate.ToShortDateString(), vatReported.ToDate.ToShortDateString()), null);
                }
            };
            dialog.Show();
        }

        private void CwTextControl_Closed(object sender, EventArgs e)
        {
            var selectedItem = dgVatVatSettlements.SelectedItem as GLVatReportedClient;
            var cwTextControl = (CWTextControl)sender;
            if (cwTextControl.DialogResult == true)
            {
                var key = cwTextControl.InputValue;
                UploadSettlements(selectedItem, key);
            }
        }

        void UploadSettlements(GLVatReportedClient selectedItem, string key)
        {
            if (UnicontaMessageBox.Show(Localization.lookup("ReportTo") + " " + Localization.lookup("CustomsService") + "\n" +
                            Localization.lookup("AreYouSureToContinue"), Localization.lookup("Confirmation"),
                                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Upload(selectedItem, key);
        }

        async void DeleteRow(GLVatReported rec)
        {
            var err = await api.Delete(rec);
            UtilDisplay.ShowErrorCode(err);
            if (err == 0)
                dgVatVatSettlements.Refresh();
        }

        static long Generate(VatReportLine[] lst, List<GLTransClientTotal> Trans, DateTime Date, int CompanyId, string Offset, string Text)
        {
            long total = 0;
            for (var i = 0; i < lst.Length; i++)
            {
                var lin = lst[i];
                if (lin.AccountIsVat != 0 && lin.AmountWithVat != 0 && lin.AccountNumber != null)
                {
                    var l = NumberConvert.ToLong(lin.AmountWithVat * 100d, true);
                    total += l;
                    Trans.Add(new GLTransClientTotal
                    {
                        CompanyId = CompanyId,
                        _Date = Date,
                        _Account = lin.AccountNumber,
                        _AmountCent = -l,
                        _Text = Text ?? Util.ConcatParenthesis(Localization.lookup("VatSettlement"), lin.Vat?._Vat),
                        _Origin = LedgerPostingType.VatSettlement
                    });
                }
                else if (lin.AmountWithout != 0 && lin.Account != null &&
                        (lin.Account._SystemAccount == (byte)SystemAccountTypes.ManuallyReceivableVAT ||
                         lin.Account._SystemAccount == (byte)SystemAccountTypes.ManuallyPayableVAT))
                {
                    var l = NumberConvert.ToLong(lin.AmountWithout * 100d, true);
                    total += l;
                    Trans.Add(new GLTransClientTotal
                    {
                        CompanyId = CompanyId,
                        _Date = Date,
                        _Account = lin.AccountNumber,
                        _AmountCent = -l,
                        _Text = Text,
                        _Origin = LedgerPostingType.VatSettlement
                    });

                }
            }
            if (total != 0 && Offset != null)
            {
                Trans.Add(new GLTransClientTotal
                {
                    CompanyId = CompanyId,
                    _Date = Date,
                    _Account = Offset,
                    _AmountCent = total,
                    _Text = Text,
                    _Origin = LedgerPostingType.VatSettlement
                });
                return 0;
            }
            return total;
        }

        static long Generate(VatSumOperationReport[] lst, List<GLTransClientTotal> Trans, DateTime Date, int CompanyId, string Offset, string Text)
        {
            long total = 0;
            for (var i = 13; (i < lst.Length && i < 19); i++)
            {
                var lin = lst[i];
                if (lin._Amount != 0)
                {
                    var l = NumberConvert.ToLong(lin._Amount * 100d, true);
                    total += l;
                    Trans.Add(new GLTransClientTotal
                    {
                        CompanyId = CompanyId,
                        _Date = Date,
                        _Account = lin.Acc._Account,
                        _AmountCent = -l,
                        _Text = Text,
                        _Origin = LedgerPostingType.VatSettlement
                    });
                }
            }
            if (total != 0 && Offset != null)
            {
                Trans.Add(new GLTransClientTotal
                {
                    CompanyId = CompanyId,
                    _Date = Date,
                    _Account = Offset,
                    _AmountCent = total,
                    _Text = Text,
                    _Origin = LedgerPostingType.VatSettlement
                });
                return 0;
            }
            return total;
        }

        void Post(GLVatReportedClient rec, bool post)
        {
            string Offset = null;
            var Accounts = api.GetCache(typeof(GLAccount));
            var arr = (GLAccount[])Accounts.GetRecords;
            for (int i = 0; (i < arr.Length); i++)
            {
                if (arr[i].SystemAccountEnum == SystemAccountTypes.VATSettlement)
                {
                    Offset = arr[i]._Account;
                    break;
                }
            }

            var cs = StreamingManagerReuse.Create(rec._Data);
            cs.CompanyId = rec.CompanyId;
            cs.UnpackParm = new VatReportLine.UnpackParm
            {
                Accounts = Accounts,
                Vats = api.GetCache(typeof(GLVat)),
                VatTypes = api.GetCache(typeof(GLVatType))
            };
            // current period
            var lstPeriod = (VatReportLine[])cs.ToArray(typeof(VatReportLine));
            var sumPeriod = (VatSumOperationReport[])cs.ToArray(typeof(VatSumOperationReport));

            var Trans = new List<GLTransClientTotal>(50);
            var total = Generate(lstPeriod, Trans, rec._ToDate, rec.CompanyId, Offset, null);
            var countryId = api.CompanyEntity._CountryId;
            if (countryId == CountryCode.Denmark || countryId == CountryCode.Greenland || countryId == CountryCode.FaroeIslands)
                total += Generate(sumPeriod, Trans, rec._ToDate, rec.CompanyId, Offset, null);

            // last period
            lstPeriod = (VatReportLine[])cs.ToArray(typeof(VatReportLine));
            sumPeriod = (VatSumOperationReport[])cs.ToArray(typeof(VatSumOperationReport));
            cs.Release();

            total += Generate(lstPeriod, Trans, rec._ToDate, rec.CompanyId, Offset, Uniconta.Common.Utility.Util.ConcatParenthesis(Localization.lookup("VatSettlement"), Localization.lookup("LastPeriod")));

            if (!post || total != 0)
                AddDockItem(TabControls.SimulatedTransactions, Trans.ToArray(), Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
            else
            {
                var postingDialog = new CWPostVAT(api, rec._ToDate);
                postingDialog.DialogTableId = 2000000014;
                postingDialog.Closed += async delegate
                {
                    if (postingDialog.DialogResult == true)
                    {
                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                        busyIndicator.IsBusy = true;

                        var postingApi = new PostingAPI(api);
                        var postingResult = await postingApi.PostVat(rec, postingDialog.Journal, postingDialog.PostedDate, postingDialog.Comments, Trans);

                        busyIndicator.IsBusy = false;
                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

                        if (postingResult != null)
                        {
                            if (postingResult.Err == ErrorCodes.Succes)
                            {
                                rec.JournalPostedId = postingResult.JournalPostedlId;
                                // everything was posted fine
                                string msg;
                                if (postingResult.JournalPostedlId != 0)
                                    msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), NumberConvert.ToString(postingResult.JournalPostedlId));
                                else
                                    msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                                UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                            }
                            else
                            {
                                UtilDisplay.ShowErrorCode(postingResult.Err);
                            }
                        }
                    }
                };
                postingDialog.Show();
            }
        }

        async void JournalPosted(GLVatReported selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new[] { selectedItem }, null);
            if (result != null && result.Length == 1)
                new CWGLPostedClientFormView(result[0]).Show();
        }

        static void SumLine(long[] a, double[] lst)
        {
            for (int i = 0; i < lst.Length; i++)
            {
                int index = 0;
                switch (i)
                {
                    case 33: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseSalgsMomsBeloeb; break;
                    case 34: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseMomsEUKoebBeloeb; break;
                    case 35: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseMomsEUYdelserBeloeb; break;
                    case 23: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseKoebsMomsBeloeb; break;
                    case 1: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseEUKoebBeloeb; break;
                    case 2: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseEUKoebYdelseBeloeb; break;
                    case 3: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseEUSalgBeloebVarerBeloeb; break;
                    case 4: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseIkkeEUSalgBeloebVarerBeloeb; break;
                    case 5: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseEUSalgYdelseBeloeb; break;
                    case 6: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseEksportOmsaetningBeloeb; break;
                    case 14: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseOlieAfgiftBeloeb; break;
                    case 15: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseElAfgiftBeloeb; break;
                    case 16: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseGasAfgiftBeloeb; break;
                    case 17: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseKulAfgiftBeloeb; break;
                    case 18: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseCO2AfgiftBeloeb; break;
                    case 19: index = (int)ModtagMomsangivelseForeloebigRequestFields.MomsAngivelseVandAfgiftBeloeb; break;
                }
                if (index != 0)
                    a[index] += NumberConvert.ToLong(lst[i]);
            }
        }

        async void Upload(GLVatReported rec, string key)
        {
            var a = new long[20];
            var Accounts = api.GetCache(typeof(GLAccount));
            var cs = StreamingManagerReuse.Create(rec._Data);
            cs.CompanyId = rec.CompanyId;
            cs.UnpackParm = new VatReportLine.UnpackParm
            {
                Accounts = Accounts,
                Vats = api.GetCache(typeof(GLVat)),
                VatTypes = api.GetCache(typeof(GLVatType))
            };
            var vatArray = new double[40];
            var OtherTaxName = new string[10];

            // current period
            var lstPeriod = (VatReportLine[])cs.ToArray(typeof(VatReportLine));
            var sumPeriod = (VatSumOperationReport[])cs.ToArray(typeof(VatSumOperationReport));
            VatSumOperationReport.SumArray(vatArray, sumPeriod, OtherTaxName);
            SumLine(a, vatArray);

            // last period
            lstPeriod = (VatReportLine[])cs.ToArray(typeof(VatReportLine));
            sumPeriod = (VatSumOperationReport[])cs.ToArray(typeof(VatSumOperationReport));
            Array.Clear(vatArray, 0, vatArray.Length);
            VatSumOperationReport.SumArray(vatArray, sumPeriod, OtherTaxName);
            SumLine(a, vatArray);

            cs.Release();

            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
            busyIndicator.IsBusy = true;

            var err = await new VatAPI(api).Upload(rec, a, key);

            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            var countryId = api.CompanyEntity._CountryId;
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
            else if (countryId == CountryCode.Denmark || countryId == CountryCode.Greenland || countryId == CountryCode.FaroeIslands)
            {
                TextBlock textBlock;
                var deeplink = string.Format("https://ntse.skat.dk/ntse-front/dk/skat/RSU-Moms?periodStartDate={0}&periodSlutDate={1}", rec._FromDate.ToString("yyyy-MM-dd"), rec._ToDate.ToString("yyyy-MM-dd"));
                var deeplinkMsg = string.Format(Uniconta.ClientTools.Localization.lookup("ApproveVATLink"), "www.skat.dk");
                textBlock = GetHyperlinkTextBlock(deeplink, deeplinkMsg);
                var cwDynamicCtrl = new CWDynamicControl(textBlock, Uniconta.ClientTools.Localization.lookup("Information"));
                cwDynamicCtrl.Show();
            }
        }

        async void View(GLVatReported rec)
        {
            busyIndicator.IsBusy = true;
            var vapi = new VatAPI(api);
            var buf = await vapi.GetReciept(rec);
            busyIndicator.IsBusy = false;

            if (buf == null && vapi.LastError != 0)
                UtilDisplay.ShowErrorCode(vapi.LastError);
            else
            {
                var att = new UserDocsClient()
                {
                    _TableId = GLVatReported.CLASSID,
                    _TableRowId = rec.RowId,
                    _RowId = 1,
                    _Data = buf,
                    _DocumentType = FileextensionsTypes.PDF
                };
                ViewDocument(TabControls.UserDocsPage3, att, string.Format(Uniconta.ClientTools.Localization.lookup("ViewOBJ"),
                Uniconta.ClientTools.Localization.lookup("CustomsService")), ViewerType.Attachment);
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.VATReport)
                dgVatVatSettlements.UpdateItemSource(argument);
        }

        private TextBlock GetHyperlinkTextBlock(string link, string messageUriContent)
        {
            var contents = messageUriContent.Split('#');
            var textBlock = new TextBlock() { TextWrapping = TextWrapping.Wrap };
            textBlock.Inlines.Add(new System.Windows.Documents.Run() { Text = contents[0] });
            var hyperlink = new System.Windows.Documents.Hyperlink() { NavigateUri = new Uri(link) };
            hyperlink.Inlines.Add(contents[1]);
            hyperlink.RequestNavigate += (s, e) => { System.Diagnostics.Process.Start(e.Uri.AbsoluteUri); };
            textBlock.Inlines.Add(hyperlink);
            textBlock.Inlines.Add(new System.Windows.Documents.Run() { Text = contents[2] });
            return textBlock;
        }
    }
}
