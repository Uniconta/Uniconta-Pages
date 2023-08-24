using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using UnicontaClient.Controls.Dialogs;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TableFieldGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TableFieldsClient); } }
    }

    public partial class AddUserFields : GridBasePage
    {
        UnicontaBaseEntity master;

        public AddUserFields(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        public AddUserFields(UnicontaBaseEntity sourcedata)
            : base(null)
        {
            master = sourcedata;
            InitPage();
        }

        void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgUserField;
            SetRibbonControl(localMenu, dgUserField);
            dgUserField.api = api;
            dgUserField.UpdateMaster(master);
            dgUserField.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (rec.Name == null || rec.Name == "Master")
                {
                    var t = Global.GetTableType(rec.Value) ?? Global.GetClientTableType(rec.Value);
                    if (t != null)
                    {
                        var row = Activator.CreateInstance(t);
                        if (row is Uniconta.DataModel.ITableFieldData)
                        {
                            master = row as UnicontaBaseEntity;
                            dgUserField.UpdateMaster(master);
                            var header = string.Concat( Localization.lookup("UserFields"), ": ", Localization.lookup(rec.Value));
                            SetHeader(header);
                        }
                    }
                }
            }
            base.SetParameter(Parameters);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AddUserFieldsPage2)
            {
                object[] argumentParams = (object[])argument;
                var Row = argumentParams[1] as TableFieldsClient;
                if (Row == null)
                    return;

                var rowType = Row.master.GetType();
                var pageMasterType = master.GetType();

                var rowMaster = Row.master as Uniconta.DataModel.TableHeader;
                if (rowMaster != null)
                {
                    int pageMasterTableId = 0;
                    var argumentMasterTableId = rowMaster.RowId;

                    if (master is Uniconta.DataModel.TableHeader)
                        pageMasterTableId = ((Uniconta.DataModel.TableHeader)master).RowId;

                    if (rowType == pageMasterType && argumentMasterTableId == pageMasterTableId)
                        dgUserField.UpdateItemSource(argument);
                }
                else if (rowType == pageMasterType)
                    dgUserField.UpdateItemSource(argument);
                api.CompanyEntity.UserTables = null;
                session.OpenCompany(api.CompanyEntity.CompanyId, false);
            }
        }
        public override void PageClosing()
        {
            Controls.MenuControl.ForceOpen = true;
            base.PageClosing();
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserField.SelectedItem as TableFieldsClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.AddUserFieldsPage2, new object[2] { api, dgUserField.masterRecord }, Uniconta.ClientTools.Localization.lookup("UserFields"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] paramEdit = new object[3];
                    paramEdit[0] = selectedItem;
                    paramEdit[1] = dgUserField.masterRecord;
                    var fields = ((IList)dgUserField.ItemsSource);
                    var lastItem = fields[fields.Count - 1];
                    paramEdit[2] = selectedItem == lastItem;
                    AddDockItem(TabControls.AddUserFieldsPage2, paramEdit, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("UserFields"), selectedItem._Name));
                    break;
                case "CopyUserfields":
                    CWCopyUserFields winUserFields = new CWCopyUserFields(master, api);
                    winUserFields.Closed += async delegate
                     {
                         if (winUserFields.DialogResult == true)
                         {
                             await session.OpenCompany(api.CompanyId, false);
                             InitQuery();
                         }
                     };
                    winUserFields.Show();
                    break;
                case "BaseClass":
                    if (master != null)
                        GenerateClassCode(master, true);
                    break;
                case "ClientClass":
                    if (master != null)
                        GenerateClassCode(master, false);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void GenerateClassCode(UnicontaBaseEntity master, bool isBaseClass)
        {
            string className = string.Empty;
            if (isBaseClass)
                className = master.BaseEntityType().Name;
            else
                className = master.GetType().Name;

            UserTablePage.GenerateClass(master, isBaseClass, api, className);
        }
    }
}
