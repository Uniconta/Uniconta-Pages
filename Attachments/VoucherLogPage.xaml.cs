using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VoucherStatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (AppEnums.LineStatus.TryIndexOf(value as string))
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

    public class VoucherStatusVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values.Length == 2 && AppEnums.LineStatus.TryIndexOf(values[0] as string) > 0 &&
            !string.IsNullOrWhiteSpace(values[1] as string) ? Visibility.Visible : Visibility.Collapsed;

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    public class VoucherLogSort : IComparer
    {
        public int Compare(object x, object y)
        {
            var xTag = Convert.ToString((x as VouchersClient)?.ErrorMessage);
            var yTag = Convert.ToString((y as VouchersClient)?.ErrorMessage);
            return string.Compare(xTag, yTag, StringComparison.Ordinal);
        }
    }

    public class VoucherLogGrid : CorasauDataGridClient
    {
        internal int? VoucherRowId { get; set; }
        public override Type TableType { get { return typeof(VouchersClient); } }
        public override IComparer GridSorting => new VoucherLogSort();
        public override bool Readonly => false;
        public override bool SingleBufferUpdate => false;
        public override bool CanInsert => false;

        protected override IList ToList(UnicontaBaseEntity[] Arr)
        {
            return Arr.OfType<VouchersClient>()
                .Where(voucher => VoucherLogPage.HasMessage(voucher) &&
                    (!VoucherRowId.HasValue || voucher.RowId == VoucherRowId.Value))
                .ToList();
        }
    }

    public partial class VoucherLogPage : GridBasePage
    {
        public override string NameOfControl => TabControls.VoucherLogPage;
        public override bool ShowAllFieldsMenu => false;

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = false;
            return false;
        }

        public VoucherLogPage(BaseAPI API) : this(API, null)
        {
        }

        public VoucherLogPage(BaseAPI API, VouchersClient voucher)
            : base(API, string.Empty)
        {
            InitializeComponent();
            dgVoucherLog.VoucherRowId = voucher?.RowId;
            dgVoucherLog.api = api;
            dgVoucherLog.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgVoucherLog);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        internal static bool HasMessage(VouchersClient voucher) =>
            voucher != null && voucher._Status != DocumentStatus.Unknown && !string.IsNullOrWhiteSpace(voucher.ErrorMessage);

        protected override Filter[] DefaultFilters()
        {
            if (dgVoucherLog.VoucherRowId.HasValue)
                return new[] { new Filter { name = "RowId", value = dgVoucherLog.VoucherRowId.Value.ToString(CultureInfo.InvariantCulture), parameterType = typeof(int) } };
            return base.DefaultFilters();
        }

        private async void localMenu_OnItemClicked(string ActionType)
        {
            if (ActionType == "Delete")
            {
                var selectedItem = dgVoucherLog.SelectedItem as VouchersClient;
                if (selectedItem == null)
                    return;

                dgVoucherLog.SetLoadedRow(selectedItem);
                await ClearStatus(selectedItem, dgVoucherLog.SaveData, async () =>
                {
                    globalEvents.OnRefresh(NameOfControl, selectedItem);
                    await dgVoucherLog.RefreshTask();
                });
            }
            if (ActionType == "RefreshGrid")
                gridRibbon_BaseActions(ActionType);
        }

        internal static async Task<ErrorCodes> ClearStatus(VouchersClient voucher, Func<Task<ErrorCodes>> save, Func<Task> refresh)
        {
            voucher.ErrorMessage = null;
            voucher.Status = AppEnums.LineStatus.ToString(0);
            var result = await save();
            if (result == ErrorCodes.Succes)
                await refresh();
            return result;
        }

        protected override void OnLayoutLoaded()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var clearStatus = UtilDisplay.GetMenuCommandByName(rb, "Delete");
            clearStatus.Caption = string.Format(Localization.lookup("ClearOBJ"), Localization.lookup("Status"));
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "AddRow", "EditRow", "SaveGrid", "Layout" });
            base.OnLayoutLoaded();

            // Saved layouts can restore columns that no longer belong to this view.
            var logColumns = new[] { StatusIndicator, Status, PrimaryKeyId, Created, UserName, Text };
            foreach (var column in dgVoucherLog.Columns.ToArray())
                if (!logColumns.Any(logColumn => ReferenceEquals(logColumn, column)))
                    dgVoucherLog.Columns.Remove(column);

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

