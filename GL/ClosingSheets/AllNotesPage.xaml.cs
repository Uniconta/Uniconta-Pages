using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.Native;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AllnotesGrid : CorasauDataGridClient
    {
        public AllnotesGrid(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public AllnotesGrid()
        {
            CustomTableView tv = new CustomTableView();
            tv.AllowEditing = false;
            tv.ShowGroupPanel = false;
            SetTableViewStyle(tv);
            this.View = tv;
        }

        public override Type TableType { get { return typeof(GLAccountClient); } }
    }

    public class DataControlDetailDescriptorClass : DataControlDetailDescriptor
    {

    }

    public class AccountUserNoteList : GLAccountClient
    {        
        public UserNotesClient[] ChildRecord { get; set; }
    }
    public partial class AllNotesPage : GridBasePage
    {
        CWServerFilter glAccountFilterDialog;
        TableField[] GLAccountUserFields { get; set; }

        CWServerFilter userNotesFilterDialog;
        TableField[] UserNotesUserFields { get; set; }

        ItemBase ibase;
        public override string NameOfControl { get { return TabControls.AllNotesPage; } }

        SQLCache AccountListCache;

        Filter[] glAccountDefaultFilter;

        public AllNotesPage(CrudAPI api, SQLCache AccountListCache) : base(api, null)
        {
            this.DataContext = this;
            this.AccountListCache = AccountListCache;
            var arr = AccountListCache.GetKeyStrRecords;
            if (arr.Length > 0)
            {
                var AccountRange = string.Format("{0}..{1}", arr[0].KeyStr, arr[arr.Length - 1].KeyStr);
                glAccountDefaultFilter = new Uniconta.ClientTools.Controls.Filter[] { new Uniconta.ClientTools.Controls.Filter() { name = "Account", value = AccountRange } };
                glAccountFilterValues = new List<PropValuePair>() { PropValuePair.GenereteWhereElements("Account", typeof(string), AccountRange) };
            }
            InitializeComponent();
            SetRibbonControl(localMenu, dgGLAccount);
            dgGLAccount.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            GetMenuItem();
            ((TableView)childDgUserNotes.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ExpandAndCollapse");
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            GLAccountClient selectedItem = dgGLAccount.View.MasterRootRowsContainer.FocusedView.DataControl.CurrentItem as GLAccountClient;

            switch (ActionType)
            {
                case "Search":
                    InitQuery();
                    break;
                case "GLAccountFilter":
                    if (glAccountFilterDialog == null)
                    {
                        glAccountFilterDialog = new CWServerFilter(api, typeof(GLAccountClient), glAccountDefaultFilter, null, GLAccountUserFields);
                        glAccountFilterDialog.Closing += glAccountFilterDialog_Closing;
#if !SILVERLIGHT
                        glAccountFilterDialog.Show();
                    }
                    else
                        glAccountFilterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    glAccountFilterDialog.Show();
#endif
                    break;
                case "ClearGLAccountFilter":
                    glAccountFilterDialog = null;
                    glAccountFilterValues = null;
                    break;
                case "UserNotesFilter":
                    if (userNotesFilterDialog == null)
                    {
                        userNotesFilterDialog = new CWServerFilter(api, typeof(UserNotesClient), null, null, UserNotesUserFields);
                        userNotesFilterDialog.Closing += userNotesFilterDialog_Closing;
#if !SILVERLIGHT
                        userNotesFilterDialog.Show();
                    }
                    else
                        userNotesFilterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    userNotesFilterDialog.Show();
#endif
                    break;
                case "ClearUserNotesFilter":
                    userNotesFilterDialog = null;
                    userNotesFilterValues = null;
                    break;
                case "ExpandAndCollapse":
                    if (dgGLAccount.ItemsSource != null)
                        setExpandAndCollapse(dgGLAccount.IsMasterRowExpanded(0));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void setExpandAndCollapse(bool expandState)
        {
            if (dgGLAccount.ItemsSource == null) return;
            if (ibase == null) return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("ExpandAll") && !expandState)
            {
                ExpandAndCollapseAll(false);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("CollapseAll");
                ibase.LargeGlyph = Utility.GetGlyph(";component/Assets/img/Collapse_32x32.png");
            }
            else
            {
                if (expandState)
                {
                    ExpandAndCollapseAll(true);
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                    ibase.LargeGlyph = Utility.GetGlyph(";component/Assets/img/Expand_32x32.png");
                }
            }
        }

        void ExpandAndCollapseAll(bool ISCollapseAll)
        {
            int dataRowCount = ((IList)dgGLAccount.ItemsSource).Count;
            for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                if (!ISCollapseAll)
                    dgGLAccount.ExpandMasterRow(rowHandle);
                else
                    dgGLAccount.CollapseMasterRow(rowHandle);
        }

        IEnumerable<PropValuePair> glAccountFilterValues;
        FilterSorter glAccountPropSort;

        void glAccountFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (glAccountFilterDialog.DialogResult == true)
            {
                glAccountFilterValues = glAccountFilterDialog.PropValuePair;
                glAccountPropSort = glAccountFilterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            glAccountFilterDialog.Hide();
#endif
        }

        IEnumerable<PropValuePair> userNotesFilterValues;
        FilterSorter userNotesPropSort;

        void userNotesFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (userNotesFilterDialog.DialogResult == true)
            {
                userNotesFilterValues = userNotesFilterDialog.PropValuePair;
                userNotesPropSort = userNotesFilterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            userNotesFilterDialog.Hide();
#endif
        }
        
        public async override Task InitQuery()
        {
            AccountUserNoteList[] notesList = null;
            busyIndicator.IsBusy = true;
            var masterDtlList = await api.Query(new AccountUserNoteList(), null, glAccountFilterValues, new UserNotesClient(), userNotesFilterValues);           
            if (masterDtlList != null && masterDtlList.Length > 0)
            {
                notesList = new AccountUserNoteList[masterDtlList.Length];
                int i = 0;
                foreach (var item in masterDtlList)
                {
                    var obj = item.Master as AccountUserNoteList;
                    obj.ChildRecord = (UserNotesClient[])item.Details;
                    notesList[i++] = obj;
                }
            }

            if (notesList != null)
            {
                dgGLAccount.ItemsSource = null;
                dgGLAccount.ItemsSource = notesList;
            }
            dgGLAccount.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
            setExpandAndCollapse(false);
        }
    }
}
