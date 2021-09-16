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
    public partial class FAMGroupPage2 : FormBasePage
    {
        FAMGroupClient editrow;

        public override void OnClosePage(object[] RefreshParams)
        {
            object[] argsArray = new object[4];
            argsArray[0] = RefreshParams[0];
            argsArray[1] = RefreshParams[1];
            ((FAMGroupClient)argsArray[1]).NotifyPropertyChanged("UserField");

            argsArray[2] = this.backTab;
            argsArray[3] = editrow;
            globalEvents.OnRefresh(NameOfControl, argsArray);
        }

        public override Type TableType { get { return typeof(FAMGroupClient); } }

        public override string NameOfControl { get { return TabControls.FAMGroupPage2; } }

        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (FAMGroupClient)value; } }

        public FAMGroupPage2(UnicontaBaseEntity sourcedata, bool isEdit = true) 
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
            {
                editrow = (FAMGroupClient)StreamingManager.Clone(sourcedata);
                IdKey idkey = (IdKey)editrow;
                if (idkey.KeyStr != null)
                    idkey.KeyStr = null;
            }
            InitPage(api);
        }

        public FAMGroupPage2(CrudAPI crudApi, string dummy) : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtGroup, txtGroup);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            var Comp = api.CompanyEntity;
            layoutControl = layoutItems;
            leAcquisitionAccount.api = leAcquisitionOffset.api = leDepreciationAccount.api = leDepreciationOffset.api =
                leWriteOffAccount.api = leWriteOffOffset.api = leSalesAccount.api = leSalesOffset.api = 
                 leWriteDownAccount.api = leWriteDownOffset.api = leWriteUpAccount.api = leWriteUpOffset.api = leAutoNumber.api = crudapi;

            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons( "Delete" );
                editrow = CreateNew() as FAMGroupClient;
                editrow.SetMaster(crudapi.CompanyEntity);
            }

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_BaseActions;
            StartLoadCache();
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.GLAccount));
        }
    }
}

