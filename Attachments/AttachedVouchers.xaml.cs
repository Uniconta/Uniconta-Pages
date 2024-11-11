using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using System.Threading.Tasks;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Uniconta.API.System;
using Uniconta.DataModel;
using System.Windows;
using Uniconta.API.Service;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Util;
#if !SILVERLIGHT
using Microsoft.Win32;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AttachedVouchersGrid : VouchersGrid
    {
        public override bool Readonly { get { return true; } }
    }

    public partial class AttachedVouchers : GridBasePage
    {
        public AttachedVouchers(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
        }

        public AttachedVouchers(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }
        object cache;

        public AttachedVouchers(UnicontaBaseEntity[] lst, BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
            dgAttachedVoucherGrid.SetSource(lst);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            this.DataContext = this;
            cache = VoucherCache.HoldGlobalVoucherCache;
            InitializeComponent();
            SetRibbonControl(localMenu, dgAttachedVoucherGrid);
            dgAttachedVoucherGrid.BusyIndicator = busyIndicator;
            dgAttachedVoucherGrid.api = api;
            dgAttachedVoucherGrid.UpdateMaster(master);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgAttachedVoucherGrid.RowDoubleClick += dgAttachedVoucherGrid_RowDoubleClick;
            this.BeforeClose += Vouchers_BeforeClose;
        }

        public override Task InitQuery() 
        {
            if (dgAttachedVoucherGrid.ItemsSource == null)
                return base.InitQuery();
            else
                return null; 
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "PrimaryKeyId", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    dgAttachedVoucherGrid.FilterString = $"[{rec.Name}] = {rec.Value}";
                    break;
                }
            }
            base.SetParameter(Parameters);
        }

        void Vouchers_BeforeClose()
        {
            cache = null;
            this.BeforeClose -= Vouchers_BeforeClose;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.Creditor));
        }

        void dgAttachedVoucherGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("ViewDownloadRow");
        }

        async void View(VouchersClient selectedItem)
        {
            if (selectedItem._Data == null)
            {
                busyIndicator.IsBusy = true;
                await UtilDisplay.GetData(selectedItem, api);
            }
            ViewVoucher(TabControls.VouchersPage3, dgAttachedVoucherGrid.syncEntity);
            busyIndicator.IsBusy = false;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgAttachedVoucherGrid.SelectedItem as VouchersClient;

            switch (ActionType)
            {
                case "UpdateRow":
                    if (selectedItem != null)
                        UploadData(selectedItem);
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        View(selectedItem);
                    break;
                case "ViewTransactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AccountsTransaction, dgAttachedVoucherGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem.RowId));
                    break;
                case "ExportVouchers":
                    AddDockItem(TabControls.VoucherExportPage, new object[] { dgAttachedVoucherGrid.GetVisibleRows() }, Uniconta.ClientTools.Localization.lookup("ExportVouchers"));
                    break;
                case "ShowInInbox":
                    if (selectedItem != null)
                        UpdateInBox(selectedItem);
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AttachedVouchersPage2, new object[] { selectedItem, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Voucher"), selectedItem.RowId));
                    break;
                case "PendingApproval":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DocumentApproveAwaitPage, dgAttachedVoucherGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PendingApproval"), selectedItem.RowId));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override bool IsDataChaged { get { return false; } }

        async void UpdateInBox(VouchersClient selectedItem)
        {
            var rec = new DocumentNoRef();
            rec.SetMaster(selectedItem);
            busyIndicator.IsBusy = true;
            var result = await api.Insert(rec);
            busyIndicator.IsBusy = false;
            UtilDisplay.ShowErrorCode(result);
        }

        private void UploadData(VouchersClient selectedItem)
        {
            var fileExt = selectedItem.Fileextension;
            var cwUpdateFile = new CWUpdateFile(fileExt, Uniconta.ClientTools.Localization.lookup("Voucher"));
            cwUpdateFile.Closed += async delegate
            {
                if (cwUpdateFile.DialogResult == true)
                {
                    byte[] buffer = cwUpdateFile.Contents;
                    string url = cwUpdateFile.Url;
                    if (selectedItem.RowId != 0 && (buffer != null || !string.IsNullOrEmpty(url)))
                    {
                        var org = new VouchersClient();
                        selectedItem._Data = null;
                        selectedItem._LoadedData = null;
                        StreamingManager.Copy(selectedItem, org);

                        selectedItem.SetNewBuffer(buffer);
                        selectedItem._Url = url;
                        selectedItem._NoCompress = !cwUpdateFile.Compress;
                        selectedItem._Fileextension = cwUpdateFile.fileExtensionType;
                        selectedItem._ScanDoc = true;
                        busyIndicator.IsBusy = true;
                        var err = await api.Update(org, selectedItem);
                        busyIndicator.IsBusy = false;
                        if (err == 0)
                        {
                            VoucherCache.SetGlobalVoucherCache(selectedItem);
                            dgAttachedVoucherGrid.UpdateItemSource(2, selectedItem);
                        }
                        else
                            UtilDisplay.ShowErrorCode(err);
                    }
                    else
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ViewerFailed"), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                        return;
                    }
                }
            };
            cwUpdateFile.Show();
        }

        public override string NameOfControl
        {
            get { return TabControls.AttachedVouchers; }
        }

        protected override Filter[] DefaultFilters()
        {
            return new Filter[] { new Filter() { name = "Created", value = String.Format("{0:d}..", BasePage.GetSystemDefaultDate().AddYears(-1).Date) } };
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AttachedVouchersPage2)
            {
                dgAttachedVoucherGrid.UpdateItemSource(argument);
            }
        }

        private void PrimaryKeyId_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ribbonControl.PerformRibbonAction("ViewDownloadRow");
        }
    }
}
