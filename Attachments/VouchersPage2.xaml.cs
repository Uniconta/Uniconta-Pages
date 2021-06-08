using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Models;
using Uniconta.Common;
using Uniconta.API.System;
using System.IO;
using UnicontaClient.Utilities;
using Uniconta.Common.Utility;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Util;
using System.Threading.Tasks;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class VouchersPage2 : FormBasePage
    {
        VouchersClient voucherClientRow;
        VouchersClient[] multiVouchers;
        bool isFieldsAvailableForEdit;
        string viewInbin = string.Empty;

        public override Type TableType { get { return typeof(VouchersClient); } }

        public override string NameOfControl { get { return TabControls.VouchersPage2; } }
        public override UnicontaBaseEntity ModifiedRow { get { return voucherClientRow; } set { voucherClientRow = (VouchersClient)value; } }
        SQLCache PaymentCache, LedgerCache, GLTransTypeCache;

        public VouchersPage2(UnicontaBaseEntity sourcedata, bool isEdit)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            isFieldsAvailableForEdit = isEdit;

            InitPage(api);
        }

        public VouchersPage2(CrudAPI crudApi, string viewInBin)
            : base(crudApi, viewInBin)
        {
            InitializeComponent();
            viewInbin = viewInBin;
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(cmbContentTypes, cmbContentTypes);
#endif
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        private void InitPage(CrudAPI crudApi)
        {
            leAccount.api = lePayAccount.api = leCostAccount.api = leVat.api = leVatOpenration.api = leTransType.api = leApprover1.api = leApprover2.api = leProject.api = leProjectcat.api = crudApi;
            layoutControl = layoutItems;
            cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = crudApi;

            if (LoadedRow == null)
                frmRibbon.DisableButtons("Delete");

            var Comp = api.CompanyEntity;
            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(crudApi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (LoadedRow == null)
                voucherClientRow = CreateNew() as VouchersClient;

            if (!string.IsNullOrEmpty(viewInbin))
                voucherClientRow.ViewInFolder = viewInbin;

            if (!api.CompanyEntity._UseVatOperation)
                VatOPerationItem.Visibility = Visibility.Collapsed;
            if (!api.CompanyEntity.Project)
            {
                ProjectItem.Visibility = Visibility.Collapsed;
                ProjectCategoryItem.Visibility = Visibility.Collapsed;
            }

            if (isFieldsAvailableForEdit)
                browseCtrlColumn.Visibility = Visibility.Collapsed;
            BusyIndicator = busyIndicator;
            layoutItems.DataContext = voucherClientRow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

#if !SILVERLIGHT
            browseControl.CompressVisibility = Visibility.Visible;
            browseControl.PDFSplitVisibility = Visibility.Visible;
            txtPurchaseNumber.CrudApi = api;
            txtUrl.LostFocus += txtUrl_LostFocus;
#endif
            voucherClientRow.PropertyChanged += VoucherClientRow_PropertyChanged;

            PaymentCache = Comp.GetCache(typeof(Uniconta.DataModel.PaymentTerm));
            StartLoadCache();
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            this.LedgerCache = api.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            if (PaymentCache == null)
                PaymentCache = await api.LoadCache(typeof(Uniconta.DataModel.PaymentTerm)).ConfigureAwait(false);
            this.GLTransTypeCache = api.GetCache(typeof(Uniconta.DataModel.GLTransType)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLTransType)).ConfigureAwait(false);
            LoadType(new Type[] { typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLVat) });
        }

        private void VoucherClientRow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as VouchersClient;

            var prop = e.PropertyName;
            if (prop == "DocumentDate" || prop == "Date" || prop == "CreditorAccount")
            {
                var cre = rec.Creditor;
                if (cre != null)
                {
                    UnicontaClient.Pages.Vouchers.SetPayDate(PaymentCache, rec, cre);
                    if (prop == "CreditorAccount")
                    {
                        rec.PaymentMethod = cre.PaymentMethod;
                        if (cre._PostingAccount != null)
                            rec.CostAccount = cre._PostingAccount;
                    }
                }
            }
            else if (prop == "CostAccount")
            {
                var Acc = (Uniconta.DataModel.GLAccount)LedgerCache?.Get(rec._CostAccount);
                if (Acc == null)
                    return;
                if (Acc._PrCategory != null)
                    rec.PrCategory = Acc._PrCategory;
                if (Acc._MandatoryTax == VatOptions.NoVat)
                {
                    rec.Vat = null;
                    rec.VatOperation = null;
                }
                else
                {
                    rec.Vat = Acc._Vat;
                    rec.VatOperation = Acc._VatOperation;
                }
                if (Acc._DefaultOffsetAccount != null)
                {
                    if (Acc._DefaultOffsetAccountType == GLJournalAccountType.Creditor)
                        rec.CreditorAccount = Acc._DefaultOffsetAccount;
                    else if (Acc._DefaultOffsetAccountType == GLJournalAccountType.Finans)
                        rec.PayAccount = Acc._DefaultOffsetAccount;
                }
            }
            else if (prop == "TransType")
            {
                var glTransTYpe = (Uniconta.DataModel.GLTransType)GLTransTypeCache?.Get(rec._TransType);
                if (glTransTYpe != null)
                {
                    if (glTransTYpe._AccountType == GLJournalAccountType.Finans && glTransTYpe._Account != null)
                    {
                        rec.Text = glTransTYpe._TransType;
                        if (glTransTYpe._AccountType == 0 && glTransTYpe._Account != null)
                            rec.CostAccount = glTransTYpe._Account;
                        if (glTransTYpe._OffsetAccount != null)
                        {
                            if (glTransTYpe._OffsetAccountType == 0)
                                rec.PayAccount = glTransTYpe._OffsetAccount;
                            else if (glTransTYpe._OffsetAccountType == GLJournalAccountType.Creditor)
                                rec.CreditorAccount = glTransTYpe._OffsetAccount;
                        }
                    }
                }
            }
        }

        async void SaveIn2Steps()
        {
            // This here will first save the record without attachment. Then it will update with attachement.
            // The update will be done without await, so it will be done in the background.
            // This way it is fast to upload a document, since the user will not have to wait.
            var multiVouchers = this.multiVouchers;
            if (multiVouchers != null && browseControl.IsMultiSelect)
            {
                int l = multiVouchers.Length;
                if (l > 1)
                {
                    var buffers = new byte[l][];
                    for (int i = 0; (i < l); i++)
                    {
                        var voucher = multiVouchers[i];
                        if (voucher._Data != null)
                        {
#if !SILVERLIGHT
                            if (voucher._Fileextension == FileextensionsTypes.JPEG)
                            {
                                var imageBytes = FileBrowseControl.ImageResize(voucher._Data, ".jpg");
                                if (imageBytes != null)
                                    voucher._Data = imageBytes;
                            }
#endif
                            buffers[i] = voucher._Data;
                            voucher._Data = null;
                        }
                    }
                    var err = await api.Insert(multiVouchers);
                    if (err != ErrorCodes.Succes)
                    {
                        for (int i = 0; (i < l); i++)
                            multiVouchers[i]._Data = buffers[i];
                        ClearBusy();
                        UtilDisplay.ShowErrorCode(err);
                    }
                    else
                    {
                        Utility.UpdateBuffers(api, buffers, multiVouchers);
                        ClosePage(4); // full refresh and clearBusy inside
                        CloseDockItem();
                    }
                    return;
                }
                voucherClientRow = multiVouchers[0];
            }
            byte[] buf = null;
            if (LoadedRow == null)
            {
                buf = voucherClientRow._Data;
#if !SILVERLIGHT
                if (buf != null && voucherClientRow._Fileextension == FileextensionsTypes.JPEG)
                {
                    buf = FileBrowseControl.ImageResize(buf, ".jpg");
                    if (buf != null)
                        voucherClientRow._Data = buf;
                    else
                        buf = voucherClientRow._Data;
                }
#endif
                if (buf != null && buf.Length > 200000)
                    voucherClientRow._Data = null;
                else
                    buf = null;
            }

            await saveForm();
            ClearBusy();

            if (buf != null)
            {
                if (voucherClientRow.RowId != 0)
                    Utility.UpdateBuffers(api, new[] { buf }, new[] { voucherClientRow });
                else
                    voucherClientRow._Data = buf;
            }
            else
                VoucherCache.SetGlobalVoucherCache(voucherClientRow);
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                busyIndicator.IsBusy = true;
                if (!ValidateSave())
                {
                    busyIndicator.IsBusy = false;
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoFilesSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
                }
                else
                    SaveIn2Steps();
            }
            else
                frmRibbon_BaseActions(ActionType);
        }

        private bool ValidateSave()
        {
            bool isSucess = false;
#if !SILVERLIGHT
            if (browseControl?.FileName == null && !string.IsNullOrEmpty(voucherClientRow._Url))
            {
                isSucess = true;
                var url = voucherClientRow._Url;
                var fileExt = voucherClientRow._Fileextension;
                var fileName = voucherClientRow._Text;

                if (!isFileExtManualSet)
                {
                    if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || url.StartsWith("www", StringComparison.OrdinalIgnoreCase))
                    {
                        int indexOfExtention = voucherClientRow._Url.LastIndexOf('.');
                        var ext = DocumentConvert.GetDocumentType(url.Substring(indexOfExtention, url.Length - indexOfExtention));
                        fileExt = ext != FileextensionsTypes.UNK ? ext : FileextensionsTypes.WWW;
                    }
                    else if (!Utility.TryParseUrl(url, false, ref fileName, ref fileExt))
                    {
                        isSucess = false;
                        return isSucess;
                    }
                }
                voucherClientRow._Fileextension = fileExt;
                voucherClientRow._Text = fileName;

                return isSucess;
            }
