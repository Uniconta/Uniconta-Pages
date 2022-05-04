using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using UnicontaClient.Pages.Attachments;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VouchersFolderGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VouchersClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }

    public class ExtendedVouchersGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VoucherExtendedClient); } }
    }
    public partial class VoucherFolderPage : ControlBasePage
    {
        VouchersClient voucherClient;
        string action;
        List<int> removedRowIds = null;
        public VoucherFolderPage(UnicontaBaseEntity sourceData, string envelopeAction) :
            base(sourceData)
        {
            InitializeComponent();
            InitPage(envelopeAction);
        }

        string envelopeAction;
        private void InitPage(string _envelopeAction)
        {
            MainControl = dgVoucherFolderGrid;
            envelopeAction = _envelopeAction;
            action = _envelopeAction;
            ribbonControl = localMenu;
            dgVouchersGrid.api = dgVoucherFolderGrid.api = api;
            dgVoucherFolderGrid.Readonly = false;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            if (LoadedRow == null)
                voucherClient = new VouchersClient();
            else
                voucherClient = LoadedRow as VouchersClient;
            SetRibbon(envelopeAction);
        }

        private void SetRibbon(string Action)
        {
            switch (Action)
            {
                case "Remove":
                    ribbonControl.DisableButtons(new string[] { "AddVoucher", "AddPhysicalVoucher", "ViewPhysicalVoucher" });
                    break;
            }
        }

        public async override Task InitQuery()
        {
            await dgVouchersGrid.Filter(null);
            var defaultFilterString = "Contains([Envelope],'false')";
            dgVouchersGrid.FilterString = defaultFilterString;
            dgVoucherFolderGrid.Visibility = Visibility.Visible;
            removedRowIds = new List<int>();

            switch (envelopeAction)
            {
                case "Create":
                    dgVouchersGrid.Visibility = Visibility.Visible;
                    break;
                case "Edit":
                    var dapi = new DocumentAPI(api);
                    var items = (VouchersClient[])await dapi.GetEnvelopeContent(voucherClient, false);
                    dgVoucherFolderGrid.ItemsSource = items.ToList(); // we need to do a toList, so we can update the list
                    var rowIds = items.Select(p => p.RowId).ToList();
                    if (rowIds.Count > 0)
                        dgVouchersGrid.FilterString = GetFilterString(defaultFilterString, rowIds);
                    dgVouchersGrid.Visibility = Visibility.Visible;
                    break;
            }
        }

        private string GetFilterString(string defaultFilter, List<int> newRowIds, List<int> existingRowIds = null)
        {
            var strBuilder = StringBuilderReuse.Create(defaultFilter);
            strBuilder.Append(" And Not [RowId] In (");
            var totalRowIds = new List<int>();
            if (newRowIds != null)
                totalRowIds.AddRange(newRowIds);
            if (existingRowIds != null)
                totalRowIds.AddRange(existingRowIds);

            foreach (var rowId in totalRowIds.Distinct())
                strBuilder.Append('\'').AppendNum(rowId).Append('\'').Append(',');

            strBuilder.Length--;
            strBuilder.Append(')');

            return strBuilder.ToStringAndRelease();
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedVoucher = dgVouchersGrid.SelectedItem as VoucherExtendedClient;
            var selectedVoucherFolder = dgVoucherFolderGrid.SelectedItem as VouchersClient;
            switch (ActionType)
            {
                case "Save":
                    Save();
                    break;
                case "AddVoucher":
                    var itemsOld = dgVoucherFolderGrid.ItemsSource == null ? null : ((IEnumerable<VouchersClient>)dgVoucherFolderGrid.ItemsSource).Select(p => p.RowId).ToList();
                    var VouchersAdd = ((IEnumerable<VoucherExtendedClient>)dgVouchersGrid.ItemsSource).Where(p => p.IsAdded == true).ToList();
                    if (VouchersAdd.Count > 0)
                        AddVouchers(VouchersAdd);
                    else if (selectedVoucher != null)
                        AddVouchers(new List<VoucherExtendedClient>() { selectedVoucher });
                    var itemsNew = VouchersAdd.Select(p => p.RowId).ToList();
                    dgVouchersGrid.FilterString = GetFilterString("Contains([Envelope],'false')", itemsNew, itemsOld);
                    dgVoucherFolderGrid.Visibility = Visibility.Visible;
                    break;
                case "RemoveVoucher":
                    if (selectedVoucherFolder == null)
                        return;
                    var rowId = selectedVoucherFolder.RowId;
                    dgVoucherFolderGrid.DeleteRow();
                    removedRowIds.Add(rowId);

                    var voucherRow = ((IEnumerable<VoucherExtendedClient>)dgVouchersGrid.ItemsSource).Where(p => p.RowId == rowId).SingleOrDefault();
                    voucherRow.IsAdded = false;
                    var itemExisting = ((IEnumerable<VouchersClient>)dgVoucherFolderGrid.ItemsSource).Select(p => p.RowId).ToList();
                    dgVouchersGrid.FilterString = GetFilterString("Contains([Envelope],'false')", null, itemExisting);
                    break;
                case "ViewPhysicalVoucher":
                case "ViewVoucher":
                    if (selectedVoucher == null)
                        return;
                    ViewVoucher(TabControls.VouchersPage3, dgVouchersGrid.syncEntity);
                    break;
                case "ViewFolderVoucher":
                    if (selectedVoucherFolder == null)
                        return;
                    ViewVoucher(TabControls.VouchersPage3, dgVoucherFolderGrid.syncEntity);
                    break;
                default:
                    controlRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void AddVouchers(List<VoucherExtendedClient> vouchersAdd)
        {
            var t = api.CompanyEntity.GetUserType(typeof(VouchersClient));
            foreach (var voucher in vouchersAdd)
            {
                VouchersClient voucherClient;
                if (t != null)
                    voucherClient = Activator.CreateInstance(t) as VouchersClient;
                else
                    voucherClient = new VouchersClient();

                var buf = voucher._Data;
                voucher._Data = null;
                StreamingManager.Copy(voucher, voucherClient);
                voucher._Data = buf;
                voucherClient._Data = buf;

                if (dgVoucherFolderGrid.ItemsSource == null)
                    dgVoucherFolderGrid.InsertRow(voucherClient, -1);
                else
                {
                    var list = (IEnumerable<VouchersClient>)dgVoucherFolderGrid.ItemsSource;
                    if (!list.Contains(voucherClient))
                        dgVoucherFolderGrid.InsertRow(voucherClient, dgVoucherFolderGrid.View.FocusedRowHandle < 0 ? -1 : dgVoucherFolderGrid.View.FocusedRowHandle);
                }
            }
        }

        async private void Save()
        {
            var result = false;
            var documentApi = new DocumentAPI(api);
            try
            {
                switch (action)
                {
                    case "Create":
                        var newlist = (IEnumerable<VouchersClient>)dgVoucherFolderGrid.ItemsSource;
                        if (newlist != null)
                        {
                            CWCreateFolder createFolderDialog = new CWCreateFolder();
                            createFolderDialog.Closing += async delegate
                               {
                                   if (createFolderDialog.DialogResult == true)
                                   {
                                       voucherClient.Text = createFolderDialog.FolderName;
                                       voucherClient.Content = createFolderDialog.ContentType;
                                       var createResult = await documentApi.CreateEnvelope(voucherClient, newlist);
                                       // Folder now contains a full record with the content.
                                       if (createResult == ErrorCodes.Succes)
                                       {
                                           globalEvents.OnRefresh(TabControls.VoucherFolderPage);
                                           CloseDockItem();
                                       }
                                       else
                                           UtilDisplay.ShowErrorCode(ErrorCodes.CouldNotSave);

                                   }
                               };
                            result = true;
                            createFolderDialog.Show();
                        }
                        break;
                    case "Edit":

                        //Removing the Items
                        if (removedRowIds.Count > 0)
                        {
                            var allVouchers = (IEnumerable<VoucherExtendedClient>)dgVouchersGrid.ItemsSource;
                            var listRemove = new List<VouchersClient>();
                            foreach (var id in removedRowIds)
                            {
                                var tempVoucher = allVouchers.Where(r => r.RowId == id).SingleOrDefault();
                                if (tempVoucher != null)
                                    listRemove.Add(tempVoucher);
                            }

                            var removeResult = await documentApi.RemoveFromEnvelope(voucherClient, listRemove);
                            result = (removeResult == ErrorCodes.Succes);
                        }

                        //Adding the Items

                        var appendList = (IEnumerable<VouchersClient>)dgVoucherFolderGrid.ItemsSource;
                        if (appendList != null)
                        {
                            var appendResult = await documentApi.AppendToEnvelope(voucherClient, appendList);
                            result = (appendResult == ErrorCodes.Succes);
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                documentApi.ReportException(ex, "Envelopes, Action=" + action);
                result = false;
            }
            if (action != "Create")
            {
                if (result)
                    CloseDockItem();
                else
                    UtilDisplay.ShowErrorCode(ErrorCodes.CouldNotSave);
            }
        }

        async private void BindGrid()
        {
            var vouchers = await api.Query<VouchersClient>();
            dgVoucherFolderGrid.ItemsSource = vouchers;
            dgVoucherFolderGrid.Visibility = Visibility.Visible;
        }

        public override string NameOfControl { get { return TabControls.VoucherFolderPage; } }
    }

    public class VoucherExtendedClient : VouchersClient
    {
        bool _isAdded;
        public bool IsAdded { get { return _isAdded; } set { _isAdded = value; NotifyPropertyChanged("IsAdded"); } }
    }
}
