using Uniconta.API.System;
using UnicontaClient.Pages;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.Common.Utility;
using System.Windows.Threading;

namespace UnicontaClient.Controls.Dialogs
{
    public class AttachedVouchersGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VouchersClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class CWAttachVouchers : ChildWindow
    {
        public int DocumentRef;
        public string DocumentInvoice;
        public DateTime VoucherDocumentDate;
        public VouchersClient Voucher;
        private List<int> _attachedVoucherList;
        private CrudAPI _api;
       
        public CWAttachVouchers(CrudAPI api, List<int> attachedVouchers)
        {
            DocumentRef = 0;
            _attachedVoucherList = attachedVouchers;
            InitializeComponent();
            InitControl(api);
            BindGrid(null);
            this.dgVoucherGrid.CustomUnboundColumnData += dgVoucherGrid_CustomUnboundColumnData;
            //colCreated.Visible = false;
            this.Loaded += CW_Loaded;
        }

        void dgVoucherGrid_RowDoubleClick()
        {
            AttachButton_Click(this, null);
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { AttachButton.Focus(); }));
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
                    if (AttachButton.IsFocused)
                        AttachButton_Click(null, null);
                    else if (CancelButton.IsFocused)
                        SetDialogResult(false);
                }
        }
        DispatcherTimer dt;
        VouchersClient selectedVoucherClient;
        void dgVoucherGrid_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            selectedVoucherClient = e.NewItem as VouchersClient;
            if (dt == null)
            {
                dt = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(250) };
                dt.Tick += delegate
                {
                    AttachVoucherGridPage.ShowVoucherOnTimer(dt, selectedVoucherClient, busyIndicator, documentViewer, _api);
                };
            }
            else if (dt.IsEnabled)
                dt.Stop();
            dt.Start();
        }

        public CWAttachVouchers(CrudAPI api)
        {
            InitializeComponent();
            InitControl(api);
            PropValuePair selectPair = PropValuePair.GenereteWhereElements("Fileextension", typeof(int), NumberConvert.ToString((int)FileextensionsTypes.CSV));
            var pair = new List<PropValuePair>() { selectPair };
            BindGrid(pair);
            colAttached.Visible = false;
        }

        private void InitControl(CrudAPI api)
        {
            this.DataContext = this;
            this.Title = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Attach"), Uniconta.ClientTools.Localization.lookup("Vouchers"));
            var masterList = new List<UnicontaBaseEntity>() { new Uniconta.DataModel.DocumentNoRef() };
            _api = api;
            dgVoucherGrid.api = _api;
            dgVoucherGrid.BusyIndicator = busyIndicator;
            this.dgVoucherGrid.masterRecords = masterList;
            this.dgVoucherGrid.View.DataControl.CurrentItemChanged += dgVoucherGrid_CurrentItemChanged;
            this.dgVoucherGrid.RowDoubleClick += dgVoucherGrid_RowDoubleClick;
        }

        private void BindGrid(IEnumerable<PropValuePair> propValuePair)
        {
            Filter(propValuePair);
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return this.dgVoucherGrid.Filter(propValuePair);
        }

        private void AttachButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRow = this.dgVoucherGrid.SelectedItem as VouchersClient;
            if (selectedRow == null)
                return;
            Voucher = selectedRow;
            DocumentRef = selectedRow.RowId;
            VoucherDocumentDate = selectedRow._DocumentDate;
            DocumentInvoice = selectedRow._Invoice;
            SetDialogResult(true);
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void dgVoucherGrid_CustomUnboundColumnData(object sender, DevExpress.Xpf.Grid.GridColumnDataEventArgs e)
        {
            if (e.IsGetData)
            {
                int rowId = Convert.ToInt32(e.GetListSourceFieldValue("RowId"));
                e.Value = _attachedVoucherList.Contains(rowId);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var root = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;

            if (root != null)
            {
                var contentRoot = root.FindName("ContentRoot") as FrameworkElement;
                if (contentRoot != null)
                {
                    var group = contentRoot.RenderTransform as TransformGroup;
                    if (group != null)
                    {
                        TranslateTransform transalteTransform = null;
                        foreach (var transform in group.Children.OfType<TranslateTransform>())
                        {
                            transalteTransform = transform;
                        }

                        if (transalteTransform != null)
                        {
                            transalteTransform.X = 0.0;
                            transalteTransform.Y = 0.0;
                        }
                    }
                }
            }
            return base.ArrangeOverride(finalSize);
        }
    }
}