#endif
            if (browseControl.IsMultiSelect)
            {
#if SILVERLIGHT
                var lst = browseControl.SelectedFileInfos;
#else
                var lst = browseControl.Split ? browseControl.LoadFileInfosBySplittingPDF() : browseControl.SelectedFileInfos;
#endif
                if (lst == null && string.IsNullOrEmpty(voucherClientRow.Url))
                    return false;
                var fileCount = lst.Count();
                if (fileCount > 0)
                {
                    multiVouchers = new VouchersClient[fileCount];
                    var iCtr = 0;
                    foreach (var fileInfo in lst)
                    {
                        if (fileInfo == null)
                            continue;
                        var vc = Activator.CreateInstance(voucherClientRow.GetType()) as VouchersClient;
                        vc.SetMaster(api.CompanyEntity);
                        multiVouchers[iCtr++] = vc;
                        vc._Fileextension = DocumentConvert.GetDocumentType(fileInfo.FileExtension);
#if !SILVERLIGHT
                        if (chkIncludeOnlyReference.IsChecked == true)
                            vc._Url = fileInfo.FilePath;
                        else
#endif
                            vc._Data = fileInfo.FileBytes;
                        var text = txedVoucherComments.Text;
                        if (string.IsNullOrEmpty(text))
                        {
                            text = fileInfo.FileName;
                            if (text != null)
                            {
                                // remove extension
                                var ext = vc._Fileextension.ToString();
                                if (text.EndsWith(ext, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    var len = text.Length - ext.Length - 1; // remove the .
                                    if (len > 0)
                                        text = text.Substring(0, len);
                                }
                                text = text.Replace('_', ' ').Trim();
                            }
                        }
                        vc._Text = text;
                        FillVoucherInfo(vc);
                    }
                    isSucess = true;
                }
            }
            else
            {
                string fileExtension = browseControl.FileExtension;
                byte[] fileBytes = browseControl.FileBytes;
                if (fileBytes != null)
                {
#if !SILVERLIGHT
                    if (chkIncludeOnlyReference.IsChecked == true)
                        voucherClientRow.Url = browseControl.FilePath;
                    else
#endif
                        voucherClientRow._Data = fileBytes;
                    voucherClientRow.Fileextension = DocumentConvert.GetDocumentType(fileExtension);
                }
                else if (!string.IsNullOrEmpty(voucherClientRow.Url))
                    isSucess = true;
                FillVoucherInfo(voucherClientRow);
                voucherClientRow.Text = ValidateComment(txedVoucherComments.Text);
            }
            return isSucess;
        }

        private void FillVoucherInfo(VouchersClient vc)
        {
            vc.Content = Convert.ToString(cmbContentTypes.SelectedItemValue);
            vc._Amount = NumberConvert.ToDouble(txtAmount.Text);
            vc._Qty = NumberConvert.ToDouble(txtQty.Text);
            if (!string.IsNullOrWhiteSpace(txtInvoice.Text))
                vc._Invoice = txtInvoice.Text;
            vc._DocumentDate = txtDocumentDate.DateTime;
            vc.Currency = Convert.ToString(cmbCurrency.SelectedItem);
            vc.CreditorAccount = leAccount.Text;
            vc.PayDate = txtPaymentDate.DateTime;
            vc.CostAccount = leCostAccount.Text;
            vc.PayAccount = lePayAccount.Text;
            vc.Vat = leVat.Text;
            vc.VatOperation = leVatOpenration.Text;
            vc.TransType = leTransType.Text;
            vc._Approved = (bool)chkApproved.IsChecked;
            vc._Approver1 = leApprover1.Text;
            vc._Approver2 = leApprover2.Text;
            vc.PaymentId = txtPaymentId.Text;
            vc.Project = leProject.Text;
            vc.PrCategory = leProjectcat.Text;
            vc._PostingInstruction = txedPostingInstruction.Text;
            vc.PaymentMethod = Convert.ToString(cmbPaymentMethod.SelectedItemValue);
#if !SILVERLIGHT
            vc.ViewInFolder = Convert.ToString(cmbViewInFolder.SelectedItem);
            vc._PurchaseNumber = GetPurchaseNumber();
#endif
            if (lbldim1.Visibility == Visibility.Visible)
                vc.Dimension1 = cmbDim1.Text;
            if (lbldim2.Visibility == Visibility.Visible)
                vc.Dimension2 = cmbDim2.Text;

            if (lbldim3.Visibility == Visibility.Visible)
                vc.Dimension3 = cmbDim3.Text;

            if (lbldim4.Visibility == Visibility.Visible)
                vc.Dimension4 = cmbDim4.Text;

            if (lbldim5.Visibility == Visibility.Visible)
                vc.Dimension5 = cmbDim5.Text;
        }

        private string ValidateComment(string comment)
        {
            return string.IsNullOrEmpty(comment) ? browseControl.FileName : comment;
        }
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }

