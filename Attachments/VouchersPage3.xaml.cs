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
using Uniconta.Common.Utility;
using UnicontaClient.Utilities;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.DataModel;
using Uniconta.API.GeneralLedger;
#if SILVERLIGHT
using System.Runtime.InteropServices.Automation;
#endif
using System.IO;
#if !SILVERLIGHT
using System.Diagnostics;
#endif
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class VouchersPage3 : FormBasePage
    {
        VouchersClient voucherClient;
        VouchersClient[] envelopes;
        int selectedIndex;
        object cache;

        public VouchersPage3(SynchronizeEntity master)
            : base(true, master)
        {
            this.DataContext = this;
            Initialize(master.Row);
        }

        void Initialize(UnicontaBaseEntity row)
        {
            cache = VoucherCache.HoldGlobalVoucherCache;
            InitializeComponent();
            if (Environment.OSVersion.Platform == PlatformID.MacOSX)
                ViewProgram.Visibility = Visibility.Collapsed;
            var corasauMaster = row;
            InitMaster(corasauMaster, true);
            this.BeforeClose += Vouchers_BeforeClose;
#if SILVERLIGHT
            if (!Application.Current.IsRunningOutOfBrowser)
                ViewProgram.Visibility = Visibility.Collapsed;
#endif
        }

        public VouchersPage3(UnicontaBaseEntity row)
            : base(row)
        {
            this.DataContext = this;
            Initialize(row);
        }

        void Vouchers_BeforeClose()
        {
            cache = null;
            envelopes = null;
            voucherClient = null;
            this.BeforeClose -= Vouchers_BeforeClose;
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            InitMaster(args, false);
        }

        private void InitMaster(UnicontaBaseEntity corasauMaster, bool setFocus)
        {
            if (corasauMaster == null)
                return;
            int RowId = VoucherCache.GetDocumentRowId(corasauMaster);
            if (RowId == 0)
            {
                NoVoucherLoadMessage();
                return;
            }

            if (this.voucherClient != null && this.voucherClient.RowId == RowId)
                return; // we have found the same as shown. Lets not redraw it.

            voucherClient = VoucherCache.GetGlobalVoucherCache(corasauMaster.CompanyId, RowId);
            _LoadInitMaster(corasauMaster, voucherClient, RowId, setFocus);
        }

        async private void _LoadInitMaster(UnicontaBaseEntity corasauMaster, VouchersClient voucherClient, int RowId, bool setFocus)
        {
            try
            {
                if (voucherClient == null)
                {
                    if (RowId != 0)
                    {
                        voucherClient = new VouchersClient();
                        voucherClient.SelectRowId(RowId);
                        // we will now enter api.read
                    }
                    else
                    {
                        busyIndicator.IsBusy = true;
                        var voucher = await api.Query<VouchersClient>(corasauMaster);
                        voucherClient = voucher?.FirstOrDefault();
                        if (voucherClient?._Data != null)
                            VoucherCache.SetGlobalVoucherCache(voucherClient);
                    }
                }

                if (voucherClient._Data == null)
                {
                    busyIndicator.IsBusy = true;
                    var result = await UtilDisplay.GetData(voucherClient, api);
                    if (result != 0)
                    {
                        busyIndicator.IsBusy = false;
                        UtilDisplay.ShowErrorCode(result);
                        return;
                    }
                }

                this.documentViewer.Children.Clear();

                brdMetaInfo.Visibility = Visibility.Visible;
                if (voucherClient._Envelope)
                {
                    btnPrev.IsEnabled = false;
                    busyIndicator.IsBusy = true;
                    var dapi = new DocumentAPI(api);
                    envelopes = (VouchersClient[])await dapi.GetEnvelopeContent(voucherClient, true);
                    gridPrevNext.Visibility = Visibility.Visible;
                    if (envelopes != null && envelopes.Length > 0)
                    {
                        totalBlk.Text = NumberConvert.ToString(envelopes.Length);
                        MoveToVoucherAtIndex(selectedIndex, setFocus);

                        /*
                        currentBlk.Text = NumberConvert.ToString(selectedIndex + 1);
                        var doc = envelopes[selectedIndex];
                        this.documentViewer.Children.Add(UtilDisplay.LoadControl(doc.Buffer, doc._Fileextension, false, setFocus));
                        */
                    }
                    else
                    {
                        totalBlk.Text = "0";
                        currentBlk.Text = "0";
                    }
                    busyIndicator.IsBusy = false;
                }
                else
                {
                    busyIndicator.IsBusy = false;
                    this.documentViewer.Children.Add(UtilDisplay.LoadControl(voucherClient, false, setFocus));
                }
            }
            catch (Exception ex)
            {
                brdMetaInfo.Visibility = Visibility.Visible;
                this.documentViewer.Children.Add((UtilDisplay.LoadDefaultControl(string.Format("{0}. {1} : {2}", Uniconta.ClientTools.Localization.lookup("InvalidDocSave"),
                    Uniconta.ClientTools.Localization.lookup("ViewerFailed"), ex.Message))));
                busyIndicator.IsBusy = false;
            }
            SetMetaInfo(voucherClient);
        }

        private void NoVoucherLoadMessage()
        {
            var child = this.documentViewer.Children;
            child.Clear();
            child.Add(UtilDisplay.LoadMessage(Uniconta.ClientTools.Localization.lookup("NoVoucherExist")));
            brdMetaInfo.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void SetMetaInfo(VouchersClient vc)
        {
            this.voucherClient = vc;
            if (vc != null)
            {
                metaInfoCtrl.SetlValues(vc._Text, vc.Created.ToString("g"), vc._Fileextension.ToString(), vc._UserName, vc.Content, vc.PostingInstruction, vc.Approver1, vc.Approver2);
                metaInfoCtrl.Visibility = Visibility.Visible;
            }
        }

        public override Type TableType { get { return typeof(VouchersClient); } }

        public override UnicontaBaseEntity ModifiedRow { get; set; }

        public override void OnClosePage(object[] refreshParams) { globalEvents.OnRefresh(NameOfControl, refreshParams); }

        public override string NameOfControl { get { return TabControls.VouchersPage3.ToString(); } }

        private void saveImage_Click(object sender, RoutedEventArgs e)
        {
            if (voucherClient == null)
                return;
            busyIndicator.IsBusy = true;
            if (voucherClient._Envelope)
            {
                VouchersClient vClient = envelopes[selectedIndex];
                UtilDisplay.SaveData(vClient.Buffer, vClient._Fileextension);
            }
            else
                UtilDisplay.SaveData(voucherClient.Buffer, voucherClient._Fileextension);
            busyIndicator.IsBusy = false;
        }

        private void cancelWindow_Click(object sender, RoutedEventArgs e)
        {
            frmRibbon_BaseActions("Cancel");
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            MoveToVoucherAtIndex(--selectedIndex);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            MoveToVoucherAtIndex(++selectedIndex);
        }

        async void MoveToVoucherAtIndex(int index, bool setFocus = false)
        {
            if (index < 0 || envelopes == null || index > envelopes.Length - 1)
                return;
            btnPrev.IsEnabled = btnNext.IsEnabled = true;
            if (selectedIndex == 0)
                btnPrev.IsEnabled = false;
            if (selectedIndex == envelopes.Length - 1)
                btnNext.IsEnabled = false;
            this.documentViewer.Children.Clear();
            currentBlk.Text = NumberConvert.ToString(index + 1);

            try
            {
                VouchersClient vClient = envelopes[selectedIndex];
                if (vClient._Data != null)
                    VoucherCache.SetGlobalVoucherCache(vClient);
                else
                    await UtilDisplay.GetData(vClient, api);

                this.documentViewer.Children.Add(UtilDisplay.LoadControl(vClient, false, setFocus));
            }
            catch (Exception ex)
            {
                brdMetaInfo.Visibility = System.Windows.Visibility.Visible;
                this.documentViewer.Children.Add((UtilDisplay.LoadDefaultControl(string.Format("{0}. \n{1} : {2}", Uniconta.ClientTools.Localization.lookup("InvalidDocSave"),
                    Uniconta.ClientTools.Localization.lookup("ViewerFailed"), ex.Message))));
            }
            SetMetaInfo(voucherClient);
        }

        private void ViewProgram_Click(object sender, RoutedEventArgs e)
        {
            if (voucherClient == null)
                return;
            busyIndicator.IsBusy = true;
            if (voucherClient._Envelope)
            {
                if (selectedIndex > envelopes.Length - 1)
                    return;
                VouchersClient vClient = envelopes[selectedIndex];
                if (vClient._Data != null)
                    ViewInProgram(vClient.Buffer, vClient._Fileextension);
            }
            else if (voucherClient._Data != null)
                ViewInProgram(voucherClient.Buffer, voucherClient._Fileextension);
            busyIndicator.IsBusy = false;
        }

        public static void ViewInProgram(byte[] voucherData, FileextensionsTypes voucherExtension)
        {
            if (voucherData == null || voucherData.Length == 0)
                return;
            string fileName = System.IO.Path.GetTempFileName() + "." + voucherExtension;
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(voucherData, 0, voucherData.Length);
                fs.Flush();
                fs.Close();
            }
#if SILVERLIGHT
            //for OOB 
            try
            {
                if (Application.Current.IsRunningOutOfBrowser)
                {
                    var shell = AutomationFactory.CreateObject("Shell.Application");
                    shell.ShellExecute(fileName, null, null, null, 1);
                }
            }
            catch (Exception ex)
            {
                BasePage.session.ReportException(ex, "VoucherPage3_OOB", 0);
            }
#endif
#if !SILVERLIGHT
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }
            catch (Exception excp)
            {
                BasePage.session.ReportException(excp, "VoucherPage3", 0);
            }
#endif
        }

        private void showAllFields_Click(object sender, RoutedEventArgs e)
        {
            var childWindow = new CWShowAllFields(voucherClient);
            childWindow.Show();
        }
    }
}
