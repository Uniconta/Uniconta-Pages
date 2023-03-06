using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Controls.Dialogs;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLBudgetGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get
            {
                return typeof(GLBudgetClient);
            }
        }
    }
    public partial class GLBudgetPage : GridBasePage
    {
        public override string NameOfControl
        {
            get
            {
                return TabControls.GLBudgetPage;
            }
        }
        public GLBudgetPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public GLBudgetPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgGlBudget);
            dgGlBudget.api = api;
            dgGlBudget.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgGlBudget.RowDoubleClick += dgGlBudget_RowDoubleClick;
        }
        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.GLAccount));
        }

        public override void Utility_Refresh(string screenName, object argument)
        {
            if (screenName == TabControls.GLBudgetPage2)
                dgGlBudget.UpdateItemSource(argument);
        }
      
        private void dgGlBudget_RowDoubleClick()
        {
            var selectedItem = dgGlBudget.SelectedItem as GLBudgetClient;
            if (selectedItem == null)
                return;
            AddDockItem(TabControls.GLBudgetLinePage, selectedItem);
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgGlBudget_RowDoubleClick();
        }
        static DateTime AddMonth2Date(DateTime d, int m)
        {
            if (d == DateTime.MinValue)
                return DateTime.MinValue;
            if (m == 0)
                return d;
            return d.AddMonths(m);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGlBudget.SelectedItem as GLBudgetClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.GLBudgetPage2, api, Uniconta.ClientTools.Localization.lookup("BudgetModel"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLBudgetPage2, selectedItem);
                    break;
                case "SaveGrid":
                    saveGrid(selectedItem);
                    break;
                case "CopyRow":
                    if (selectedItem == null) return;
                    CWCopyBudget cwCopyBudget = new CWCopyBudget(selectedItem._Name);
                    cwCopyBudget.Closing += async delegate
                     {
                         if (cwCopyBudget.DialogResult == true)
                         {
                             int AddMonth = cwCopyBudget.Months;
                             double PctFactor = cwCopyBudget.Pct;

                             double Factor = PctFactor != 0d ? (PctFactor + 100d) / 100d : 0d;

                             ErrorCodes err = ErrorCodes.NoSucces;
                             GLBudgetClient copyBudget = new GLBudgetClient();
                             copyBudget.Name = cwCopyBudget.BudgetName;
                             copyBudget.FromDate = AddMonth2Date(selectedItem._FromDate, AddMonth);
                             copyBudget.ToDate = AddMonth2Date(selectedItem._ToDate, AddMonth);
                             copyBudget.Comment = selectedItem._Comment;
                             copyBudget.BaseBudget = selectedItem._BaseBudget;
                             copyBudget._Active = selectedItem._Active;
                             busyIndicator.IsBusy = true;
                             err = await api.Insert(copyBudget);
                             if (err == ErrorCodes.Succes)
                             {
                                 var budgetLines = await api.Query<Uniconta.DataModel.GLBudgetLine>(selectedItem);
                                 if (budgetLines != null && budgetLines.Length > 0)
                                 {
                                     foreach (var line in budgetLines)
                                     {
                                         line._Date = AddMonth2Date(line._Date, AddMonth);
                                         line._ToDate = AddMonth2Date(line._ToDate, AddMonth);
                                         if (Factor != 0)
                                             line._Amount = Math.Round(line._Amount * Factor);
                                         line.SetMaster(copyBudget);
                                     }
                                     err = await api.Insert(budgetLines);
                                 }
                                 dgGlBudget.UpdateItemSource(1, copyBudget);
                             }
                             busyIndicator.IsBusy = false;
                             if (err != ErrorCodes.Succes)
                                 UtilDisplay.ShowErrorCode(err);
                         }
                     };
                    cwCopyBudget.Show();
                    break;
                case "BudgetLines":
                    if (selectedItem != null)
                        SaveAndOpenLines(selectedItem);
                    break;
                case "IncludeSubModels":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLBudgetBudgetPage, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("SubModels"), selectedItem._Name));
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgGlBudget.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Notes"), selectedItem._Name));
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgGlBudget.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void SaveAndOpenLines(GLBudgetClient selectedItem)
        {
            if (dgGlBudget.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && selectedItem.RowId == 0)
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.GLBudgetLinePage, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("BudgetLines"), selectedItem._Name));
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var budget = (sender as Image).Tag as GLBudgetClient;
            if (budget != null)
                AddDockItem(TabControls.UserDocsPage, dgGlBudget.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var budget = (sender as Image).Tag as GLBudgetClient;
            if (budget != null)
                AddDockItem(TabControls.UserNotesPage, dgGlBudget.syncEntity);
        }
    }
}
