using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Models;
using Uniconta.API.Service;
using DevExpress.Xpf.Docking;
using System.Windows.Threading;
using Uniconta.API.System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for AttachVouchers.xaml
    /// </summary>
    public partial class AttachVoucherGridPage : GridBasePage
    {
        private List<int> _attachedVoucherList;
        private bool addFilter;
        private object[] attachParams;
        object cache;
        static double pageHeight = 650.0d, pageWidth = 850.0d;
        static Point position = new Point();
        public AttachVoucherGridPage(List<int> attachedVouchers) : base(null)
        {
            InitializeComponent();
            _attachedVoucherList = attachedVouchers;
            cache = VoucherCache.HoldGlobalVoucherCache;
            InitControl();
            dgAttachVouchers.CustomUnboundColumnData += DgAttachVouchers_CustomUnboundColumnData;
            dgAttachVouchers.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Attach":
                    AttachVoucher();
                    break;
                case "Cancel":
                    Close();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void AttachVouchers_BeforeClose()
        {
            attachParams = null;
            position = GetPosition(dockCtrl);
            voucherViewer.Dispose();
            BeforeClose -= AttachVouchers_BeforeClose;
        }

        public static Point GetPosition(DockControl dockControl)
        {
            var floatGrp = GetFloatGroup(dockControl);
            if (floatGrp == null)
                return new Point();
            return floatGrp.FloatLocation;
        }
        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, attachParams);
            base.PageClosing();
        }
        protected override Filter[] DefaultFilters()
        {
            if (addFilter)
            {
                Filter fileExtensionFilter = new Filter();
                fileExtensionFilter.name = "Fileextension";
                fileExtensionFilter.value = Convert.ToString((int)FileextensionsTypes.CSV);
                return new Filter[] { fileExtensionFilter };
            }
            return base.DefaultFilters();
        }

        public override string NameOfControl { get { return TabControls.AttachVoucherGridPage; } }

        public AttachVoucherGridPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            InitControl();
            colAttached.Visible = colAttached.ShowInColumnChooser = false;
            addFilter = true;
        }
        bool showAllDocs;
        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (rec.Name == null || rec.Name == "ShowAll")
                {
                    if (int.Parse(rec.Value) == 1)
                        dgAttachVouchers.masterRecords = null;
                }
            }
            base.SetParameter(Parameters);

        }
        private void InitControl()
        {
            dgAttachVouchers.api = api;
            dgAttachVouchers.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgAttachVouchers);
            dgAttachVouchers.UpdateMaster(new Uniconta.DataModel.DocumentNoRef());
            BeforeClose += AttachVouchers_BeforeClose;
        }

        private void AttachVoucher()
        {
            var selectedVouher = dgAttachVouchers.SelectedItem as VouchersClient;
            if (selectedVouher != null)
            {
                attachParams = new object[] { selectedVouher, backTab };
                CloseDockItem();
            }
        }

        private void Close()
        {
            attachParams = null;
            CloseDockItem();
        }

        DispatcherTimer dt;
        VouchersClient selectedVoucherClient;
        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs ce)
        {
            selectedVoucherClient = ce.NewItem as VouchersClient;
            if (dt == null)
            {
                dt = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(250) };
                dt.Tick += delegate
                {
                    ShowVoucherOnTimer(dt, selectedVoucherClient, busyIndicator, voucherViewer, api);
                };
            }
            else if (dt.IsEnabled)
                dt.Stop();
            dt.Start();
        }

        public static async void ShowVoucherOnTimer(DispatcherTimer dt, VouchersClient selectedVoucherClient, BusyIndicator busyIndicator, UnicontaVoucherViewer voucherViewer, CrudAPI api)
        {
            dt?.Stop();
            if (selectedVoucherClient != null)
            {
                busyIndicator.IsBusy = true;
                try
                {
                    if (selectedVoucherClient._Data == null)
                        await UtilDisplay.GetData(selectedVoucherClient, api);

                    if (selectedVoucherClient._Fileextension != FileextensionsTypes.DIR)
                    {
                        voucherViewer.HasMultipleVouchers = false;
                        voucherViewer.Vouchers = new VouchersClient[] { selectedVoucherClient };
                    }
                    else
                    {
                        var dapi = new Uniconta.API.GeneralLedger.DocumentAPI(api);
                        voucherViewer.HasMultipleVouchers = true;
                        voucherViewer.Vouchers = (VouchersClient[])await dapi.GetEnvelopeContent(selectedVoucherClient, true);
                    }

                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                }
                finally { busyIndicator.IsBusy = false; }
            }
        }

        private void DgAttachVouchers_CustomUnboundColumnData(object sender, DevExpress.Xpf.Grid.GridColumnDataEventArgs e)
        {
            if (e.IsGetData)
            {
                int rowId = Convert.ToInt32(e.GetListSourceFieldValue("RowId"));
                e.Value = _attachedVoucherList.Contains(rowId);
                if (_attachedVoucherList.Contains(rowId))
                    dgAttachVouchers.View.FocusedRowHandle = e.ListSourceRowIndex;
            }
        }

        protected override void OnLayoutLoaded()
        {
            SetFloatingHeightAndWidth(dockCtrl, position, TabControls.AttachVoucherGridPage);
            base.OnLayoutLoaded();
        }
        public static void SetFloatingHeightAndWidth(DockControl dockCtrl, Point position, string nameOfControl)
        {
            var winSetting = BasePage.session.Preference.GetWindowSetting(nameOfControl);
            if (winSetting == null)
            {
                var currPanel = dockCtrl?.Activpanel;
                if (currPanel != null && currPanel.IsFloating)
                {
                    SetFloatGroup(dockCtrl, position);
                    currPanel.Parent.FloatSize = new Size(pageWidth, pageHeight);
                    currPanel.SizeChanged += delegate (object sender, SizeChangedEventArgs e)
                    {
                        var size = e.NewSize;
                        pageHeight = size.Height;
                        pageWidth = size.Width;
                    };

                    currPanel.UpdateLayout();
                }
            }
        }
        static void SetFloatGroup(DockControl dockCtrl, Point position)
        {
            var floatgrp = GetFloatGroup(dockCtrl);
            if (floatgrp != null)
                floatgrp.FloatLocation = position;
        }

        static FloatGroup GetFloatGroup(DockControl dockCtrl)
        {
            var curPanel = dockCtrl?.Activpanel;

            if (curPanel != null && curPanel.IsFloating && curPanel.Parent is FloatGroup)
                return curPanel.Parent as FloatGroup;

            return null;
        }
    }
}
