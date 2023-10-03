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
using UnicontaClient.Controls.Dialogs;
using NPOI.SS.UserModel;

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
                UtilDisplay.RemoveMenuCommand(rb, "Upload");
            }

        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new[] { typeof(Uniconta.DataModel.GLVatType), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLAccount) });
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgVatVatSettlements.SelectedItem as GLVatReportedClient;
            //if (selectedItem == null)
            //    selectedItem = new GLVatReportedClient();
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
                        var cwConfirmationBox = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("UploadVATPermissionMsg"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false, null,
                        true, Uniconta.ClientTools.Localization.lookup("GrantPermission"), Uniconta.ClientTools.Localization.lookup("Continue"));
                        cwConfirmationBox.Closed += delegate
                        {
                            if (cwConfirmationBox.DialogResult == true)
                            {
                                if (cwConfirmationBox.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                                {
                                    System.Diagnostics.Process.Start("https://eur02.safelinks.protection.outlook.com/?url=https%3A%2F%2Furldefense.proofpoint.com%2Fv2%2Furl%3Fu%3Dhttps-3A__pdcs.skat.dk_dcs-2Datn-2Dgateway_" +
                                        "nemlogin-3FtargetUrl-3DaHR0cHM6Ly93d3cuc2thdC5kay9mcm9udC9hcHBtYW5hZ2VyL3NrYXQvbnRzZT9fbmZwYj10cnVlJl9uZnBiPXRydWUmX3BhZ2VMYWJlbD1QMjgwMDY1NTM5MTM1OTczNDk4MTkwOSZfbmZscz1mYWxzZQ-3D-3D%26d%3DDwMFAw%26c%3Dvgc7_" +
                                        "vOYmgImobMVdyKsCY1rdGZhhtCa2JetijQZAG0%26r%3Dy-Ffb9u1CNakT-m5-tkvg8uGekr8sZ4_HWF7ClzBAtg%26m%3D-_GeOzxAxYuJnuimeioPUULdHuMjnGgsT_BvugxBrqE%26s%3DLHNyqY-YQe8nsTNDtajbSGgim9u3q9TtoUWmznVLg_M%26e%3D&data=05%7C01%7Cac%40uniconta.com%" +
                                        "7Cbdab8dea154a4bc3742208dbaea79758%7Cc77f48dcdd824f6da939e3b2bd7f2e95%7C0%7C0%7C638295807398038001%7CUnknown%7CTWFpbGZsb3d8eyJWIjoiMC4wLjAwMDAiLCJQIjoiV2luMzIiLCJBTiI6Ik1haWwiLCJXVCI6Mn0%3D%7C3000%7C%7C%7C&sdata=Jtbi9Wd1jp%2F0dALSTYtzof1bR8fbEzxHVhWe6TNlyYs%3D&reserved=0");
                                }
                                else if (cwConfirmationBox.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.No)
                                    UploadSettlements(selectedItem);
                            }
                        };
                        cwConfirmationBox.Show();
                    }
                    break;
                case "View":
                    if (selectedItem != null)
                        View(selectedItem);
                    break;
                case "Create":
                    DateTime fromdate = (dgVatVatSettlements.ItemsSource as IList<GLVatReportedClient>).Select(v => v._ToDate).DefaultIfEmpty().Max();
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
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void UploadSettlements(GLVatReportedClient selectedItem)
        {
            if (UnicontaMessageBox.Show(Localization.lookup("ReportTo") + " " + Localization.lookup("To") + " " + Localization.lookup("CustomsService") + "\n" +
                            Localization.lookup("AreYouSureToContinue"), Localization.lookup("Confirmation"),
                                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Upload(selectedItem);
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
            for (var i = 13; i < 19; i++)
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
            var Accounts = (GLAccount[])api.GetCache(typeof(GLAccount)).GetRecords;
            for (int i = 0; (i < Accounts.Length); i++)
            {
                if (Accounts[i].SystemAccountEnum == SystemAccountTypes.VATSettlement)
                {
                    Offset = Accounts[i]._Account;
                    break;
                }
            }

            var cs = StreamingManagerReuse.Create(rec._Data);
            cs.CompanyId = rec.CompanyId;
            // current period
            var lstPeriod = (VatReportLine[])cs.ToArray(typeof(VatReportLine));
            var sumPeriod = (VatSumOperationReport[])cs.ToArray(typeof(VatSumOperationReport));

            var Trans = new List<GLTransClientTotal>(50);
            var total = Generate(lstPeriod, Trans, rec._ToDate, rec.CompanyId, Offset, null);
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

        async void Upload(GLVatReported rec)
        {
            var a = new long[20];

            var cs = StreamingManagerReuse.Create(rec._Data);
            cs.CompanyId = rec.CompanyId;

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

            var err = await new VatAPI(api).Upload(rec, a);

            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

            UtilDisplay.ShowErrorCode(err);
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
                    _Data = buf
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
    }
}
