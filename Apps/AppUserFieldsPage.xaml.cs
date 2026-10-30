using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using static Uniconta.DataModel.TableField;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AppTableFieldGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(AppTableFieldsClient); } }
    }

    public partial class AppUserFieldsPage : GridBasePage
    {
        UnicontaBaseEntity master;

        public AppUserFieldsPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        public AppUserFieldsPage(UnicontaBaseEntity sourcedata)
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
            //dgUserField.masterRecords= new List<UnicontaBaseEntity>() { master, App.MainPageObj.ActiveApp };
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
                            var masters = new List<UnicontaBaseEntity>();
                            master = row as UnicontaBaseEntity;
                            masters.Add(master);
                            masters.Add(App.MainPageObj.ActiveApp);
                            dgUserField.masterRecords = masters;
                            var header = string.Concat(Localization.lookup("UserFields"), ": ", Localization.lookup(rec.Value));
                            SetHeader(header);
                        }
                    }
                }
            }
            base.SetParameter(Parameters);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AppUserFieldsPage2)
            {
                object[] argumentParams = (object[])argument;
                var Row = argumentParams[1] as AppTableFieldsClient;
                if (Row == null)
                    return;

                int pageMasterTableId = Row.TableNo;
                var rowType = Row.master.GetType();
                var pageMasterType = master.GetType();

                var rowMaster = Row.master as Uniconta.DataModel.AppTableHeader;
                if (rowMaster != null)
                {
                    var MasterTableId = rowMaster.RowId;
                    if (master is Uniconta.DataModel.AppTableHeader mst)
                        pageMasterTableId = mst.RowId;

                    if (rowType == pageMasterType && MasterTableId == pageMasterTableId)
                        dgUserField.UpdateItemSource(argument);

                    //var lst = api.CompanyEntity.UserTables;
                    //if (lst != null)
                    //{
                    //    var tbl = lst.FirstOrDefault(x => x.RowId == pageMasterTableId);
                    //    if (tbl != null)
                    //        tbl.ClearGeneratedType();
                    //    else
                    //        lst.Add(rowMaster);

                     //   pageMasterTableId = TableData.GetClassIdSpecial(pageMasterTableId);
                    //}
                }
                else if (rowType == pageMasterType)
                    dgUserField.UpdateItemSource(argument);
                else
                    return;

                //var arr = (dgUserField.ItemsSource as IEnumerable<Uniconta.DataModel.AppTableField>).ToArray();
                //Array.Sort(arr, new TableFieldSort());
                //api.CompanyEntity.UpdateUserField(pageMasterTableId, arr);

                //if (rowMaster == null)
                //    api.CompanyEntity.UpdateUserType(Global.BaseType2ClientRefType(rowType));

                //api.CompanyEntity.UserTables = null;
                //session.OpenCompany(api.CompanyEntity.CompanyId, false);
            }
        }
        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, dgUserField?.ItemsSource == null ? false : (dgUserField.ItemsSource as IList).Count != 0);
            base.PageClosing();
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserField.SelectedItem as AppTableFieldsClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.AppUserFieldsPage2, new object[2] { api, dgUserField.masterRecord }, Uniconta.ClientTools.Localization.lookup("UserFields"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    selectedItem.SetMaster(api.CompanyEntity);
                    object[] paramEdit = new object[3];
                    paramEdit[0] = selectedItem;
                    paramEdit[1] = dgUserField.masterRecord;
                    var fields = ((IList)dgUserField.ItemsSource);
                    var lastItem = fields[fields.Count - 1];
                    paramEdit[2] = selectedItem == lastItem;
                    AddDockItem(TabControls.AppUserFieldsPage2, paramEdit, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("UserFields"), selectedItem._Name));
                    break;
                case "CopyUserfields":
                    CWCopyUserFields winUserFields = new CWCopyUserFields(master, api);
                    winUserFields.Args = new Uniconta.ClientTools.ChildWindowCallerArgs(this, api);
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
                //case "BaseClass":
                //    if (master != null)
                //        GenerateClassCode(master, true);
                //    break;
                //case "ClientClass":
                //    if (master != null)
                //        GenerateClassCode(master, false);
                //    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        //async private void GenerateClassCode(UnicontaBaseEntity master, bool isBaseClass)
        //{
        //    string className = string.Empty;
        //    if (isBaseClass)
        //        className = master.BaseEntityType().Name;
        //    else
        //        className = master.GetType().Name;

        //    UserTablePage.GenerateClass(master, isBaseClass, api, className);
        //}
    }
}
