using System;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class AppsPage2 : FormBasePage
    {
        AppClient editrow;

        public override Type TableType { get { return typeof(AppClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (AppClient)value; } }
        public override string NameOfControl => TabControls.AppsPage2;

        public AppsPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage();
        }

        public AppsPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage();
        }

        void InitPage()
        {
            ribbonControl = frmRibbon;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as AppClient;
            }
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            layoutItems.DataContext = editrow;
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                // TODO: Implement actions from the ribbon here
                /* Example:
                case "AddDoc":
                    AddDockItem(TabControls.UserDocsPage, editrow);
                    break;*/
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
    }
}
