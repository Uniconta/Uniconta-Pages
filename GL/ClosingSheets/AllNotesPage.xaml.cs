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
            CustomTableView tv = new CustomTableView
            {
                AllowEditing = false,
                ShowGroupPanel = false
            };
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
        public List<UserNotesClient> ChildRecord { get; set; }
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
                var AccountRange = string.Concat(arr[0].KeyStr, "..", arr[arr.Length - 1].KeyStr);
                glAccountDefaultFilter = new Uniconta.ClientTools.Controls.Filter[] { new Uniconta.ClientTools.Controls.Filter() { name = "Account", value = AccountRange } };
                glAccountFilterValues = new List<PropValuePair>() { PropValuePair.GenereteWhereElements("Account", typeof(string), AccountRange) };
            }
            InitializeComponent();
            SetRibbonControl(localMenu, dgGLAccount);
            dgGLAccount.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            GetMenuItem();
            ((TableView)childDgUserNotes.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
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
                        glAccountFilterDialog.Show();
                    }
                    else
                        glAccountFilterDialog.Show(true);
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
                        userNotesFilterDialog.GridSource = dgGLAccount.ItemsSource as IList<UnicontaBaseEntity>;
                        userNotesFilterDialog.Show();
                    }
                    else
                    {
                        userNotesFilterDialog.GridSource = dgGLAccount.ItemsSource as IList<UnicontaBaseEntity>;
                        userNotesFilterDialog.Show(true);
                    }
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
                ibase.LargeGlyph = Utility.GetGlyph("Collapse_32x32");
            }
            else
            {
                if (expandState)
                {
                    ExpandAndCollapseAll(true);
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                    ibase.LargeGlyph = Utility.GetGlyph("Expand_32x32");
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

        class SortNote : IComparer<Note>
        {
            public int Compare(Note x, Note y)
            {
                int c = x._TableRowId - y._TableRowId;
                if (c != 0)
                    return c;
                var local = (y._Text != null && y._Text[0] == '!');
                if (x._Text != null && x._Text[0] == '!')
                {
                    if (! local)
                        return -1;
                }
                else if (local)
                    return 1;
                return DateTime.Compare(y._Created, x._Created); // we have swapped them
            }
        }

        public async override Task InitQuery()
        {
            busyIndicator.IsBusy = true;

            UserNotesClient n;
            var lst = new List<UserNotesClient>(20);
            var Arr = (GLAccountClosingSheetClient[])AccountListCache.GetRecords;
            foreach (var Acc in Arr)
            {
                if (!string.IsNullOrWhiteSpace(Acc._SheetNote))
                {
                    n = new UserNotesClient();
                    if (Acc._Note != null)
                        n.Copy(Acc._Note);
                    n._Text = "! " + Acc._SheetNote;
                    n._TableId = GLAccount.CLASSID;
                    n._TableRowId = Acc.RowId;
                    lst.Add(n);
                }
            }
            lst.AddRange(await api.Query<UserNotesClient>(new GLAccount(), userNotesFilterValues));
            lst.Sort(new SortNote());

            StreamingManager.Copy(null, null);

            if (lst.Count > 0)
            {
                var notesList = new List<AccountUserNoteList>(lst.Count);

                for(var i = 0; i < lst.Count; i++)
                {
                    n = lst[i];
                    var Acc = (GLAccount)AccountListCache.Get(n._TableRowId);
                    if (Acc != null)
                    {
                        var acc = new AccountUserNoteList() { ChildRecord = new List<UserNotesClient>() { n } };
                        acc.Copy(Acc);
                        notesList.Add(acc);
                        while (i + 1 < lst.Count)
                        {
                            n = lst[i + 1];
                            if (n._TableRowId == Acc.RowId)
                            {
                                acc.ChildRecord.Add(n);
                                i++;
                            }
                            else
                                break;
                        }
                    }
                }
                if (notesList.Count > 0)
                {
                    notesList.Sort(Uniconta.Common.SQLCache.KeyStrSorter);
                    dgGLAccount.ItemsSource = null;
                    dgGLAccount.ItemsSource = notesList;
                }
            }
            dgGLAccount.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
            setExpandAndCollapse(false);
        }
    }
}
