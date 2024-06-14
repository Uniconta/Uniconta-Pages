using DevExpress.CodeParser;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.Filtering;
using DevExpress.XtraPrinting.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
using System.ServiceModel.Syndication;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CopyProjectBudgetPageGrid : ProjectBudgetGrid
    {
        public override bool Readonly => true;
    }
    public class CopyProjectBudgetEstimationGrid : ProjectBudgetEstimationGrid
    {
        public override bool Readonly => true;
    }
    
    public partial class CopyBudgetLines : GridBasePage
    {
        CrudAPI crudApi;
        public List<ProjectInvoiceProposalLineClient> ProposalLines { get; set; }
        public bool IsDeleteLines { get; set; }

        public bool OnlyCopyTotals { get; set; }
        public override string NameOfControl => TabControls.CopyBudgetLines;
        static double pageHeight = 650.0d, pageWidth = 850.0d;
        static Point position = new Point();
        public override void PageClosing()
        {
            if (ProposalLines != null)
            {
                var args = new object[2];
                args[0] = IsDeleteLines;
                if (proposal is ProjectReservation)
                    args[1] = BudgetLines;
                else
                    args[1] = ProposalLines;
                globalEvents.OnRefresh(NameOfControl, args);
            }
            position = AttachVoucherGridPage.GetPosition(dockCtrl);
        }
        DCOrder proposal;
        public CopyBudgetLines(CrudAPI api, DCOrder proposal) : base(api, string.Empty)
        {
            this.proposal = proposal;
            InitializeComponent();
            crudApi = api;
            InitPage();
        }
        protected override Filter[] DefaultFilters()
        {
            Filter dateFilter = new Filter() { name = "Project", value = proposal._Project };
            return new Filter[] { dateFilter };
        }
        private void InitPage()
        {
            this.DataContext = this;
            InitializeComponent();
            var comp = crudApi.CompanyEntity;
            SetRibbonControl(localMenu, dgBudgetGrid);
            dgBudgetGrid.api = api;
            dgBudgetLinesGrid.api = api;
            dgBudgetGrid.BusyIndicator = busyIndicator;
            dgBudgetGrid.SelectedItemChanged += DgBudgetGrid_SelectedItemChanged;
            dgBudgetLinesGrid.ItemsSourceChanged += DgBudgetLinesGrid_ItemsSourceChanged;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgBudgetLinesGrid.treeListView.ShowNodeFooters = false;

            if (proposal is ProjectReservation)
                chkOnlyCopyTotals.Visibility = chkDeleteProposalLines.Visibility = Visibility.Collapsed;
        }

        private void DgBudgetLinesGrid_ItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            ProjectBudgetEstimationPage.ResetSum(dgBudgetLinesGrid);
            dgBudgetLinesGrid.treeListView.ExpandAllNodes();
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        ProjectBudgetLineLocal[] budgetLines;
        private async void DgBudgetGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            var budget = e.NewItem as ProjectBudgetClient;
            BusyIndicator.IsBusy = true;
            budgetLines = await api.Query<ProjectBudgetLineLocal>(budget);
            if (budgetLines?.Length > 0)
            {
                Array.Sort(budgetLines, new ProjectBudgetLineSort(budgetLines));
                var maxId = budgetLines.Select(s => s.Id).Max();
                foreach (var line in budgetLines)
                {
                    if (line.Id == 0)
                        line.Id = ++maxId;
                }
                dgBudgetLinesGrid.MaxId = maxId;
                dgBudgetLinesGrid.SetSource(budgetLines);
            }
            BusyIndicator.IsBusy = false;
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ProposalLines = null;
                this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    ProposalLines = null;
                    this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        public List<ProjectBudgetLineLocal> BudgetLines;
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;
            if (budgetLines?.Length > 0)
            {
                ProposalLines = new List<ProjectInvoiceProposalLineClient>();
                List<ProjectBudgetLineLocal> treeSource = new List<ProjectBudgetLineLocal>();
                GenerateProjectEstimateReportSource(dgBudgetLinesGrid.treeListView.Nodes, treeSource);
                BudgetLines = treeSource;
                foreach (var ProjectBudgetLineLocal in treeSource)
                {
                    if (OnlyCopyTotals && !ProjectBudgetLineLocal.Header)
                        continue;
                    var ProjInvProposalLine = new ProjectInvoiceProposalLineClient();
                    ProjInvProposalLine.Text = ProjectBudgetLineLocal.Text;
                    ProjInvProposalLine.WorkSpace = ProjectBudgetLineLocal.WorkSpace;
                    ProjInvProposalLine._Task = ProjectBudgetLineLocal._Task;
                    ProjInvProposalLine._Text = ProjectBudgetLineLocal._Comment;
                    if (OnlyCopyTotals && ProjectBudgetLineLocal.Header)
                    {
                        var currentNode = dgBudgetLinesGrid.treeListView.GetNodeByContent(ProjectBudgetLineLocal);
                        bool allSubHeaders = false;
                        if (currentNode.HasChildren)
                        {
                            allSubHeaders = true;
                            foreach (var node in currentNode.Nodes)
                            {
                                var childNode = node.Content as ProjectBudgetLineLocal;
                                if (!childNode.Header)
                                {
                                    allSubHeaders = false;
                                    break;
                                }
                            }
                        }
                        if (!allSubHeaders)
                        {
                            ProjInvProposalLine.Qty = 1;
                            ProjInvProposalLine.Price = ProjectBudgetLineLocal.Sales;
                        }
                    }
                    else if (!ProjectBudgetLineLocal.Header)
                    {
                        ProjInvProposalLine.PrCategory = ProjectBudgetLineLocal.PrCategory;
                        ProjInvProposalLine.Employee = ProjectBudgetLineLocal.Employee;
                        ProjInvProposalLine.Qty = ProjectBudgetLineLocal.Qty;
                        ProjInvProposalLine.Price = ProjectBudgetLineLocal.SalesPrice;
                        ProjInvProposalLine.Item = ProjectBudgetLineLocal.Item;
                        ProjInvProposalLine.Unit = ProjectBudgetLineLocal.Unit;
                        ProjInvProposalLine.Note = ProjectBudgetLineLocal.Comment;
                        ProjInvProposalLine.Dimension1 = ProjectBudgetLineLocal.Dimension1;
                        ProjInvProposalLine.Dimension2 = ProjectBudgetLineLocal.Dimension2;
                        ProjInvProposalLine.Dimension3 = ProjectBudgetLineLocal.Dimension3;
                        ProjInvProposalLine.Dimension4 = ProjectBudgetLineLocal.Dimension4;
                        ProjInvProposalLine.Dimension5 = ProjectBudgetLineLocal.Dimension5;
                    }
                    ProposalLines.Add(ProjInvProposalLine);
                }
            }
            busyIndicator.IsBusy = false;
            this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
        }
        private void GenerateProjectEstimateReportSource(TreeListNodeCollection nodes, List<ProjectBudgetLineLocal> treeSource)
        {
            foreach (var node in nodes)
            {
                treeSource.Add(node.Content as ProjectBudgetLineLocal);
                if (node.HasChildren && node.IsExpanded)
                    GenerateProjectEstimateReportSource(node.Nodes, treeSource);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            budgetLines = null;
            this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
        }
        protected override void OnLayoutLoaded()
        {
            AttachVoucherGridPage.SetFloatingHeightAndWidth(dockCtrl, position, NameOfControl);
            base.OnLayoutLoaded();
        }
    }
}