#if !SILVERLIGHT

        private int GetPurchaseNumber()
        {
            int purchaseNumber;
            var lookupValueStr = Convert.ToString(txtPurchaseNumber.LookupEditValue);

            if (lookupValueStr != null && int.TryParse(lookupValueStr, out purchaseNumber))
                return purchaseNumber;

            return 0;
        }

        string browseUrl;
        private void browseControl_FileSelected()
        {
            if (browseControl.SelectedFileInfos?.Length == 1)
            {
                var selectedFileInfo = browseControl.SelectedFileInfos[0];
                browseUrl = selectedFileInfo.FilePath;
                if (chkIncludeOnlyReference.IsChecked == true)
                    voucherClientRow.Url = browseUrl;
            }
        }

        private void chkIncludeOnlyReference_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            browseControl.CheckMaxSize = !chkIncludeOnlyReference.IsChecked.Value;
            if (browseControl.SelectedFileInfos?.Length == 1)
            {
                if (chkIncludeOnlyReference.IsChecked == true)
                {
                    if (!string.IsNullOrEmpty(browseUrl))
                        voucherClientRow.Url = browseUrl;
                }
                else
                    voucherClientRow.Url = null;
            }
        }

        bool isFileExtManualSet;
        private void cmbFileextensionType_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            FileextensionsTypes updatedFileExt;
            if (e.OldValue != e.NewValue && Enum.TryParse(Convert.ToString(e.NewValue), out updatedFileExt) && voucherClientRow.Fileextension != updatedFileExt)
            {
                string comments = voucherClientRow._Text;
                if (Utility.TryParseUrl(voucherClientRow._Url, false, ref comments, ref updatedFileExt))
                {
                    voucherClientRow.Fileextension = updatedFileExt;
                    voucherClientRow.Text = comments;
                    isFileExtManualSet = true;
                }
            }
        }

        private void txtUrl_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(voucherClientRow._Url))
                liFileextension.Visibility = Visibility.Collapsed;
            else
            {
                liFileextension.Visibility = Visibility.Visible;
                voucherClientRow.Fileextension = FileextensionsTypes.WWW;
            }
        }

        /// <summary>
        /// Loads Voucher page2 with the files dropped
        /// </summary>
        /// <param name="filePaths"></param>
        public void LoadPageOnFileDrop(string[] filePaths)
        {
            browseControl.Visibility = Visibility.Collapsed;
            try
            {
                List<string> failedFiles = null;
                var fileInfos = UtilDisplay.CreateSelectedFileInfo(filePaths, out failedFiles);
                if (failedFiles != null && failedFiles.Count > 0)
                {
                    var cwErrorBox = new CWErrorBox(failedFiles.ToArray(), true);
                    cwErrorBox.Show();
                }

                txedVoucherComments.Text = fileInfos.Length > 1 ? string.Empty : fileInfos.SingleOrDefault()?.FileName;
                browseControl.SelectedFileInfos = fileInfos.Length > 0 ? fileInfos : null;
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("FileError"), ex.Message), Uniconta.ClientTools.Localization.lookup("Exception"));
            }
        }

        /// <summary>
        /// Load files when outlook mail dropped
        /// </summary>
        /// <param name="dataObject">Dropped data object</param>
        public void LoadPageOnOutlookMailDrop(IDataObject dataObject)
        {
            try
            {
                browseControl.Visibility = Visibility.Collapsed;
                string error;
                var selectedFileInfo = UtilDisplay.CreateSelectedFileInfo(dataObject, out error);
                if (string.IsNullOrEmpty(error) && selectedFileInfo != null)
                {
                    txedVoucherComments.Text = selectedFileInfo.FileName;
                    browseControl.SelectedFileInfos = new SelectedFileInfo[] { selectedFileInfo };
                }
                else
                    UnicontaMessageBox.Show(error, Uniconta.ClientTools.Localization.lookup("Error"));
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("FileError"), ex.Message), Uniconta.ClientTools.Localization.lookup("Exception"));
            }
        }
#endif
    }
}
