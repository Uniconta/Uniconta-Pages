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
            BeforeClose += AttachVouchers_BeforeClose;
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
            position = GetPosition();
            BeforeClose -= AttachVouchers_BeforeClose;
        }

        private Point GetPosition()
        {
            var floatGrp = GetFloatGroup();
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

        private void InitControl()
        {
            dgAttachVouchers.api = api;
            dgAttachVouchers.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgAttachVouchers);
            dgAttachVouchers.UpdateMaster(new Uniconta.DataModel.DocumentNoRef());
        }

        private void AttachVoucher()
        {
            var selectedVouher = dgAttachVouchers.SelectedItem as VouchersClient;
            if (selectedVouher != null)
            {
                attachParams = new object[] { selectedVouher };
                CloseDockItem();
            }
        }

        private void Close()
        {
            attachParams = null;
            CloseDockItem();
        }

        async private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var selectedVoucherClient = e.NewItem as VouchersClient;
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
            var winSetting = BasePage.session.Preference.GetWindowSetting(TabControls.AttachVoucherGridPage);
            if (winSetting == null)
            {
                var currPanel = dockCtrl?.Activpanel;
                if (currPanel != null && currPanel.IsFloating)
                {
                    SetFloatGroup();
                    currPanel.Parent.FloatSize = new Size(pageWidth, pageHeight);
                    currPanel.SizeChanged += CurrPanel_SizeChanged;
                    currPanel.UpdateLayout();
                }
            }
            base.OnLayoutLoaded();
        }

        private void SetFloatGroup()
        {
            var floatgrp = GetFloatGroup();
            if (floatgrp != null)
                floatgrp.FloatLocation = position;
        }

        private FloatGroup GetFloatGroup()
        {
            var curPanel = dockCtrl?.Activpanel;

            if (curPanel != null && curPanel.IsFloating && curPanel.Parent is FloatGroup)
                return curPanel.Parent as FloatGroup;

            return null;
        }

        private void CurrPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var size = e.NewSize;
            pageHeight = size.Height;
            pageWidth = size.Width;
        }
    }
}
