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
            : base(null)
        {
            InitPage(master);
        }
        object cache;

        private void InitPage(UnicontaBaseEntity master)
        {
            this.DataContext = this;
            cache = VoucherCache.HoldGlobalVoucherCache;
            InitializeComponent();
            SetRibbonControl(localMenu, dgAttachedVoucherGrid);
            dgAttachedVoucherGrid.BusyIndicator = busyIndicator;
            dgAttachedVoucherGrid.api = api;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgAttachedVoucherGrid.RowDoubleClick += dgAttachedVoucherGrid_RowDoubleClick;
            this.BeforeClose += Vouchers_BeforeClose;
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
            localMenu_OnItemClicked("ViewDownloadRow");
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
                    var vouchers = (IEnumerable<VouchersClient>)dgAttachedVoucherGrid.GetVisibleRows();
                    object[] param = new object[] { vouchers };
                    AddDockItem(TabControls.VoucherExportPage, param, Uniconta.ClientTools.Localization.lookup("ExportVouchers"));
                    break;
                case "ShowInInbox":
                    if (selectedItem != null)
                        UpdateInBox(selectedItem);
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;
                    AddDockItem(TabControls.AttachedVouchersPage2, EditParam, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Voucher"), selectedItem.RowId));
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

        public override bool IsDataChaged { get{ return false; } }
       
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
                    if (cwUpdateFile.Contents != null)
                    {
                        byte[] buffer = cwUpdateFile.Contents;
                        if (buffer != null && selectedItem.RowId != 0)
                        {
                            selectedItem._Data = buffer;
                            VoucherCache.SetGlobalVoucherCache(selectedItem);
                            busyIndicator.IsBusy = true;
                            await api.Update(selectedItem);
                            busyIndicator.IsBusy = false;
                        }
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

        async void SaveVouchers(IEnumerable<VouchersClient> vouchers)
        {
            byte[] compressedResult;

            var saveDialog = Uniconta.ClientTools.Util.UtilDisplay.LoadSaveFileDialog;
            saveDialog.Filter = "ZIP Files (*.zip)|*.zip";
            bool? dialogResult = saveDialog.ShowDialog();
            if (dialogResult == true)
            {
                try
                {
#if !SILVERLIGHT
                    using (Stream stream = File.Create(saveDialog.FileName))
#else
                    using (Stream stream = (Stream)saveDialog.OpenFile())
#endif
                    {
                        compressedResult = await CreateZip(vouchers);
                        stream.Write(compressedResult, 0, compressedResult.Length);
                        stream.Flush();
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                }
            }
        }

        private async Task<byte[]> CreateZip(IEnumerable<VouchersClient> vouchers)
        {
            var zippedMemoryStream = UnistreamReuse.Create();
            using (ZipOutputStream zipOutputStream = new ZipOutputStream(zippedMemoryStream))
            {
                // Highest compression rating
                zipOutputStream.SetLevel(9);
                busyIndicator.IsBusy = true;

                foreach (var voucher in vouchers)
                {
                    zippedMemoryStream.SecureSize();
                    if (voucher._Data == null)
                        await UtilDisplay.GetData(voucher, api);
                    byte[] attachment = voucher.Buffer;
                    zippedMemoryStream.SecureSize(attachment.Length);
                    // Write the data to the ZIP file  
                    string name = string.Format("{0}_{1}.{2}", voucher.Text, voucher.RowId, Enum.GetName(typeof(FileextensionsTypes), voucher.Fileextension));
                    name = name.Replace("/", "-").Replace(@"\", "-");
                    ZipEntry entry = new ZipEntry(name);
                    zipOutputStream.PutNextEntry(entry);
                    zipOutputStream.Write(attachment, 0, attachment.Length);
                }
                busyIndicator.IsBusy = false;

                zipOutputStream.Finish();
            }
            return zippedMemoryStream.ToArrayAndRelease();
        }

        public override string NameOfControl
        {
            get { return TabControls.AttachedVouchers.ToString(); }
        }

        protected override Filter[] DefaultFilters()
        {
            Filter dateFilter = new Filter();
            dateFilter.name = "Created";
            dateFilter.value = String.Format("{0:d}..", BasePage.GetSystemDefaultDate().AddYears(-1).Date);
            return new Filter[] { dateFilter };
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AttachedVouchersPage2)
            {
                dgAttachedVoucherGrid.UpdateItemSource(argument);
            }
        }
    }
}
