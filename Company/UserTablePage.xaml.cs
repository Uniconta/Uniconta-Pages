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
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserTableGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TableHeaderClient); } }

        protected override void DataLoaded(UnicontaBaseEntity[] Arr)
        {
            api.CompanyEntity.UserTables = ((TableHeader[])Arr).ToList();
        }
    }

    public partial class UserTablePage : GridBasePage
    {
        public UserTablePage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage(null);
        }

        public UserTablePage(UnicontaBaseEntity master)
           : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            SetRibbonControl(localMenu, dgUserTable);
            dgUserTable.api = api;
            dgUserTable.BusyIndicator = busyIndicator;
            dgUserTable.UpdateMaster(master);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgUserTable.SelectedItemChanged += DgUserTable_SelectedItemChanged;
        }

        private void DgUserTable_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var selectedItem = dgUserTable.SelectedItem as TableHeaderClient;
            if (selectedItem == null)
                return;
            if (selectedItem._SharedFromCompanyId == 0)
            {
                ribbonControl.EnableButtons("EditRow");
                ribbonControl.EnableButtons("Fields");
                ribbonControl.EnableButtons("SharedToCompany");
            }
            else
            {
                ribbonControl.DisableButtons("EditRow");
                ribbonControl.DisableButtons("Fields");
                ribbonControl.DisableButtons("SharedToCompany");
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.UserTablePage2)
            {
                dgUserTable.UpdateItemSource(argument);

                var items = (IList)dgUserTable.ItemsSource;
                IEnumerable<TableHeader> castItem = items.Cast<TableHeader>();
                api.CompanyEntity.UserTables = castItem.ToList();
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserTable.SelectedItem as TableHeaderClient;
            switch (ActionType)
            {
                case "AddRow":
                    object[] param = new object[2];
                    param[0] = api;
                    param[1] = null;
                    AddDockItem(TabControls.UserTablePage2, param, Uniconta.ClientTools.Localization.lookup("UserTables"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem != null && selectedItem._SharedFromCompanyId == 0)
                        AddDockItem(TabControls.UserTablePage2, selectedItem, string.Format("{0}:{1}", Localization.lookup("UserTables"), selectedItem.Name));
                    break;
                case "Fields":
                    if (selectedItem != null && selectedItem._SharedFromCompanyId == 0)
                        AddDockItem(TabControls.TablesUserFields, selectedItem, string.Format("{0}:{1}", Localization.lookup("UserFields"), selectedItem.Name));
                    break;
                case "Data":
                    if (selectedItem == null)
                        return;
                    var tablePrompt = !string.IsNullOrEmpty(selectedItem._Prompt) ? UserFieldControl.LocalizePrompt(selectedItem._Prompt) : selectedItem._Name;
                    AddDockItem(TabControls.UserTableData, selectedItem, string.Format("{0}:{1}", Localization.lookup("Data"), tablePrompt));
                    break;
                case "CalculatedFields":
                    if (selectedItem == null)
                        return;
                    var tablePromp = !string.IsNullOrEmpty(selectedItem._Prompt) ? UserFieldControl.LocalizePrompt(selectedItem._Prompt) : selectedItem._Name;
                    if (selectedItem.UserType != null)
                    {
                        var rec = (TableData)Activator.CreateInstance(selectedItem.UserType);
                        rec.SetMaster(api.CompanyEntity);
                        AddDockItem(TabControls.TablePropertyPage, rec, string.Format("{0}:{1}", Localization.lookup("CalculatedFields"), tablePromp));
                    }
                    break;
                case "CopyUserTable":
                    CWCopyUserFields winUserFields = new CWCopyUserFields(api);
                    winUserFields.Closed += async delegate
                    {
                        if (winUserFields.DialogResult == true)
                        {
                            await session.OpenCompany(api.CompanyEntity.CompanyId, false);
                            InitQuery();
                        }
                    };
                    winUserFields.Show();
                    break;
                case "BaseClass":
                    if (selectedItem != null)
                        GenerateClass(selectedItem, true);
                    break;
                case "ClientClass":
                    if (selectedItem != null)
                        GenerateClass(selectedItem, false);
                    break;
                case "SharedToCompany":
                    if (selectedItem != null && selectedItem._SharedFromCompanyId == 0)
                        AddDockItem(TabControls.TableHeaderSharePage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("SharedToCompany"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void GenerateClass(TableHeader selectedItem, bool isBaseClass)
        {
            var properties = await api.Query<TableField>(selectedItem);
            var str = ClassGenerator.Create(selectedItem, properties, api, isBaseClass);
            var cwGenerateClass = new CWGenerateClass(str);
            cwGenerateClass.Show();
        }
    }
}
