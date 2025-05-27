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

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using System.Reflection;
using System.Windows;
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class AddUserFieldsPage2 : FormBasePage
    {
        TableFieldsClient editrow;
        UnicontaBaseEntity master;
        public override void OnClosePage(object[] RefreshParams)
        {

            ((TableFieldsClient)RefreshParams[1]).master = master;
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }

        public override Type TableType { get { return typeof(TableFieldsClient); } }

        public override string NameOfControl { get { return TabControls.AddUserFieldsPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (TableFieldsClient)value; } }
        public AddUserFieldsPage2(UnicontaBaseEntity sourcedata, UnicontaBaseEntity master, bool isLastField = false)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api, master, isLastField);
        }

        public AddUserFieldsPage2(CrudAPI crudApi, UnicontaBaseEntity master)
            : base(crudApi, null)
        {
            InitializeComponent();
            InitPage(crudApi, master);

        }
        void InitPage(CrudAPI crudapi, UnicontaBaseEntity master = null, bool isLastField = false)
        {
            if (!isLastField)
            {
                ribbonControl = frmRibbon;
                RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "Delete" });
            }
            this.master = master;
            layoutControl = layoutItems;
            if (LoadedRow == null && master != null)
            {
                editrow = CreateNew() as TableFieldsClient;
                editrow._FieldType = Uniconta.DataModel.CustomTypeCode.String;
                editrow.SetMaster(master);

            }
            BindRefTable(editrow._FieldType);
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void BindRefTable(CustomTypeCode _FieldType)
        {
            if (_FieldType == CustomTypeCode.AppEnum)
            {
                var appenums = AppEnums.GetAppEnumsList();
                cbRefTable.ItemsSource = appenums;
                layoutRefTable.Visibility = Visibility.Visible;
                liMultiline.Visibility = Visibility.Collapsed;
                liMultiSelection.Visibility = Visibility.Collapsed;
            }
            else if ((_FieldType == CustomTypeCode.String))
            {
                var referenceTables = new List<string>(200);

                var userTables = api.CompanyEntity.UserTables;
                if (userTables != null)
                {
                    foreach (var tbl in userTables)
                        if (tbl._HasPrimaryKey)
                            referenceTables.Add(tbl._Name);
                }
                foreach (Type tabletype in Global.GetStandardRefTables())
                    referenceTables.Add(tabletype.Name);

                cbRefTable.ItemsSource = referenceTables;
                layoutRefTable.Visibility = Visibility.Visible;
                liMultiline.Visibility = Visibility.Visible;
                liMultiSelection.Visibility = Visibility.Collapsed;
            }
            else if ((_FieldType == CustomTypeCode.Double) || (_FieldType == CustomTypeCode.Single) || (_FieldType == CustomTypeCode.Money))
            {
                liMath.Visibility = Visibility.Visible;
                layoutRefTable.Visibility = Visibility.Collapsed;
                liMultiline.Visibility = Visibility.Collapsed;
                liMultiSelection.Visibility = Visibility.Collapsed;

            }
            else if (_FieldType == CustomTypeCode.Enum)
            {
                liMath.Visibility = Visibility.Collapsed;
                layoutRefTable.Visibility = Visibility.Collapsed;
                liMultiline.Visibility = Visibility.Collapsed;
                liMultiSelection.Visibility = Visibility.Visible;
            }
            else
            {
                liMath.Visibility = Visibility.Collapsed;
                layoutRefTable.Visibility = Visibility.Collapsed;
                liMultiline.Visibility = Visibility.Collapsed;
                liMultiSelection.Visibility = Visibility.Collapsed;
            }
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                var element = FocusManager.GetFocusedElement(UtilDisplay.GetCurentWindow());
                if (element is System.Windows.Controls.Control)
                {
                    var ctrl = element as System.Windows.Controls.Control;
                    TraversalRequest tReq = new TraversalRequest(FocusNavigationDirection.Down);
                    ctrl.MoveFocus(tReq);
                }
                var name = (LoadedRow as TableFieldsClient)?.Name;
                if (name == null || name != editrow.Name)
                {

                    if (!TablePropertyPage2.FieldExists(api, this.master.GetType(), editrow.Name))
                        frmRibbon_BaseActions(ActionType);
                }
                else
                    frmRibbon_BaseActions(ActionType);
            }
            else
                frmRibbon_BaseActions(ActionType);
        }

        private void cbType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            BindRefTable(editrow._FieldType);
        }
    }
}
