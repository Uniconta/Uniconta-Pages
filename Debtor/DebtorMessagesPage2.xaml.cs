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
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorMessagesPage2 : FormBasePage
    {
        DebtorMessagesClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.DebtorMessagesPage2; } }
        public override Type TableType { get { return typeof(DebtorMessagesClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorMessagesClient)value; } }

        public DebtorMessagesPage2(UnicontaBaseEntity sourceData, bool isEdit) : base(sourceData, isEdit)
        {
            InitializeComponent();
            InitPage();
        }

        public DebtorMessagesPage2(CrudAPI crudApi, string dummy) : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage();
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtName, txtName);
#endif
        }
        private void InitPage()
        {
            layoutControl = layoutItems;
            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons( "Delete" );
                editrow = CreateNew() as DebtorMessagesClient;
                editrow._Default = true;
            }
            layoutItems.DataContext = editrow;
            BusyIndicator = busyIndicator;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;

            var cache = api.GetCache(typeof(Uniconta.DataModel.CompanyDocumentLayout));
            if (cache != null)
            {
                foreach (var rec in (IEnumerable<Uniconta.DataModel.CompanyDocumentLayout>)cache.GetNotNullArray)
                    rec.LastMessage = null; // we clear cache.
            }
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
    }
}
