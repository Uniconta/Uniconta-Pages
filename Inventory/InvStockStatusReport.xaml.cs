using Corasau.Client.Pages;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System.ComponentModel;

namespace Uniconta.WPFClient.Pages
{
    public class InvStockStatusReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvStockStatusReportGridClient); } }

        protected override IList ToList(UnicontaBaseEntity[] Arr)
        {
            return ((InvStockStatusReportGridClient[])Arr).ToList();
        }

        public override bool Readonly { get { return false; } }
    }

    public class InvStockStatusReportGridClient : InvItemClient
    {
        public double _Quantity;
        [Display(Name = "Qty", ResourceType = typeof(DCOrderText))]
        public double Quantity { get { return _Quantity; } set { _Quantity = value; NotifyPropertyChanged("Quantity"); NotifyPropertyChanged("Difference"); } }

        [Display(Name = "Difference", ResourceType = typeof(DCOrderText))]
        public double Difference { get { return _Quantity - _Qty; } }

    }

    public partial class InvStockStatusReport : GridBasePage
    {
        
        public InvStockStatusReport()
            : base(null)
        {
            InitPage(null);
        }
        public InvStockStatusReport(UnicontaBaseEntity _master)
            : base(_master)
        {
            InitPage(_master);
        }

        public override bool IsDataChaged { get { return false; } }

        private void InitPage(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            dgInvStockStatus.UpdateMaster(_master);
            dgInvStockStatus.ShowTotalSummary();
            localMenu.dataGrid = dgInvStockStatus;
            SetRibbonControl(localMenu, dgInvStockStatus);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            var t = dgInvStockStatus.Filter(null);
            StartLoadCache(t);
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.InvJournal));
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvStockStatus.SelectedItem as InvStockStatusReportGridClient;
            switch (ActionType)
            {
                case "DeleteRow":
                    if (selectedItem == null) return;
                    dgInvStockStatus.DeleteRow();
                    break;
                case "PostJournal":
                    CwInvJournal journals = new CwInvJournal(api, true);
                    journals.Closed += delegate
                    {
                        if (journals.DialogResult == true)
                        {
                            PostJournal(journals.InvJournal, journals.Date);
                        }
                    };
                    journals.Show();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void PostJournal(InvJournalClient invJournal,DateTime date)
        {
            var mainList = (IList<InvStockStatusReportGridClient>)dgInvStockStatus.ItemsSource;
            var invJournalLineList = new List<InvJournalLineClient>();
            foreach (var item in mainList)
            {
                var journalLine = new InvJournalLineClient();

                journalLine._Date = date;
                journalLine._MovementType = 2; // counting
                journalLine._Item = item._Item;
                journalLine._Qty = item.Difference;
                
                journalLine.SetMaster(invJournal);
                invJournalLineList.Add(journalLine);
            }

            var result = await api.Insert(invJournalLineList);

            UtilDisplay.ShowErrorCode(result);
        }
    }
}
