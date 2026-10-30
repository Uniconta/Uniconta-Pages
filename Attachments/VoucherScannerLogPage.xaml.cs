using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VoucherScannerStatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (AppEnums.DocumentState.TryIndexOf(value as string))
            {
                case 1: return System.Windows.Media.Brushes.Green;
                case 2: return System.Windows.Media.Brushes.Yellow;
                case 3: return System.Windows.Media.Brushes.Red;
                default: return System.Windows.Media.Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Windows.Data.Binding.DoNothing;
        }
    }

    public class VoucherScannerLogSort : IComparer
    {
        public int Compare(object x, object y)
        {
            var xTag = Convert.ToString((x as VouchersClient)?.ErrorMessage);
            var yTag = Convert.ToString((y as VouchersClient)?.ErrorMessage);
            return string.Compare(xTag, yTag, StringComparison.Ordinal);
        }
    }

    public class VoucherScannerLogGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VouchersClient); } }
        public override IComparer GridSorting => new VoucherScannerLogSort();
        public override bool Readonly => false;
        public override bool SingleBufferUpdate => false;
        public override bool CanInsert => false;

        protected override IList ToList(UnicontaBaseEntity[] Arr)
        {
            return Arr.OfType<VouchersClient>()
                .Where(voucher => voucher._SentToScanner && !string.IsNullOrWhiteSpace(voucher.ErrorMessage))
                .ToList();
        }
    }

    public partial class VoucherScannerLogPage : GridBasePage
    {
        public override string NameOfControl => TabControls.VoucherScannerLogPage;

        public VoucherScannerLogPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            dgVoucherScannerLog.api = api;
            dgVoucherScannerLog.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgVoucherScannerLog);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private async void localMenu_OnItemClicked(string ActionType)
        {
            if (ActionType == "Delete")
            {
                var selectedItem = dgVoucherScannerLog.SelectedItem as VouchersClient;
                if (selectedItem == null)
                    return;

                dgVoucherScannerLog.SetLoadedRow(selectedItem);
                selectedItem.ErrorMessage = null;
                selectedItem.Status = AppEnums.DocumentState.ToString(0);

                var result = await dgVoucherScannerLog.SaveData();
                if (result == ErrorCodes.Succes)
                    await dgVoucherScannerLog.RefreshTask();
            }
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

