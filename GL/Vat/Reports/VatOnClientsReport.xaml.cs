using UnicontaClient.Pages;
using System;
using System.Collections;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VatOnClientsReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransDCSumClient); } }
        public override IComparer GridSorting { get { return new GLTransDCSumCmp(); } }

        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            base.PrintGrid(PrintReportName, printparam, format, page);
        }

        public string PrintReportName { get; set; }
    }

    public partial class VatOnClientsReport : GridBasePage
    {
        CWServerFilter filterDialog;
        bool filterCleared;
        UnicontaBaseEntity master;

        static DateTime firstDayOfMonth, lastDayOfMonth;
        public VatOnClientsReport(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }
        public VatOnClientsReport(UnicontaBaseEntity _master)
            : base(_master)
        {
            master = _master;
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgVatOnClients.UpdateMaster(master);
            localMenu.dataGrid = dgVatOnClients;
            SetRibbonControl(localMenu, dgVatOnClients);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            var strDCType = new string[] { Uniconta.ClientTools.Localization.lookup("Debtor"), Uniconta.ClientTools.Localization.lookup("Creditor") };
            cmbDCType.ItemsSource = strDCType;
            cmbDCType.SelectedIndex = 0;

            if (firstDayOfMonth == DateTime.MinValue)
            {
                DateTime date = BasePage.GetSystemDefaultDate();
                firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
                lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            }
            FromDate.DateTime = firstDayOfMonth;
            ToDate.DateTime = lastDayOfMonth;
            dgVatOnClients.ShowTotalSummary();
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor) });
        }

        public override Task InitQuery()
        {
            return null;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ItemFilter":
                    if (filterDialog == null)
                    {
                        if (filterCleared)
                            filterDialog = new CWServerFilter(api, typeof(GLTransDCSumClient), null, null);
                        else
                            filterDialog = new CWServerFilter(api, typeof(GLTransDCSumClient), null, null);
                        filterDialog.Closing += itemFilterDialog_Closing;
#if !SILVERLIGHT
                        filterDialog.Show();
                    }
                    else
                        filterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    filterDialog.Show();
#endif
                    break;

                case "ClearItemFilter":
                    filterDialog = null;
                    filterValues = null;
                    filterCleared = true;
                    break;
                case "Search":
                    LoadData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        IEnumerable<PropValuePair> filterValues;
        FilterSorter itemPropSort;

        void itemFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (filterDialog.DialogResult == true)
            {
                filterValues = filterDialog.PropValuePair;
                itemPropSort = filterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            filterDialog.Hide();
#endif
        }

        private void LoadData()
        {
            var FromStr = string.Format("{0:d}", FromDate.DateTime);
            var ToStr = string.Format("{0:d}", ToDate.DateTime);

            string reportHeaderContet;
            if (master != null)
            {
                var masterTable = master as GLAccountClient; 
                reportHeaderContet = string.Format("{0} {1} {2} {3} ({4} - {5})", Uniconta.ClientTools.Localization.lookup("VATprDC"), masterTable.Account, masterTable.Name, Uniconta.ClientTools.Localization.lookup("Report"), FromStr, ToStr);
            }
            else
            {
                reportHeaderContet = string.Format("{0} {1} ({2} - {3})", Uniconta.ClientTools.Localization.lookup("VATprDC"), Uniconta.ClientTools.Localization.lookup("Report"), FromStr, ToStr);
            }

            dgVatOnClients.PrintReportName = reportHeaderContet;
            var inputs = new List<PropValuePair>();
            inputs.Add(PropValuePair.GenereteParameter("FromDate", typeof(DateTime), FromStr));
            inputs.Add(PropValuePair.GenereteParameter("ToDate", typeof(DateTime), ToStr));
            inputs.Add(PropValuePair.GenereteParameter("DCType", typeof(string), Convert.ToString(cmbDCType.SelectedIndex+1)));

            if (filterValues != null)
                inputs.AddRange(filterValues);

            var t = Filter(inputs);
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgVatOnClients.Filter(propValuePair);
        }
    }
}
