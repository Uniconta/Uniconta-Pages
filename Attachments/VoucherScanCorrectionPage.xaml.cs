using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class VoucherScanCorrectionClient : AzureScanCorrectionClient
    {
        [Display(Name = "CreditorAccount", ResourceType = typeof(VouchersClientText))]
        public string CreditorAccount
        {
            get
            {
                var creditorAccount = GetVouncherClient()?.CreditorAccount;
                if (!string.IsNullOrWhiteSpace(creditorAccount))
                    return creditorAccount;

                var taxId = _CreditorLegalIdent?.Split('|')?.FirstOrDefault(p => p.StartsWith("TAXID:"));
                if (taxId != null)
                {
                    var colonIdx = taxId.IndexOf(':');
                    return colonIdx >= 0 ? taxId.Substring(colonIdx + 1) : null;
                }

                return null;
            }
        }

        [Display(Name = "CreditorName", ResourceType = typeof(VouchersClientText))]
        public string CreditorName => GetVouncherClient()?.CreditorName;

        [Display(Name = "Invoice", ResourceType = typeof(GLDailyJournalText))]
        public string Invoice => GetVouncherClient()?.Invoice ?? GetVouncherClient()?.Voucher.ToString();

        [Display(Name = "DocumentDate", ResourceType = typeof(VouchersClientText))]
        public DateTime DocumentDate =>
            GetVouncherClient()?.DocumentDate ??
            GetVouncherClient()?.PostingDate ??
            GetVouncherClient()?.Created ??
            DateTime.MinValue;

        private VouchersClient _voucherClient;
        private VouchersClient GetVouncherClient()
        {
            if (_voucherClient == null)
                _voucherClient = VoucherCache.GetGlobalVoucherCache(CompanyId, DocumentRef);

            return _voucherClient;
        }
    }

    public partial class VoucherScanCorrectionGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VoucherScanCorrectionClient); } }
        public override bool Readonly => false;
    }
    public partial class VoucherScanCorrectionPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.VoucherScanCorrectionPage.ToString(); } }
        public VoucherScanCorrectionPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            dgVoucherScanCorrection.api = api;
            dgVoucherScanCorrection.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgVoucherScanCorrection);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override async Task InitQuery()
        {
            await base.InitQuery();
            var lst = dgVoucherScanCorrection.ItemsSource as IEnumerable<VoucherScanCorrectionClient>;
            var vouchers = lst?
                .Where(c => c.DocumentRef != 0 &&
                            (c._CreditorLegalIdent == null || !c._CreditorLegalIdent.Contains("TAXID")))?
                .Select(c => new VouchersClient { RowId = c.DocumentRef })?
                .ToList();

            if (vouchers == null || vouchers.Count == 0)
                return;

            var err = await api.Read(vouchers);
            if (err != 0)
                return;

            vouchers.ForEach(VoucherCache.SetGlobalVoucherCache);
            foreach (var item in lst)
                item.NotifyPropertyChanged(null);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            if (ActionType == "Delete")
                dgVoucherScanCorrection.DeleteRow();
            if (ActionType == "RefreshGrid")
                gridRibbon_BaseActions(ActionType);
        }

        protected override void OnLayoutLoaded()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "AddRow", "EditRow", "SaveGrid", "Layout" });
            base.OnLayoutLoaded();

            var floatGroup = this.ParentControl?.Parent as DevExpress.Xpf.Docking.FloatGroup;
            if (floatGroup != null)
            {
                var newWidth = 900;
                var newHeight = 500;
                floatGroup.FloatSize = new System.Windows.Size(newWidth, newHeight);

                // Center on screen
                var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                var screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                floatGroup.FloatLocation = new System.Windows.Point(
                    (screenWidth - newWidth) / 2,
                    (screenHeight - newHeight) / 2
                );
            }
        }
    }
}
