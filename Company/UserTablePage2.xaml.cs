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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class UserTablePage2 : FormBasePage
    {
        TableHeaderClient editrow;
        async public override void OnClosePage(object[] RefreshParams)
        {
            var comp = api.CompanyEntity;

            if (comp != null)
            {
                Utilities.Utility.SetDefaultCompany(null);
                var curCompany = await BasePage.session.OpenCompany(comp.CompanyId, true);

                if (curCompany != null)
                    Utilities.Utility.SetDefaultCompany(curCompany);
                else
                    Utilities.Utility.SetDefaultCompany(comp);
            }

            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }

        public override Type TableType { get { return typeof(TableHeaderClient); } }
        public override string NameOfControl { get { return TabControls.UserTablePage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (TableHeaderClient)value; } }
        public UserTablePage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public UserTablePage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(pkPrompt, pkPrompt);
#endif
        }

        void InitPage(CrudAPI crudapi, UnicontaBaseEntity master = null)
        {
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            leAutoNumber.api = crudapi;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as TableHeaderClient;
                editrow.SetMaster(crudapi.CompanyEntity);
                if (master != null)
                    editrow.SetMaster(master);
            }
            layoutItems.DataContext = editrow;
            if (!editrow.HasPrimaryKey)
                pkPrompt.IsReadOnly = true;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            BindMaster();
        }

        private void BindMaster()
        {
            var referenceTables = new List<string>();

            var utbl = api.CompanyEntity.UserTables;
            if (utbl != null)
                foreach (var tbl in utbl)
                    if (tbl._HasPrimaryKey)
                        referenceTables.Add(tbl._Name);

            foreach (Type tabletype in Global.GetStandardRefTables())
                referenceTables.Add(tabletype.Name);
            cbMaster.ItemsSource = referenceTables;
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Delete":
                    if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ComfirmDeleteAllRecords"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                            Delete();
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void CheckEditor_Checked(object sender, RoutedEventArgs e)
        {
            pkPrompt.IsReadOnly = false;
        }

        private void CheckEditor_Unchecked(object sender, RoutedEventArgs e)
        {
            pkPrompt.IsReadOnly = true;
            pkPrompt.Text = null;
        }


    }
}

