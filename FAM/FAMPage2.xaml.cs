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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for FAMPage2.xaml
    /// </summary>
    public partial class FAMPage2 : FormBasePage
    {
        FamClient editrow;

        public override void OnClosePage(object[] RefreshParams)
        {
            object[] argsArray = new object[4];
            argsArray[0] = RefreshParams[0];
            argsArray[1] = RefreshParams[1];
            ((FamClient)argsArray[1]).NotifyPropertyChanged("UserField");

            argsArray[2] = this.backTab;
            argsArray[3] = editrow;
            globalEvents.OnRefresh(NameOfControl, argsArray);
        }

        public override Type TableType { get { return typeof(FamClient); } }
        public override string NameOfControl { get { return TabControls.FAMPage2; } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (FamClient)value; } }

        public FAMPage2(UnicontaBaseEntity sourcedata, bool isEdit = true) : base(sourcedata, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
            {
                editrow = (FamClient)StreamingManager.Clone(sourcedata);
                IdKey idkey = (IdKey)editrow;
                if (idkey.KeyStr != null)
                    idkey.KeyStr = null;
            }
            InitPage(api);
        }

        public FAMPage2(CrudAPI crudApi, string dummy) : base(crudApi, dummy)
        {
            InitializeComponent();

            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtAsset, txtAsset);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            layoutControl = layoutItems;
            leEmployee.api = leGroup.api = leInsurer.api = leSoldTo.api = leParent.api = crudapi;

            if (LoadedRow == null && editrow== null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as FamClient;
                editrow.SetMaster(crudapi.CompanyEntity);
            }

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            dim1lookupeditior.api = dim2lookupeditior.api = dim3lookupeditior.api = dim4lookupeditior.api = dim5lookupeditior.api = api;
            //cbAssetLifeCycle.ItemsSource  = Enum.GetValues(typeof(AssetStatus));
            //cbDepreciationMethod.ItemsSource  = Enum.GetValues(typeof(AssetDepreciationMethod));
            StartLoadCache();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.FAMGroup), typeof(Uniconta.DataModel.Employee) });
        }

        private void cbDepreciationMethod_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            /*
            var idx = cbDepreciationMethod.SelectedIndex;
            if (idx == 0 || idx == 2)
            {
                deDepreciationLife.Visibility = Visibility.Collapsed;
                liDepreciationLife.Visibility = Visibility.Collapsed;

                deDepreciationRate.Visibility = Visibility.Visible;
                liDepreciationRate.Visibility = Visibility.Visible;
            }
            else if (idx == 4)
            {
                deDepreciationLife.Visibility = Visibility.Visible;
                liDepreciationLife.Visibility = Visibility.Visible;

                deDepreciationRate.Visibility = Visibility.Visible;
                liDepreciationRate.Visibility = Visibility.Visible;
            }
            else
            {
                deDepreciationLife.Visibility = Visibility.Visible;
                liDepreciationLife.Visibility = Visibility.Visible;

                deDepreciationRate.Visibility = Visibility.Collapsed;
                liDepreciationRate.Visibility = Visibility.Collapsed;
            }
            */
        }

        protected override void OnLayoutCtrlLoaded()
        {
            var comp = api.CompanyEntity;

            if (comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, usedim);
        }
    }
}
