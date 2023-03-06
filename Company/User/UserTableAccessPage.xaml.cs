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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserTableAccessDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserTableAccessClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class UserTableAccessPage : GridBasePage
    {
        public UserTableAccessPage(UnicontaBaseEntity master)
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
        }

        private void bindTablelist()
        {
            List<Type> tablestype= Global.GetTables(api.CompanyEntity);
            tablestype.AddRange(Global.GetUserTables(api.CompanyEntity));
            var tableLst = new Dictionary<int,string>();
            foreach (var type in tablestype)
            {
                var entity = Activator.CreateInstance(type) as UnicontaBaseEntity;
                int userTblNo = entity != null ? entity.ClassId() : 0;
                var clientTableAttr = type.GetCustomAttributes(typeof(ClientTableAttribute), true);
                if (clientTableAttr.Length > 0 && userTblNo != 0)
                {
                    var attr = (ClientTableAttribute)clientTableAttr[0];
                    if (!tableLst.ContainsKey(userTblNo))
                        tableLst.Add(userTblNo, string.Format("{0} ({1})", type.Name, Uniconta.ClientTools.Localization.lookup(attr.LabelKey)));
                }
            }

            var lst = tableLst?.OrderBy(x => x.Value);
            cmbTables.ItemsSource = lst;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserTblAccess.SelectedItem as UserTableAccessClient;
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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
