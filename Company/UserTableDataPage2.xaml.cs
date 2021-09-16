using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DevExpress.Xpf.Docking;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class UserTableDataPage2 : FormBasePage
    {
        BaseUserTable editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }

        public override Type TableType { get { return typeof(TableData); } }
        public override string LayoutName { get { return string.Concat("TableDataFrom_", this.ParentControl.Caption.ToString()); } }

        public override string NameOfControl { get { return TabControls.UserTableDataPage2; } }
        public override UnicontaBaseEntity ModifiedRow { get { return (UnicontaBaseEntity)editrow; } set { editrow = (BaseUserTable)value; } }
        UnicontaBaseEntity sourcdata;
        TableHeader tableheadermaster;
        public UserTableDataPage2(UnicontaBaseEntity sourcedata, TableHeader master = null)
            : base(sourcedata, true)
        {
            tableheadermaster = master;
            sourcdata = sourcedata;
            InitializeComponent();
            lookupMasterKey.IsEnabled = false;
            InitPage(api);
        }
        public UserTableDataPage2(CrudAPI crudApi, UnicontaBaseEntity master = null, UnicontaBaseEntity parent = null)
            : base(crudApi, null)
        {
            InitializeComponent();
            tableheadermaster = master as TableHeader;
            if (master != null && parent != null)
               sourcdata = parent;
           InitPage(crudApi);
        }

        void InitPage(CrudAPI crudapi)
        {
            frmRibbon.TableName = tableheadermaster?._Name;
            Layout._SubId = crudapi.CompanyId;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = Activator.CreateInstance(tableheadermaster.UserType) as BaseUserTable;
                if (sourcdata != null)
                    editrow.SetMaster(sourcdata);
            }
            CreateUserField();
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
        void CreateUserField()
        {
            if (tableheadermaster._HasPrimaryKey)
                UserFieldControl.CreateKeyFieldsGroupOnPage2(layoutItems, tableheadermaster._PKprompt);
            if(tableheadermaster._TableType == TableBaseType.Transaction)
                UserFieldControl.CreateDateFieldGroupOnPage2(layoutItems);
            var UserFieldDef = editrow.UserFieldDef();
            if (UserFieldDef != null)
                UserFieldControl.CreateUserFieldOnPage2(layoutItems, UserFieldDef, (RowIndexConverter)this.Resources["RowIndexConverter"], this.api, this);

            if (tableheadermaster._MasterTable!=null)
            {
                int _tableId = editrow.MasterTableId;
                var RefType = editrow.MasterType;
                if (RefType == null && _tableId != 0)
                {
                    RefType = typeof(Uniconta.DataModel.TableDataWithKey);
                    liMasterKey.FieldName = Uniconta.ClientTools.Localization.lookup(tableheadermaster._MasterTable);
                    liMasterName.FieldName = Uniconta.ClientTools.Localization.lookup("Name");
                }
                lookupMasterKey.api = this.api;
                lookupMasterKey.SetForeignKeyRef(RefType, _tableId);
            }
            else
            {
                lookupMasterKey.IsEnabled = false;
                grpMaster.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void lookupMasterKey_SelectedIndexChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            var selectedItem = lookupMasterKey.SelectedItem;
            if (selectedItem != null)
            {
                var item = selectedItem as TableDataWithKey;
                if (item != null)
                    txtMasterName.Text = item.KeyName;
                else if (selectedItem is UnicontaBaseEntity)
                    txtMasterName.Text = Convert.ToString(selectedItem.GetType().GetProperty("KeyName").GetValue(selectedItem, null));
            }
        }
    }
}

