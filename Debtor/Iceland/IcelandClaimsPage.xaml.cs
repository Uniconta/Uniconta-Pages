using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class IcelandClaimsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(IkB2BClaimsClient); } }
        public override bool Readonly { get { return true; } }
        public override bool CanInsert { get { return false; } }
        public override bool CanDelete { get { return false; } }
    }

    // Kröfuskrá: the Icelandic online claims (IkB2BClaims) and what can be done with them at the bank through the server
    // (Uniconta.API.DebtorCreditor.IcelandClaimsAPI). Opened from the Direct Debit page for Icelandic companies.
    public partial class IcelandClaimsPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.IcelandClaimsPage; } }

        public IcelandClaimsPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgIcelandClaimsGrid;
            dgIcelandClaimsGrid.api = api;
            SetRibbonControl(localMenu, dgIcelandClaimsGrid);
            dgIcelandClaimsGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            GetShowHideStatusInfoSection();
            SetShowHideStatusInfoSection(true);
        }

        protected override SortingProperties[] DefaultSort()
        {
            return new SortingProperties[] { new SortingProperties("PaymentId") { Ascending = false } };
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ClaimsRefreshStatus":
                    RefreshStatus();
                    break;
                case "ClaimsSend":
                    SendClaims();
                    break;
                case "ClaimsChangeFinalDueDate":
                    ChangeFinalDueDate();
                    break;
                case "ClaimsCancel":
                    CancelClaims();
                    break;
                case "ClaimsBook":
                    BookClaims();
                    break;
                case "ClaimsPayments":
                    ShowPayments();
                    break;
                case "ClaimsSync":
                    SyncClaims();
                    break;
                case "EnableStatusInfoSection":
                    hideStatusInfoSection = !hideStatusInfoSection;
                    SetShowHideStatusInfoSection(hideStatusInfoSection);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        List<IkB2BClaimsClient> SelectedClaims()
        {
            var lst = new List<IkB2BClaimsClient>();
            var items = dgIcelandClaimsGrid.SelectedItems;
            if (items != null)
                lst.AddRange(items.OfType<IkB2BClaimsClient>());
            if (lst.Count == 0 && dgIcelandClaimsGrid.SelectedItem is IkB2BClaimsClient one)
                lst.Add(one);
            return lst;
        }

        bool NoneSelected(List<IkB2BClaimsClient> lst)
        {
            if (lst.Count > 0)
                return false;
            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
            return true;
        }

        void RefreshStatus()
        {
            var lst = SelectedClaims();
            if (lst.Count <= 1)
            {
                // one row is usually just the focused row: offer every claim at the bank
                var answer = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("IcelandClaimsRefreshAllAsk"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.YesNoCancel);
                if (answer == MessageBoxResult.Cancel)
                    return;
                if (answer == MessageBoxResult.Yes)
                    lst = null;
                else if (NoneSelected(lst))
                    return;
            }
            Run(capi => capi.Refresh(lst));
        }

        void SendClaims()
        {
            var lst = SelectedClaims();
            if (NoneSelected(lst))
                return;
            Run(capi => capi.Send(lst));
        }

        void ChangeFinalDueDate()
        {
            var lst = SelectedClaims();
            if (NoneSelected(lst))
                return;
            var current = lst[0]._Deadline != DateTime.MinValue ? lst[0]._Deadline : BasePage.GetSystemDefaultDate().Date;
            var cw = new CWDateSelector(current);
            cw.DateLabel = Uniconta.ClientTools.Localization.lookup("GraceDate");
            cw.Closing += delegate
            {
                if (cw.DialogResult == true && cw.SelectedDate != DateTime.MinValue)
                    Run(capi => capi.ChangeFinalDueDate(lst, cw.SelectedDate.Date));
            };
            cw.Show();
        }

        void CancelClaims()
        {
            var lst = SelectedClaims();
            if (NoneSelected(lst))
                return;
            if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            Run(capi => capi.Cancel(lst));
        }

        void BookClaims()
        {
            var lst = SelectedClaims().Where(c => c.Status == IkB2BClaimStatus.Paid || c.Status == IkB2BClaimStatus.PartlyPaid).ToList();
            if (lst.Count == 0)
            {
                // nothing paid selected: every paid claim
                if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ProceedConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                    return;
                lst = null;
            }
            Run(capi => capi.Book(lst));
        }

        // the payments the bank reported on the focused claim, one row each (IkB2BClaimPayment), read-only
        async void ShowPayments()
        {
            var claim = dgIcelandClaimsGrid.SelectedItem as IkB2BClaimsClient;
            if (claim == null)
            {
                NoneSelected(new List<IkB2BClaimsClient>());
                return;
            }
            IkB2BClaimPaymentClient[] rows;
            busyIndicator.IsBusy = true;
            try
            {
                rows = await api.Query<IkB2BClaimPaymentClient>(new[] { PropValuePair.GenereteWhereElements("Claim", typeof(int), claim.RowId.ToString(System.Globalization.CultureInfo.InvariantCulture)) });
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                UnicontaMessageBox.Show(ex);
                return;
            }
            busyIndicator.IsBusy = false;
            var title = string.Concat(Uniconta.ClientTools.Localization.lookup("BankClaim"), " ", claim.PaymentId.ToString("000000"));
            if (rows == null || rows.Length == 0)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("IcelandClaimNoPayments"), title), Uniconta.ClientTools.Localization.lookup("Information"));
                return;
            }
            var hide = new List<string> { "CompanyId", "RowId", "Claim", "Account", "Name", "Kind", "Status" }; // KindText and StatusText show them
            new CWMessageGridBox(rows.OrderBy(r => r._PayDate).ThenBy(r => r.RowId).ToList(), title, hide).Show();
        }

        void SyncClaims()
        {
            var today = BasePage.GetSystemDefaultDate().Date;
            var cw = new CWInterval(today.AddMonths(-1), today.AddMonths(3), 0, hideVoucher: true);
            cw.Closing += delegate
            {
                if (cw.DialogResult == true)
                    Run(capi => capi.Sync(null, cw.FromDate, cw.ToDate)); // null: the default Iceland payment format
            };
            cw.Show();
        }

        async void Run(Func<IcelandClaimsAPI, Task<IcelandClaimsResult>> call)
        {
            var capi = new IcelandClaimsAPI(api);
            IcelandClaimsResult result;
            busyIndicator.IsBusy = true;
            try
            {
                result = await call(capi);
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                UnicontaMessageBox.Show(ex);
                return;
            }
            busyIndicator.IsBusy = false;
            ShowResult(result, capi);
            gridRibbon_BaseActions("RefreshGrid");
        }

        /// <summary>Counts, the server's message and the per-claim lines of a claims call, or the error when the call failed</summary>
        public static void ShowResult(IcelandClaimsResult result, BaseAPI capi)
        {
            if (result == null)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup(capi.LastError.ToString()), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }
            var sb = new StringBuilder();
            if (result.Err != ErrorCodes.Succes)
                sb.AppendLine(result.Message ?? Uniconta.ClientTools.Localization.lookup(result.Err.ToString()));
            else if (!string.IsNullOrWhiteSpace(result.Message))
                sb.AppendLine(result.Message);

            Count(sb, "Completed", result.Done);
            Count(sb, "Updated", result.Updated);
            Count(sb, "Created", result.Inserted);
            Count(sb, "Pending", result.Pending);
            Count(sb, "NoSucces", result.Failed);
            Count(sb, "ClaimsSkipped", result.Skipped);

            if (!string.IsNullOrWhiteSpace(result.Details))
                sb.AppendLine().AppendLine(result.Details.Trim());

            var text = sb.ToString().Trim();
            if (text.Length == 0)
                text = Uniconta.ClientTools.Localization.lookup("Completed");
            var ok = result.Err == ErrorCodes.Succes && result.Failed == 0;
            UnicontaMessageBox.Show(text, Uniconta.ClientTools.Localization.lookup(ok ? "Information" : "Warning"));
        }

        static void Count(StringBuilder sb, string key, int n)
        {
            if (n != 0)
                sb.Append(Uniconta.ClientTools.Localization.lookup(key)).Append(": ").Append(n).AppendLine();
        }

        ItemBase ibase;
        bool hideStatusInfoSection = true;
        void GetShowHideStatusInfoSection()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "EnableStatusInfoSection");
        }

        void SetShowHideStatusInfoSection(bool _hideStatusInfoSection)
        {
            if (ibase == null)
                return;
            if (_hideStatusInfoSection)
            {
                rowgridSplitter.Height = new GridLength(0);
                rowStatusInfoSection.Height = new GridLength(0);
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("SystemInfo"));
            }
            else
            {
                if (rowgridSplitter.Height.Value == 0 && rowStatusInfoSection.Height.Value == 0)
                {
                    rowgridSplitter.Height = new GridLength(2);
                    rowStatusInfoSection.Height = new GridLength(1, GridUnitType.Auto);
                }
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("HideOBJ"), Uniconta.ClientTools.Localization.lookup("SystemInfo"));
            }
        }
    }
}
