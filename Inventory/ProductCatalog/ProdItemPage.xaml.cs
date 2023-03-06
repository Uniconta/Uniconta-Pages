using System;
using System.Collections.Generic;
using System.Data;
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
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProdItemPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProdItemClient); } }
        public override bool IsAutoSave { get { return false; } }
        public override bool Readonly { get { return false; } }
    }

    public partial class ProdItemPage : GridBasePage
    {
        SQLCache prodSupplierCache, prodDisGrpCache, prodItmGrpCache;
        ProdCatalogClient master;
        public ProdItemPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public ProdItemPage(UnicontaBaseEntity _master)
           : base(_master)
        {
            master = _master as ProdCatalogClient;
            InitPage();
        }

        void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgProdItem;
            SetRibbonControl(localMenu, dgProdItem);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            dgProdItem.UpdateMaster(master);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            StartLoadCache();
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Catalog", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    var cache = api.GetCache(typeof(Uniconta.DataModel.ProdCatalog)) ?? api.LoadCache(typeof(Uniconta.DataModel.ProdCatalog)).GetAwaiter().GetResult();
                    master = (ProdCatalogClient)cache.Get(rec.Value);
                }
            }
            base.SetParameter(Parameters);
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgProdItem.AddRow();
                    break;
                case "DeleteRow":
                    dgProdItem.DeleteRow();
                    break;
                case "SaveGrid":
                    SaveData();
                    break;
                case "LoadProdCatalog":
#if !SILVERLIGHT
                    LoadProductCatalog();
#endif
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            prodSupplierCache = api.GetCache(typeof(Uniconta.DataModel.ProdSupplier)) ?? await api.LoadCache(typeof(Uniconta.DataModel.ProdSupplier)).ConfigureAwait(false);
            prodDisGrpCache = api.GetCache(typeof(Uniconta.DataModel.ProdDiscountGroup)) ?? await api.LoadCache(typeof(Uniconta.DataModel.ProdDiscountGroup)).ConfigureAwait(false);
            prodItmGrpCache = api.GetCache(typeof(Uniconta.DataModel.ProdItemGroup)) ?? await api.LoadCache(typeof(Uniconta.DataModel.ProdItemGroup)).ConfigureAwait(false);
        }

        async void SaveData()
        {
            var lst = new List<UnicontaBaseEntity>();

            if (supplierLst != null)
            {
                foreach (var name in supplierLst)
                {
                    var prodSupp = (ProdSupplier)prodSupplierCache?.Get(name);
                    if (prodSupp == null)
                    {
                        var newRecord = new ProdSupplierClient();
                        newRecord._Name = name;
                        newRecord.SetMaster(master);
                        lst.Add(newRecord);
                    }
                }
            }

            if (itemGrpLst != null)
            {
                foreach (var name in itemGrpLst)
                {
                    var itmGrp = (ProdItemGroup)prodItmGrpCache?.Get(name);
                    if (itmGrp == null)
                    {
                        var newRecord = new ProdItemGroupClient();
                        newRecord._Name = name;
                        newRecord.SetMaster(master);
                        lst.Add(newRecord);
                    }
                }
            }

            if (disGrpLst != null)
            {
                foreach (var name in disGrpLst)
                {
                    var disGrp = (ProdDiscountGroup)prodDisGrpCache?.Get(name);
                    if (disGrp == null)
                    {
                        var newRecord = new ProdDiscountGroupClient();
                        newRecord._Name = name;
                        newRecord.SetMaster(master);
                        lst.Add(newRecord);
                    }
                }
            }

            ErrorCodes errorCode;
            if (lst.Count > 0)
            {
                api.AllowBackgroundCrud = false;
                errorCode = await api.Insert(lst);
            }
            else
                errorCode = ErrorCodes.Succes;

            if (errorCode == ErrorCodes.Succes)
                saveGrid();
            else
                UtilDisplay.ShowErrorCode(errorCode);
        }

        IEnumerable<string> supplierLst;
        IEnumerable<string> itemGrpLst;
        IEnumerable<string> disGrpLst;

#if !SILVERLIGHT
        void LoadProductCatalog()
        {
            var openFileDailog = UtilDisplay.LoadOpenFileDialog;
            openFileDailog.Filter = "CSV Files |*.csv";
            openFileDailog.Multiselect = false;
            bool? userClickedOK = openFileDailog.ShowDialog();
            if (userClickedOK != true) return;
            var selectedFile = openFileDailog.FileName;
            var data = ProductCatalogHelper.FromCsv(selectedFile);
            if (data != null)
            {
                supplierLst = data.AsEnumerable().Select(x => x.Field<string>("Supplier")).Distinct();
                itemGrpLst = data.AsEnumerable().Select(x => x.Field<string>("ItemGroup")).Distinct();
                disGrpLst = data.AsEnumerable().Select(x => x.Field<string>("DiscountGroup")).Distinct();

                foreach (DataRow row in data.Rows)
                {
                    var prodItem = new ProdItemClient
                    {
                        _Item = row["Item"].ToString(),
                        _EAN = row["EAN"].ToString(),
                        _Name = row["Name"].ToString(),
                        Unit = row["Unit"].ToString(),
                        _SupplierItemId = row["SupplierItemId"].ToString(),
                        //AlternativeItem = row["AlternativeItem"].ToString(),
                        _SalesPrice = NumberConvert.ToDouble(row["SalesPrice"].ToString()),
                        _Supplier = row["Supplier"].ToString(),
                        _DiscountGroup = row["DiscountGroup"].ToString(),
                        _ItemGroup = row["ItemGroup"].ToString(),
                        _WebArg = row["WebArg"].ToString(),
                    };
                    prodItem.SetMaster(master);
                    dgProdItem.AddRow(prodItem, -1, false);
                }
            }
        }
#endif
    }
}
