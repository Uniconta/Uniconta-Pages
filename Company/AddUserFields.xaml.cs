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
        public AddUserFields(UnicontaBaseEntity sourcedata)
            : base(null)
        {
            master = sourcedata;
            InitializeComponent();
            localMenu.dataGrid = dgUserField;
            SetRibbonControl(localMenu, dgUserField);
            dgUserField.api = api;
            dgUserField.UpdateMaster(master);
            dgUserField.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AddUserFieldsPage2)
            {
                object[] argumentParams = (object[])argument;
                var Row = argumentParams[1] as TableFieldsClient;

                if (Row == null) return;
                var rowType = Row.master.GetType();
                var pageMasterType = master.GetType();
                if (rowType == typeof(TableHeaderClient))
                {
                    int argumentMasterTableId = 0, pageMasterTableId = 0;

                    if (Row.master is TableHeaderClient)
                        argumentMasterTableId = ((TableHeaderClient)Row.master).TableId;

                    if (master is TableHeaderClient)
                        pageMasterTableId = ((TableHeaderClient)master).TableId;

                    if (rowType == pageMasterType && argumentMasterTableId == pageMasterTableId)
                        dgUserField.UpdateItemSource(argument);
                }
                else if (rowType == pageMasterType)
                    dgUserField.UpdateItemSource(argument);

                session.OpenCompany(api.CompanyEntity.CompanyId, false);
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserField.SelectedItem as TableFieldsClient;
            switch (ActionType)
            {
                case "AddRow":
                    object[] param = new object[2];
                    param[0] = api;
                    param[1] = dgUserField.masterRecord;
                    AddDockItem(TabControls.AddUserFieldsPage2, param, Uniconta.ClientTools.Localization.lookup("UserFields"), ";component/Assets/img/Add_16x16.png");
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
                    AddDockItem(TabControls.AddUserFieldsPage2, paramEdit, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("UserFields"), selectedItem.Name));
                    break;
                case "CopyUserfields":
                    CWCopyUserFields winUserFields = new CWCopyUserFields(master, api);
                    winUserFields.Closed += async delegate
                     {
                         if (winUserFields.DialogResult == true)
                         {
                             await session.OpenCompany(api.CompanyEntity.CompanyId, false);
                             await InitQuery();
                         }
                     };
                    winUserFields.Show();
                    break;
                case "BaseClass":
                    GenerateClassCode(master, true);
                    break;
                case "ClientClass":
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

            var userFields = await api.Query<TableFieldsClient>(master);
            var classCode = ClassGenerator.Create(className, userFields, isBaseClass);

            var cwGenerateClass = new CWGenerateClass(classCode);
            cwGenerateClass.Show();
        }
    }
}
