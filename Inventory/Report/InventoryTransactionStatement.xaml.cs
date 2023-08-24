using UnicontaClient.Models;
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
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.API.Inventory;
using DevExpress.Xpf.Grid;
using System.ComponentModel.DataAnnotations;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using Uniconta.API.System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class InvItemTransStatementList
    {
        [Display(Name = "Item", ResourceType = typeof(InventoryText))]
        public string ItemNumber { get; set; }

        [Display(Name = "Name", ResourceType = typeof(InventoryText))]
        public string Name { get; set; }

        [Display(Name = "ItemType", ResourceType = typeof(InventoryText))]
        public string ItemType { get; set; }

        [Display(Name = "CostValue", ResourceType = typeof(InvTransText))]
        public double SumCost { get { return _SumCost; } }

        [Display(Name = "SumQty", ResourceType = typeof(InvTransText))]
        public double SumQty { get { return _SumQty; } }

        [Display(Name = "UseSerialBatch", ResourceType = typeof(InventoryText))]
        public string SerialNumbers { get { return _SerialNumbers; } }

        public List<InvSeriesBatchTotal> ChildRecord { get; set; }

        public double _SumCost;
        public double _SumQty;
        public string _SerialNumbers;
    }

    public class InvSeriesBatchTotal : InvSerieBatchClient
    {
        public double _SumCost;
        public double _SumQty;

        [Display(Name = "SumCost", ResourceType = typeof(InvTransText))]
        public double SumCost { get { return _SumCost; } }

        [Display(Name = "SumQty", ResourceType = typeof(InvTransText))]
        public double SumQty { get { return _SumQty; } }
    }

    public partial class InventoryTransactionStatement : GridBasePage
    {
        ItemBase ibase;
        List<InvItemTransStatementList> statementlist;
        public override string NameOfControl { get { return TabControls.InventoryTransactionStatement; } }
        public InventoryTransactionStatement(UnicontaBaseEntity master): base(master)
        {
            InitPage(master);
        }
        UnicontaBaseEntity master;
        void InitPage(UnicontaBaseEntity _master)
        {
            this.DataContext = this;
            InitializeComponent();
            master = _master;
            SetRibbonControl(localMenu, dgInvSeriesBatch);
            statementlist = new List<InvItemTransStatementList>();
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            GetMenuItem();
        }

        public override Task InitQuery()
        {
            return LoadInvSeriesBatch();
        }

        public override void AssignMultipleGrid(List<Uniconta.ClientTools.Controls.CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgInvSeriesBatch);
            gridCtrls.Add(childDgInvSeriesBatch);
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ExpandAndCollapse":
                    if (dgInvSeriesBatch.ItemsSource == null)
                        return;
                    setExpandAndCollapse(dgInvSeriesBatch.IsMasterRowExpanded(0));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void setExpandAndCollapse(bool expandState)
        {
            if (dgInvSeriesBatch.ItemsSource == null) return;
            if (ibase == null) return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("ExpandAll") && !expandState)
            {
                ExpandAndCollapseAll(false);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("CollapseAll");
                ibase.LargeGlyph = UnicontaClient.Utilities.Utility.GetGlyph("Collapse_32x32");
            }
            else
            {
                if (expandState)
                {
                    ExpandAndCollapseAll(true);
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                    ibase.LargeGlyph = UnicontaClient.Utilities.Utility.GetGlyph("Expand_32x32");
                }
            }
        }

        void ExpandAndCollapseAll(bool ISCollapseAll)
        {
            int dataRowCount = statementlist.Count;
            for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                if (!ISCollapseAll)
                    dgInvSeriesBatch.ExpandMasterRow(rowHandle);
                else
                    dgInvSeriesBatch.CollapseMasterRow(rowHandle);
        }

        private async Task LoadInvSeriesBatch()
        {
            setExpandAndCollapse(true);
            statementlist.Clear();
            busyIndicator.IsBusy = true;

            var ItemCache = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem));

            var tranApi = new ReportAPI(api);
            var lstEntity = await tranApi.GetSeriaBatchInBOM(master as InvTransClient, new InvSerieBatchClient());
            if (lstEntity != null)
            {
                string curItem = " ";
                InvItemTransStatementList ob = null;
                List<InvSeriesBatchTotal> tlst = null;
                double SumCost = 0d, SumQty = 0d;
                foreach (var s in lstEntity)
                {
                    var master = s.Master as InvTransClient;
                    if (master._Item != curItem)
                    {
                        var ac = (InvItem)ItemCache.Get(master._Item);
                        if (ac == null)
                            continue;

                        ob = new InvItemTransStatementList();
                        curItem = ac._Item;
                        ob.ItemNumber = ac._Item;
                        ob.Name = ac._Name;
                        ob.ItemType = AppEnums.ItemType.ToString(ac._ItemType);
                        tlst = new List<InvSeriesBatchTotal>(s.Details.Length);
                        ob.ChildRecord = tlst;
                        SumCost = SumQty = 0d;
                        foreach (var detail in s.Details)
                        {
                            var child = detail as InvSerieBatchClient;
                            var child1 = new InvSeriesBatchTotal();
                            StreamingManager.Copy(child, child1);
                            child._Item = curItem;
                            var costLine = (child._CostPrice * child._Qty);
                            SumCost += costLine;
                            child1._SumCost = costLine;

                            SumQty += child._Qty;
                            child1._SumQty = child._Qty;
                            tlst.Add(child1);
                            if (ob._SerialNumbers == null)
                                ob._SerialNumbers = child._Number;
                            else
                                ob._SerialNumbers = string.Format("{0}, {1}", ob._SerialNumbers, child._Number);
                        }
                        ob._SumCost = SumCost;
                        ob._SumQty = SumQty;
                        statementlist.Add(ob);
                    }
                }
            }

            dgInvSeriesBatch.ItemsSource = statementlist;
            dgInvSeriesBatch.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ExpandAndCollapse");
            UtilDisplay.RemoveMenuCommand(rb, "ViewDownloadRow");
        }
    }
}
