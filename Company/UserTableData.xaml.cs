using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.Common;
using Uniconta.DataModel;
using System.Threading;
using System.Windows.Data;
using System.Collections;
using Uniconta.API.System;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TabledataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return UserTableType; } }
        public override bool Readonly { get { return !IsEditable; } }
        public bool IsEditable;
        public Type UserTableType;
    }
    public partial class UserTableData : GridBasePage
    {
        public override string LayoutName { get { return string.Format("TableData_{0}", layoutname != null ? layoutname : this.thMaster?._Name); } }

        public override string NameOfControl { get { return TabControls.UserTableData; } }

        TableHeader thMaster;
        bool isTableDataWithKey;
        string layoutname;
        UnicontaBaseEntity master;
        string mastertabName;
        bool isInitialized;
        public UserTableData(TableHeader thMaster, string layoutname, UnicontaBaseEntity masterRecord)
            : base(thMaster)
        {
            Init(thMaster, layoutname, masterRecord);
        }

        void Init(TableHeader thMaster, string layoutname, UnicontaBaseEntity masterRecord)
        {
            this.thMaster = thMaster;
            Layout._SubId = api.CompanyId;
            if (layoutname.IndexOf(';') >= 0)
            {
                var param = layoutname.Split(';');
                mastertabName = param[1];
                layoutname = param[0];
            }
            this.layoutname = layoutname;
            InitializeComponent();
            Initialize(thMaster, masterRecord);
        }
        public UserTableData(TableHeader thMaster, string layoutname)
            : this(thMaster, layoutname, null as UnicontaBaseEntity)
        {
        }

        public UserTableData(TableHeader master) : base(master)
        {
            this.thMaster = master;
            Layout._SubId = api.CompanyId;
            InitializeComponent();
            Initialize(master, null);
        }
        public UserTableData(string lookupKey, TableHeader master) : base(master)
        {
            this.thMaster = master;
            Layout._SubId = api.CompanyId;
            InitializeComponent();
            Initialize(master, null, lookupKey);
        }
        public UserTableData(TableHeader thMaster, string layoutname, SynchronizeEntity syncEntity)
         : base(syncEntity, true)
        {
            Init(thMaster, layoutname, syncEntity?.Row);
        }

        public UserTableData(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
        }
        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            string tablename = null;
            foreach (var param in Parameters)
            {
                var paramName = param.Name;
                var paramValue = param.Value;
                if (string.IsNullOrEmpty(paramName) || string.Compare(param.Name, "table", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    tablename = paramValue;
                    break;
                }
            }
            if (tablename != null)
            {
                foreach (var master in api.CompanyEntity.UserTables)
                {
                    if (master._Name == tablename)
                    {
                        var tableHeaderClient = new TableHeaderClient();
                        StreamingManager.Copy(master, tableHeaderClient);
                        string header = master._Prompt == null ? master._Name : master._Prompt;
                        this.SetHeader(header);
                        this.thMaster = tableHeaderClient;
                        Layout._SubId = api.CompanyId;
                        this.layoutname = header;
                        if (!isInitialized)
                            Initialize(thMaster, null);
                        break;
                    }
                }
            }
            base.SetParameter(Parameters);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgTabledataGrid.UpdateMaster(args);
            SetHeader(args);
            InitQuery();
        }

        void SetHeader(UnicontaBaseEntity args)
        {
            var syncMaster = args;
            string header = null;
            if (syncMaster != null)
            {
                string key = (args as TableData)?._KeyName;
                if (string.IsNullOrEmpty(key))
                    key = (args as IdKey)?.KeyName;
                if (!string.IsNullOrEmpty(key))
                    header = string.Format("{0}:{1}/{2}", Uniconta.ClientTools.Localization.lookup("Data"), mastertabName, key);
            }
            if (header != null)
                SetHeader(header);
        }
        private void Initialize(TableHeader thMaster, UnicontaBaseEntity masterRecord, string lookupkey=null)
        {
            if (lookupkey != null)
                this.LookupKey = lookupkey;
            master = masterRecord;
            dgTabledataGrid.UserTableType = thMaster.UserType;
            dgTabledataGrid.IsEditable = thMaster._EditLines;

            // first call setUserFields after grid is setup correctly
            setUserFields(thMaster);
            RemoveMenuItem();
            LayoutControl = detailControl.layoutItems;
            localMenu.TableName = thMaster?._Name;
            SetRibbonControl(localMenu, dgTabledataGrid);
            localMenu.TableName = thMaster?._Name;
            dgTabledataGrid.api = api;
            dgTabledataGrid.BusyIndicator = busyIndicator;

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgTabledataGrid.UpdateMaster(masterRecord);
            isInitialized = true;
        }

        List<TableHeader> dtlTables;
        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (!this.thMaster._Attachment)
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "AddDoc", "AddNote" });
            if (dgTabledataGrid.IsEditable)
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "AddItem", "EditItem" });
            else
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "AddRow", "CopyRow", "DeleteRow", "SaveGrid" });
            dtlTables = Utilities.Utility.GetDefaultCompany().UserTables.Where(x => x._MasterTable == thMaster._Name).ToList();
            if (dtlTables.Count > 0)
            {
                var childList = new List<TreeRibbon>();
                var childRibbon = new TreeRibbon();
                string nodeText = string.Empty;
                string tblName = string.Empty;
                if (dtlTables.Count > 1)
                    nodeText = Uniconta.ClientTools.Localization.lookup("UserTableData");
                else
                {
                    var tbl = dtlTables[0];
                    tblName = tbl._Name;
                    nodeText = tbl._Prompt != null ? UserFieldControl.LocalizePrompt(tbl._Prompt) : tbl._Name;
                }
                childRibbon.Name = nodeText;
                childRibbon.ActionName = dtlTables.Count > 1 ? "" : string.Concat("UserTableData;", tblName);
                childRibbon.Child = childList;
                childRibbon.Glyph = "UserFieldData_32x32.png";
                childRibbon.LargeGlyph = "UserFieldData_32x32.png";
                var userRbnList = new List<TreeRibbon>();
                userRbnList.Add(childRibbon);
                var treeRibbon = new TreeRibbon();
                treeRibbon.Child = userRbnList;
                rb.rbnlist.Add(treeRibbon);
                if (dtlTables.Count > 1)
                {
                    foreach (var ur in dtlTables)
                    {
                        var ribbonNode = new TreeRibbon();
                        ribbonNode.Name = !string.IsNullOrEmpty(ur._Prompt) ? UserFieldControl.LocalizePrompt(ur._Prompt) : ur._Name;
                        ribbonNode.ActionName = string.Concat("UserTableData;", ur._Name);
                        ribbonNode.LargeGlyph = "CopyUserTable_16x16.png";
                        ribbonNode.Glyph = "CopyUserTable_16x16.png";
                        ribbonNode.Child = new List<TreeRibbon>();
                        childList.Add(ribbonNode);
                    }
                }
                rb.RefreshMenuItem(userRbnList);
            }
        }

        void setUserFields(TableHeader thMaster)
        {
            var userType = thMaster.UserType;
            if (userType == null)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UserTypeMasterError"), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }
            var row = Activator.CreateInstance(userType) as BaseUserTable;
            var UserFieldDef = row.UserFieldDef();
            localMenu.UserFields = UserFieldDef;

            if (dgTabledataGrid.Columns.Count == 0)
            {
                if (thMaster._HasPrimaryKey)
                    UserFieldControl.CreateKeyFieldsOnGrid(dgTabledataGrid, thMaster._PKprompt);
                if (thMaster._TableType == TableBaseType.Transaction)
                    UserFieldControl.CreateDateFieldOnGrid(dgTabledataGrid);
                if (UserFieldDef != null)
                    UserFieldControl.CreateUserFieldOnGrid(dgTabledataGrid, UserFieldDef, (RowIndexConverter)this.Resources["RowIndexConverter"], api, !dgTabledataGrid.IsEditable, useBinding: false);
                Layout._SubId = api.CompanyId;
            }
            else
                SetColBinding(UserFieldDef);
            detailControl.CreateUserField(UserFieldDef, thMaster._HasPrimaryKey, this.api, thMaster._PKprompt);
            if (thMaster._MasterTable != null)
            {
                var masterColumn = new CorasauDataGridTemplateColumnClient();
                masterColumn.FieldName = "MasterKey";
                masterColumn.RefType = row.MasterType;
                if (masterColumn.RefType == null)
                {
                    masterColumn.RefType = typeof(Uniconta.DataModel.TableDataWithKey);
                    masterColumn.TableId = row.MasterTableId;
                }
                if (dgTabledataGrid.IsEditable)
                    masterColumn.AllowEditing = DevExpress.Utils.DefaultBoolean.True;
                else
                    masterColumn.AllowEditing = DevExpress.Utils.DefaultBoolean.False;

                dgTabledataGrid.Columns.Add(masterColumn);

                dgTabledataGrid.LookupFieldsAdded = true;
            }
        }

        void SetColBinding(TableField[] UserFieldDef)
        {
            var CurrentCulture = Thread.CurrentThread.CurrentCulture;
            var Path = new PropertyPath("UserField");
            int i = 0;
            foreach (var def in UserFieldDef)
            {
                if (def._Delete || def._Hide)
                    continue;
                var b = new Binding();
                b.Converter = (RowIndexConverter)this.Resources["RowIndexConverter"];
                b.ConverterParameter = def;
                b.Path = Path;
                b.ConverterCulture = CurrentCulture;
                if (def._ReadOnly)
                    b.Mode = BindingMode.OneWay;
                dgTabledataGrid.Columns[i].Binding = b;
                i++;
            }
        }
        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTabledataGrid.SelectedItem as BaseUserTable;
            if (ActionType.Contains("UserTableData"))
            {
                if (selectedItem == null)
                    return;
                var sender = ribbonControl.senderRibbonButton;
                if (sender == null)
                    return;
                var tabName = sender.Content;
                var tableName = (sender.Tag as string)?.Split(';')[1];
                var userTable = dtlTables.Where(x => x._Name == tableName).FirstOrDefault();
                if (userTable == null)
                    return;
                var tableHeaderClient = userTable as TableHeaderClient;
                if (tableHeaderClient == null)
                {
                    tableHeaderClient = new TableHeaderClient();
                    StreamingManager.Copy(userTable, tableHeaderClient);
                }
                object[] parmtbldata = new object[3];
                parmtbldata[0] = tableHeaderClient;
                parmtbldata[1] = string.Concat(tableName, ";", tabName);
                parmtbldata[2] = dgTabledataGrid.syncEntity;
                AddDockItem(TabControls.UserTableData, parmtbldata, string.Format("{0}:{1}/{2}", Uniconta.ClientTools.Localization.lookup("Data"), tabName, (selectedItem as TableData)?._KeyName));
                return;
            }
            switch (ActionType)
            {
                case "AddItem":
                    if (this.thMaster?.UserType != null)
                    {
                        object[] param = new object[3];
                        param[0] = api;
                        param[1] = this.thMaster;
                        param[2] = this.master;
                        AddDockItem(TabControls.UserTableDataPage2, param, (this.thMaster as TableHeader)?._Name, "Add_16x16.png");
                    }
                    break;
                case "EditItem":
                    if (selectedItem != null)
                    {
                        object[] parameter = new object[2];
                        parameter[0] = selectedItem;
                        parameter[1] = this.thMaster;
                        AddDockItem(TabControls.UserTableDataPage2, parameter, (this.thMaster as TableHeader)?._Name, "Edit_16x16.png");
                    }
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), this.thMaster._Name));
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), this.thMaster._Name));
                    break;
                case "RefreshGrid":
                    if (gridControl.Visibility == Visibility.Visible)
                        gridRibbon_BaseActions(ActionType);
                    break;
                case "AddRow":
                    dgTabledataGrid.AddRow();
                    break;
                case "CopyRow":
                    dgTabledataGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgTabledataGrid.SaveData();
                    break;
                case "DeleteRow":
                    dgTabledataGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.UserTableDataPage2)
            {
                dgTabledataGrid.UpdateItemSource(argument);
                localMenu_OnItemClicked("RefreshGrid");
                if (dgTabledataGrid.Visibility == Visibility.Collapsed)
                    detailControl.Refresh(argument, dgTabledataGrid.SelectedItem);
            }
        }
    }
}
