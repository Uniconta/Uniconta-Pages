using DevExpress.XtraSpreadsheet.Commands;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProdCatalogPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProdCatalogClient); } }
    }

    public partial class ProdCatalogPage : GridBasePage
    {
        public ProdCatalogPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            dgProdCatalog.api = api;
            SetRibbonControl(localMenu, dgProdCatalog);
            dgProdCatalog.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        public async override Task InitQuery()
        {
            await Filter();
            var source = dgProdCatalog.ItemsSource as IList;
            var prods = await api.Query<ProdCompany>();
            var ProdIds = prods.Select(p => p._ProdId).ToList();
            foreach (ProdCatalogClient pc in source)
            {
                if (ProdIds.Contains(pc.RowId))
                    pc.Selected = true;
            }
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProdCatalog.SelectedItem as ProdCatalogClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.ProdCatalogPage2, api, Uniconta.ClientTools.Localization.lookup("ProdCatalog"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null) return;
                    string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProdCatalog"), selectedItem._Name);
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;
                    AddDockItem(TabControls.ProdCatalogPage2, EditParam, header, "Edit_16x16");
                    break;
                case "CopyRow":
                    if (selectedItem == null) return;
                    object[] copyParam = new object[2];
                    copyParam[0] = selectedItem;
                    copyParam[1] = false;
                    string hdr = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem._Name);
                    AddDockItem(TabControls.ProdCatalogPage2, copyParam, hdr);
                    break;
                case "ProdSupplier":
                    if(selectedItem!= null)
                    AddDockItem(TabControls.ProdSupplierPage, selectedItem, string.Format("{0}:{1}/{2}", string.Format(Uniconta.ClientTools.Localization.lookup("ProductOBJ"), Uniconta.ClientTools.Localization.lookup("Supplier")),selectedItem.RowId, selectedItem._Name));
                    break;
                case "ProdItemgroup":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProdItemgroupPage, selectedItem, string.Format("{0}:{1}/{2}", string.Format(Uniconta.ClientTools.Localization.lookup("ProductOBJ"), Uniconta.ClientTools.Localization.lookup("ItemGroup")), selectedItem.RowId, selectedItem._Name));
                    break;
                case "ProdDiscountGroup":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProdDiscountGroupPage, selectedItem, string.Format("{0}:{1}/{2}", string.Format(Uniconta.ClientTools.Localization.lookup("ProductOBJ"), Uniconta.ClientTools.Localization.lookup("DiscountGroup")), selectedItem.RowId, selectedItem._Name));
                    break;
                case "ProdItem":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProdItemPage, selectedItem, string.Format("{0}:{1}/{2}", string.Format(Uniconta.ClientTools.Localization.lookup("ProductOBJ"), Uniconta.ClientTools.Localization.lookup("Item")), selectedItem.RowId, selectedItem._Name));
                    break;
                case "Select":
                    Select(selectedItem);
                    break;
                case "Unselect":
                    Unselect(selectedItem);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break; 
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        public async void Select(ProdCatalogClient selectedItem)
        {
            if (selectedItem?.Selected == false)
            {
                var res = await api.Insert(new ProdCompany() { _CompanyId = api.CompanyId, _ProdId = selectedItem.RowId });
                if (res != ErrorCodes.Succes)
                   Uniconta.ClientTools.Util.UtilDisplay.ShowErrorCode(res);
                else
                    selectedItem.Selected = true;
            }
        }
        public async void Unselect(ProdCatalogClient selectedItem)
        {
            if (selectedItem?.Selected == true)
            {
                var res = await api.Delete(new ProdCompany() { _CompanyId = api.CompanyId, _ProdId = selectedItem.RowId });
                if (res != ErrorCodes.Succes)
                    Uniconta.ClientTools.Util.UtilDisplay.ShowErrorCode(res);
                else
                    selectedItem.Selected = false;
            }
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ProdCatalogPage2)
                dgProdCatalog.UpdateItemSource(argument);
            if (screenName == TabControls.ProdItemPage)
                dgProdCatalog.UpdateItemSource(argument);
        }
    }
}
