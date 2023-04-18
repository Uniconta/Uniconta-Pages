using Uniconta.API.System;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using UnicontaClient.Pages;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using System.ComponentModel;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWAddVouchers : ChildWindow
    {
        private CrudAPI api;
        public VouchersClient vouchersClient;
        VouchersClient[] multiVouchersClient;
        public int[] VoucherRowIds;

        public CWAddVouchers(CrudAPI crudApi, bool isMultiselect = false, VouchersClient voucher = null)
        {
            InitializeComponent();
            if (voucher != null)
                vouchersClient = voucher;
            else
                vouchersClient = new VouchersClient();
            fileBrowseControl.IsMultiSelect = isMultiselect;
            api = crudApi;
            txtAccount.api = api;
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("PhysicalVouchers"));
            this.DataContext = this;
            contentlayout.DataContext = vouchersClient;
            this.Loaded += CW_Loaded;
            fileBrowseControl.CompressVisibility = Visibility.Visible;
            fileBrowseControl.PDFSplitVisibility = Visibility.Visible;
            ueLookupCtrl.CrudApi = api;

            if (!api.CompanyEntity.Purchase)
            {
                txtPurchaseNumber.Visibility = ueLookupCtrl.Visibility = Visibility.Collapsed;
                rowPurchaseNumberSpace.Height = rowPurchaseNumber.Height = new GridLength(0);
            }
            if (!api.CompanyEntity.Creditor)
            {
                txtCreditorAccount.Visibility = txtAccount.Visibility = Visibility.Collapsed;
                rowAccountNumberSpace.Height = rowAccountNumber.Height = new GridLength(0);
            }
        }

        public CWAddVouchers(CrudAPI crudAPI, VouchersClient voucher, bool isFromDragDropWindow) : this(crudAPI, false, voucher)
        {
            if (isFromDragDropWindow)
            {
                Height = 640.0;
                Width = 1024.0;
                fileBrowseControl.Visibility = Visibility.Collapsed;
                fileBrowseControl.FileBytes = vouchersClient._Data;
                fileBrowseControl.FileName = vouchersClient._Text;
                fileBrowseControl.FileExtension = vouchersClient._Fileextension.ToString();
                SizeToContent = SizeToContent.Manual;
                txtSelectFile.Visibility = Visibility.Collapsed;
                colContentLayout.Width = new GridLength(0.5, GridUnitType.Star);
                colViewLayout.Width = new GridLength(1, GridUnitType.Star);
                splitter.Visibility = Visibility.Visible;
                viewLayout.Visibility = Visibility.Visible;
                voucherViewer.HasMultipleVouchers = isFromDragDropWindow;
                voucherViewer.Loaded += VoucherViewer_Loaded;
            }
        }

        private void VoucherViewer_Loaded(object sender, RoutedEventArgs e)
        {
            voucherViewer.Vouchers = new VouchersClient[] { vouchersClient };
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { SaveButton.Focus(); }));

        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (SaveButton.IsFocused)
                    SaveButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
        async private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorCodes result = ErrorCodes.NoSucces;

            if (fileBrowseControl.IsMultiSelect)
            {
                var list = fileBrowseControl.Split ? fileBrowseControl.LoadFileInfosBySplittingPDF() : fileBrowseControl.SelectedFileInfos;
                var fileCount = list != null ? list.Length : 0;
                if (fileCount > 0)
                {
                    multiVouchersClient = new VouchersClient[fileCount];
                    byte[][] buffer;
                    if (fileCount > 1)
                        buffer = new byte[fileCount][];
                    else
                    {
                        var buf = list[0].FileBytes;
                        if (buf != null && buf.Length > 100000)
                            buffer = new byte[1][];
                        else
                            buffer = null;
                    }
                    VoucherRowIds = new int[fileCount];
                    int iCtr;
                    for (iCtr = 0; (iCtr < fileCount); iCtr++)
                    {
                        var voucher = new VouchersClient();
                        var fileInfo = list[iCtr];
                        if (buffer != null)
                            buffer[iCtr] = fileInfo.FileBytes;
                        else
                            voucher._Data = fileInfo.FileBytes;
                        voucher._Text = string.IsNullOrEmpty(vouchersClient._Text) ? fileInfo.FileName : vouchersClient._Text;
                        voucher._Fileextension = DocumentConvert.GetDocumentType(fileInfo.FileExtension);
                        voucher._Content = vouchersClient._Content;
                        voucher._Amount = vouchersClient._Amount;
                        voucher._Invoice = vouchersClient._Invoice;
                        voucher._DocumentDate = vouchersClient._DocumentDate;
                        voucher._Currency = vouchersClient._Currency;
                        voucher._CreditorAccount = vouchersClient._CreditorAccount;
                        voucher._PurchaseNumber = vouchersClient._PurchaseNumber;
                        multiVouchersClient[iCtr] = voucher;
                    }

                    busyIndicator.IsBusy = true;
                    result = await api.Insert(multiVouchersClient);
                    busyIndicator.IsBusy = false;
                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                    {
                        for (iCtr = 0; (iCtr < fileCount); iCtr++)
                            VoucherRowIds[iCtr] = multiVouchersClient[iCtr].RowId;

                        if (buffer != null)
                            Utility.UpdateBuffers(api, buffer, multiVouchersClient);
                    }
                }
            }
            else
            {
                var fileBytes = fileBrowseControl.FileBytes;
                if (fileBytes != null && fileBytes.Length > 0)
                {
                    // This here will first save the record without attachment. Then it will update with attachement.
                    // The update will be done without await, so it will be done in the background.
                    // This way it is fast to upload a document, since the user will not have to wait.

                    if (fileBytes.Length < 100000) // No delayed update
                    {
                        vouchersClient._Data = fileBytes;
                        fileBytes = null;
                    }
                    vouchersClient._Fileextension = DocumentConvert.GetDocumentType(fileBrowseControl.FileExtension);
                    busyIndicator.IsBusy = true;
                    result = await api.Insert(vouchersClient);
                    busyIndicator.IsBusy = false;

                    if (result == ErrorCodes.Succes && vouchersClient.RowId != 0)
                    {
                        VoucherRowIds = new int[] { vouchersClient.RowId };

                        if (fileBytes != null) // Delayed update
                        {
                            vouchersClient._LoadedData = null;
                            vouchersClient._Data = null;
                            var org = StreamingManager.Clone(vouchersClient);
                            vouchersClient._Data = fileBytes;
                            VoucherCache.SetGlobalVoucherCache(vouchersClient);
                            api.UpdateNoResponse(org, vouchersClient);
                        }
                        else
                            VoucherCache.SetGlobalVoucherCache(vouchersClient);
                    }
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
                else
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InvalidFile"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
            }

            if (result == ErrorCodes.Succes)
                SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            voucherViewer.Dispose();
            base.OnClosing(e);
        }
    }
}

