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
    public class UserProfileTableAccessDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserProfileTableAccessClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class UserProfileTableAccessPage : GridBasePage
    {
        public UserProfileTableAccessPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgUserPrflTblAccess.UpdateMaster(master);
            localMenu.dataGrid = dgUserPrflTblAccess;
            SetRibbonControl(localMenu, dgUserPrflTblAccess);
            dgUserPrflTblAccess.api = api;
            dgUserPrflTblAccess.BusyIndicator = busyIndicator;
            bindTablelist();
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void bindTablelist()
        {
            List<Type> tablestype = Global.GetTables(api.CompanyEntity);
            tablestype.AddRange(Global.GetUserTables(api.CompanyEntity));
            var tableLst = new Dictionary<int, string>();
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
            var selectedItem = dgUserPrflTblAccess.SelectedItem as UserProfileTableAccessClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgUserPrflTblAccess.AddRow();
                    break;
                case "SaveGrid":
                    dgUserPrflTblAccess.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgUserPrflTblAccess.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
