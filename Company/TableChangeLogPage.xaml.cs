using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

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

        public TableChangeLogPage(UnicontaBaseEntity rec, CrudAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgTableChangeLog;
            SetRibbonControl(localMenu, dgTableChangeLog);
            dgTableChangeLog.api = api;
            filterDate = BasePage.GetSystemDefaultDate().AddMonths(-3);
            dgTableChangeLog.UpdateMaster(rec);
            dgTableChangeLog.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTableChangeLog.SelectedItem as TableChangeLogClient;
            switch (ActionType)
            {
                case "ShowFieldChanges":
                    if (selectedItem != null)
                    {
                        var param  = new object[2] { selectedItem, api };
                        AddDockItem(TabControls.FieldChangeLogPage, param, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("FieldChangeLog"), selectedItem.KeyName));
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
