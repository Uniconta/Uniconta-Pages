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
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class AttachedVouchersPage2 : FormBasePage
    {
        VouchersClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.AttachedVouchersPage2; } }

        public override Type TableType { get { return typeof(VouchersClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (VouchersClient)value; } }
        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            return false;    
        }
        public AttachedVouchersPage2(UnicontaBaseEntity sourcedata, bool isEdit = true) : base(sourcedata, isEdit)
        {
            InitPage(sourcedata);
        }

        void InitPage(UnicontaBaseEntity sourcedata)
        {
            InitializeComponent();
            ribbonControl = frmRibbon;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
            leApprover1.api = leApprover2.api = api;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
#if !SILVERLIGHT
            if (string.IsNullOrWhiteSpace(editrow._Url))
                grpURL.Visibility = Visibility.Collapsed;
#endif
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
    }
}
