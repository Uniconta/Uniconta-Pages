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
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Utilities;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class UserNotesPage2 : FormBasePage
    {
        UserNotesClient userNotesClientRow;
        private bool isFieldsAvailableForEdit = false;

        public override Type TableType { get { return typeof(UserNotesClient); } }
        public override string NameOfControl { get { return TabControls.UserNotesPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return userNotesClientRow; } set { userNotesClientRow = (UserNotesClient)value; } }
        public UserNotesPage2(UnicontaBaseEntity sourcedata, bool isEdit)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            isFieldsAvailableForEdit = isEdit;
            InitPage(api);
        }

        public UserNotesPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        private void InitPage(CrudAPI api)
        {
            layoutControl = layoutItems;         
            layoutItems.DataContext = userNotesClientRow;
            if(LoadedRow== null)
                frmRibbon.DisableButtons(new string[] { "Delete" });
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
    }
}
