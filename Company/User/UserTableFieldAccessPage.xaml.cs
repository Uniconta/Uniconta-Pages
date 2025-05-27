using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Linq;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System.Collections.Generic;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using System.ComponentModel;
using DevExpress.DataAccess.Excel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserTableFieldAccessDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserTableFieldAccessClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class UserTableFieldAccessPage : GridBasePage
    {
        public UserTableFieldAccessPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgUserTblAccess.UpdateMaster(master);
            localMenu.dataGrid = dgUserTblAccess;
            SetRibbonControl(localMenu, dgUserTblAccess);
            dgUserTblAccess.api = api;
            dgUserTblAccess.BusyIndicator = busyIndicator;
            bindTablelist();
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgUserTblAccess.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
        }
        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            UserTableFieldAccessClient oldselectedItem = e.OldItem as UserTableFieldAccessClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= UserTableFieldAccessClient_PropertyChanged;
            UserTableFieldAccessClient selectedItem = e.NewItem as UserTableFieldAccessClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += UserTableFieldAccessClient_PropertyChanged;
        }

        private void UserTableFieldAccessClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var rec = sender as UserTableFieldAccessClient;
            switch (e.PropertyName)
            {
                case "TableId":
                    TableName table = null;
                    tableLst?.TryGetValue(rec.TableId, out table);
                    var type = table?.Type;
                    if (type != null)
                    {
                        rec.Field = null;
                        rec.FieldSource = bindFieldlist(type);
                        rec.NotifyPropertyChanged("FieldSource");
                    }
                    break;
            }
        }
        Dictionary<int, TableName> tableLst = null;
        private void bindTablelist()
        {
            List<Type> tablestype = Global.GetTables(api.CompanyEntity);
            tablestype.AddRange(Global.GetUserTables(api.CompanyEntity));
            tableLst = new Dictionary<int, TableName>();
            foreach (var type in tablestype)
            {
                var entity = Activator.CreateInstance(type) as UnicontaBaseEntity;
                int userTblNo = entity != null ? entity.ClassId() : 0;
                var clientTableAttr = type.GetCustomAttributes(typeof(ClientTableAttribute), true);
                if (clientTableAttr.Length > 0 && userTblNo != 0)
                {
                    var attr = (ClientTableAttribute)clientTableAttr[0];
                    if (!tableLst.ContainsKey(userTblNo))
                        tableLst.Add(userTblNo, new TableName(type, string.Format("{0} ({1})", type.Name, Uniconta.ClientTools.Localization.lookup(attr.LabelKey))));
                }
            }

            var lst = tableLst?.OrderBy(x => x.Value.Name);
            cmbTables.ItemsSource = lst;
        }
        private IEnumerable<DisplayProperties> bindFieldlist(Type table)
        {
            var props = FilterSortHelper.GetDisplayProperties(true, false, table, api, null);
            var lst = props?.OrderBy(x => x.DisplayName);
            return lst;
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserTblAccess.SelectedItem as UserTableFieldAccessClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgUserTblAccess.AddRow();
                    break;
                case "SaveGrid":
                    dgUserTblAccess.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgUserTblAccess.DeleteRow();
                    break;
                case "CopyRow":
                    var row = dgUserTblAccess.CopyRow() as UserTableFieldAccessClient;
                    if (row != null)
                        row.Field = null;
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void PART_Editor_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            var rec = dgUserTblAccess.SelectedItem as UserTableFieldAccessClient;
            if (rec != null && rec?.FieldSource == null)
            {
                TableName table = null;
                if (tableLst?.TryGetValue(rec.TableId, out table) == true)
                {
                    var type = table?.Type;
                    if (type != null)
                    {
                        rec.FieldSource = bindFieldlist(type);
                        rec.NotifyPropertyChanged("FieldSource");
                    }
                }
            }
        }
    }
    public class TableName
    {
        public TableName(Type type, string name)
        {
            this.Type = type;
            this.Name = name;
        }
        public string Name { get; set; }
        public Type Type { get; set; }
    }

}
