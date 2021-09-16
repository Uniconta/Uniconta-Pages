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
    public partial class InventoryJournalPage2 : FormBasePage
    {
        InvJournalClient editrow;

        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }

        public override string NameOfControl { get { return TabControls.InventoryJournalPage2.ToString(); } }

        public override Type TableType { get { return typeof(InvJournalClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (InvJournalClient)value; } }

        public InventoryJournalPage2(UnicontaBaseEntity sourcedata) : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }

        public InventoryJournalPage2(CrudAPI crudApi, string dummy) : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtJournal, txtJournal);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            layoutControl = layoutItems;
            lkNumberSerie.api = lkTransType.api = cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = lkAccount.api = lkOffsetAccount.api = crudapi;
            Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow = CreateNew() as InvJournalClient;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
    }
}
