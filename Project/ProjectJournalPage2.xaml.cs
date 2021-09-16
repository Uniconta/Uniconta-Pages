using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProjectJournalPage2 : FormBasePage
    {
        ProjectJournalClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.ProjectJournalPage2.ToString(); } }

        public override Type TableType { get { return typeof(ProjectJournalClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (ProjectJournalClient)value; } }
        /*For Edit*/
        public ProjectJournalPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public ProjectJournalPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
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
            lkEmployee.api = lkNumberSerie.api = lkTransType.api = cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = lePrWorkSpace.api = crudapi;
            Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow = CreateNew() as ProjectJournalClient;
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
