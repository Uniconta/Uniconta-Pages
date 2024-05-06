using UnicontaClient.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TableChangeLogPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TableChangeLogClient); } }
        public override IComparer GridSorting { get { return new TableChangeLogTimeSort(); } }
    }

    public partial class TableChangeLogPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.TableChangeLogPage; } }

        DateTime filterDate;
        protected override Filter[] DefaultFilters()
        {
            if (filterDate != DateTime.MinValue)
            {
                Filter dateFilter = new Filter() { name = "Time", value = String.Format("{0:d}..", filterDate) };
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }
        UnicontaBaseEntity pageMaster;
        public TableChangeLogPage(UnicontaBaseEntity rec, CrudAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
            InitPage(rec);
        }

        private void InitPage(UnicontaBaseEntity rec)
        {
            localMenu.dataGrid = dgTableChangeLog;
            SetRibbonControl(localMenu, dgTableChangeLog);
            dgTableChangeLog.api = this.api;
            filterDate = BasePage.GetSystemDefaultDate().AddMonths(-3);
            pageMaster = rec;
            dgTableChangeLog.UpdateMaster(rec);
            dgTableChangeLog.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public TableChangeLogPage(BaseAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            if (Parameters == null)
                return;

            var tabLogParams = new TabChangeLogParameters();
            tabLogParams.company = api.CompanyEntity;
            tabLogParams.Parameters = Parameters;

            if(tabLogParams.TryGetChangeLogParameters())
            {
                var tableType = tabLogParams.TableType;
                if (tableType != null)
                {
                    var header = string.Concat(Uniconta.ClientTools.Localization.lookup("TableChangeLog"), ": ", tableType.Name);
                    SetHeader(header);
                    InitPage(Activator.CreateInstance(tabLogParams.TableType) as UnicontaBaseEntity);
                }
            }
            base.SetParameter(Parameters);
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTableChangeLog.SelectedItem as TableChangeLogClient;
            switch (ActionType)
            {
                case "ShowFieldChanges":
                    if (selectedItem != null)
                    {
                        var param = new object[3] { pageMaster, selectedItem, api };
                        AddDockItem(TabControls.FieldChangeLogPage, param, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("FieldChangeLog"), selectedItem.KeyName));
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }

    internal class TabChangeLogParameters
    {
        public IEnumerable<BasePage.ValuePair> Parameters;
        internal Type TableType;
        internal Uniconta.DataModel.Company company;
        public bool TryGetChangeLogParameters()
        {
            bool isvalid = false;

            try
            {
                foreach (var param in Parameters)
                {
                    var paramName = param.Name;
                    var paramValue = param.Value;

                    if (string.IsNullOrEmpty(paramName) || string.Compare(param.Name, "table", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string importtableType = paramValue;
                        if (!string.IsNullOrEmpty(importtableType) && company != null)
                        {

                            if (company != null)
                            {
                                List<Type> tablestype;
                                tablestype = Global.GetTables(company); // Standard tables
                                tablestype.AddRange(Global.GetUserTables(company)); // User-defined tables
                                for (int i = 0; i < tablestype.Count; i++)
                                {
                                    var tType = tablestype[i];
                                    if (string.Compare(tType.Name, importtableType, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        TableType = tType;
                                        isvalid = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { isvalid = false; }

            return isvalid;
        }
    }
}
