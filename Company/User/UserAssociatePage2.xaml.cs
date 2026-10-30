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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class UserAssociatePage2 : FormBasePage
    {
        UserAssociateClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.UserAssociatePage2; } }

        public override Type TableType { get { return typeof(UserAssociateClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (UserAssociateClient)value; } }
        public UserAssociatePage2(UnicontaBaseEntity sourcedata, bool isEdit = true) : base(sourcedata, isEdit)
        {
            InitPage(sourcedata);
        }
        public UserAssociatePage2(CrudAPI crudApi)
            : base(crudApi, "")
        {
            InitializeComponent();
            liKey.Visibility = Visibility.Collapsed;
            InitPage(null);
        }
        void InitPage(UnicontaBaseEntity sourcedata)
        {
            InitializeComponent();
            ribbonControl = frmRibbon;
            layoutControl = layoutItems;
            if (LoadedRow == null && editrow == null)
                editrow = CreateNew() as UserAssociateClient;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
        private void btnCopyKey_Click(object sender)
        {
           System.Windows.Clipboard.SetText(txtKey.Text);
        }
    }
}
