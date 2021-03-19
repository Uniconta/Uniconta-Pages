using UnicontaClient.Models;
using UnicontaClient.Utilities;
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
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using DevExpress.Xpf.Grid.Native;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvItemClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvItemClient); } }
        public InvItemClientGrid()
        {
            CustomTableView tv = new CustomTableView();
            tv.AllowEditing = false;
            tv.ShowGroupPanel = false;
            SetTableViewStyle(tv);
            View = tv;
        }
    }

    public class InvBOMClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvBOMClient); } }

        public InvBOMClientGrid(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement)
        {

        }
        public InvBOMClientGrid()
        {
            CustomTableView tv = new CustomTableView();
            tv.AllowEditing = false;
            tv.ShowGroupPanel = false;
            SetTableViewStyle(tv);
            View = tv;
        }
    }

    public partial class InventoryBOMStatement : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InventoryBOMStatement; } }
        List<InvItemClient> inventoryItemList = null;
        ItemBase ibase;
        public InventoryBOMStatement(BaseAPI API) : base(API, string.Empty)
        {
            this.DataContext = this;
            InitializeComponent();
            SetRibbonControl(localMenu, dgInvItem);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
#if SILVERLIGHT
            childDgInvBom.CurrentItemChanged += ChildDgInvBom_CurrentItemChanged;
#endif
            GetMenuItem();
            LoadInv();
        }

#if SILVERLIGHT
        private void ChildDgInvBom_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var detailsSelectedItem = e.NewItem as InvBOMClient;
            childDgInvBom.SelectedItem = detailsSelectedItem;
            childDgInvBom.syncEntity.Row = detailsSelectedItem;
        }
#endif

        public override Task InitQuery()
        {
            return null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
        }

        private async void LoadInv()
        {
            busyIndicator.IsBusy = true;
            var list = await api.LoadCache(typeof(Uniconta.DataModel.InvItem));
            inventoryItemList = new List<InvItemClient>();
            foreach (var rec in (Uniconta.DataModel.InvItem[])list.GetNotNullArray)
            {
                if (rec._ItemType >= 2)
                {
                    var inv = new InvItemClient();
                    StreamingManager.Copy(rec, inv);
                    inventoryItemList.Add(inv);
                }
            }

            var cache = new SQLCache(inventoryItemList.ToArray(), true);

            var invBomLst = await api.Query<InvBOMClient>();
            InvItemClient master = null;
            foreach (var invBom in invBomLst)
            {
                if (invBom._ItemMaster != master?._Item)
                    master = (InvItemClient)cache.Get(invBom._ItemMaster);
                if (master != null)
                {
                    List<InvBOMClient> bomLst;
                    if (master.BOMs != null)
                        bomLst = (List<InvBOMClient>)master.BOMs;
                    else
                        master.BOMs = bomLst = new List<InvBOMClient>();
                    bomLst.Add(invBom);
                }
            }

            if (inventoryItemList.Count > 0)
            {
                dgInvItem.ItemsSource = null;
                dgInvItem.ItemsSource = inventoryItemList;
            }
            dgInvItem.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ExpandAndCollapse":
                    if (dgInvItem.ItemsSource == null)
                        return;
                    setExpandAndCollapse(dgInvItem.IsMasterRowExpanded(0));
                    break;
                case "HierarichalInvBOMReport":

                    if (dgInvItem.SelectedItem != null)
                    {
                        var selectedInvItem = dgInvItem.SelectedItem as InvItemClient;
                        AddDockItem(TabControls.InventoryHierarchicalBOMStatement, selectedInvItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("HierarchicalBOM"), selectedInvItem._Item));
                    }
                    else if (childDgInvBom.SelectedItem != null)
                    {
                        var selectedInvBomClient = childDgInvBom.SelectedItem as InvBOMClient;
                        AddDockItem(TabControls.InventoryHierarchicalBOMStatement, selectedInvBomClient.InvItemPart, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("HierarchicalBOM"), selectedInvBomClient.InvItemPart._Item));
                    }
                    break;
                case "AddNestedProperty":
                    if (dgInvItem.SelectedItem != null)
                    {
                        gridRibbon_BaseActions(ActionType);
                        return;
                    }

                    var selectedItem = childDgInvBom.SelectedItem as InvBOMClient;
                    WizardWindow nestedPropDialog = new WizardWindow(new SelectNestedPropWizardView(childDgInvBom.TableTypeUser, api.CompanyEntity, selectedItem), Uniconta.ClientTools.Localization.lookup("AddNestedProperty"),
                        setHeight: false);
#if WPF
                    nestedPropDialog.Width = System.Convert.ToDouble(System.Windows.SystemParameters.PrimaryScreenWidth) * 0.35;
                    nestedPropDialog.Height = System.Convert.ToDouble(System.Windows.SystemParameters.PrimaryScreenHeight) * 0.45;
#else
                    if (Application.Current.IsRunningOutOfBrowser)
                    {
                        nestedPropDialog.Width = Application.Current.Host.Content.ActualWidth * 0.35;
                        nestedPropDialog.Height = Application.Current.Host.Content.ActualHeight * 0.45;
                    }
                    else
                    {
                        nestedPropDialog.Width = System.Convert.ToDouble(System.Windows.Browser.HtmlPage.Window.Eval("screen.width")) * 0.35;
                        nestedPropDialog.Height = System.Convert.ToDouble(System.Windows.Browser.HtmlPage.Window.Eval("screen.height")) * 0.45;
                    }
#endif
                    nestedPropDialog.MinHeight = 450.0d;
                    nestedPropDialog.MinWidth = 620.0d;

                    nestedPropDialog.Closed += delegate
                    {
                        if (nestedPropDialog.DialogResult == true)
                        {
                            var selectedProperties = nestedPropDialog.WizardData as IList<NestedPropertyObject>;
                            if (selectedProperties?.Count > 0)
                            {
                                foreach (var prop in selectedProperties)
                                    NestedColumn.CreatePropertyOnGrid(childDgInvBom, prop.PropertyName, prop.PropertyType, prop.DisplayName);
                            }
                        }
                    };
                    nestedPropDialog.Show();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void AssignMultipleGrid(List<Uniconta.ClientTools.Controls.CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgInvItem);
            gridCtrls.Add(childDgInvBom);
            isChildGridExist = true;
        }

        private void setExpandAndCollapse(bool expandState)
        {
            if (dgInvItem.ItemsSource == null) return;
            if (ibase == null) return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("ExpandAll") && !expandState)
            {
                ExpandAndCollapseAll(false);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("CollapseAll");
                ibase.LargeGlyph = UnicontaClient.Utilities.Utility.GetGlyph("Collapse_32x32.png");
            }
            else
            {
                if (expandState)
                {
                    ExpandAndCollapseAll(true);
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                    ibase.LargeGlyph = UnicontaClient.Utilities.Utility.GetGlyph("Expand_32x32.png");
                }
            }
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ExpandAndCollapse");
        }

        void ExpandAndCollapseAll(bool ISCollapseAll)
        {
            int dataRowCount = inventoryItemList.Count;
            for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                if (!ISCollapseAll)
                    dgInvItem.ExpandMasterRow(rowHandle);
                else
                    dgInvItem.CollapseMasterRow(rowHandle);
        }
    }
}
