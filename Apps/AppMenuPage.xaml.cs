using UnicontaClient;
using UnicontaClient.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Uniconta.Common.Utility;
using Uniconta.ClientTools.BusinessRules;
using NPOI.HPSF;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class AppUserRibbonClient : AppUserMenuClient
    {
        public RowsType RowType { get; set; }
    }

    public partial class AppMenuPage : FormBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.AppMenuPage; }
        }

        public override Type TableType
        {
            get { return null; }
        }
        public override UnicontaBaseEntity ModifiedRow { get { return null; } set { } }

        public override void OnClosePage(object[] RefreshParams) { }

        Company company;

        public AppMenuPage(Company cmp) : base(cmp)
        {
            company = cmp;
            InitializeComponent();
            this.DataContext = this;
            ribbonControl = frmRibbon;
            BindControlType();
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            LoadAppMenus();
        }
        List<AppUserMenuClient> allAppMenus;
        async void LoadAppMenus()
        {
            var res = await api.Query<AppUserMenuClient>(App.MainPageObj.ActiveApp);
            allAppMenus = res.ToList();
        }
        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
                SaveUserMenu();
            else
                frmRibbon_BaseActions(ActionType);
        }

        private async void SaveUserMenu()
        {
            var newUserRibbonList = new List<AppUserRibbonClient>();
            var updatedUserRibbonList = new List<AppUserRibbonClient>();
            var deletedUserRibbonList = new List<AppUserRibbonClient>();
            foreach (var tn in ribbonTreeView.Items)
            {
                TreeViewItem item = (TreeViewItem)ribbonTreeView.ItemContainerGenerator.ContainerFromItem(tn);
                var treeNode = item.DataContext as Node;
                moduleName = "None";
                SaveNode(treeNode, newUserRibbonList, updatedUserRibbonList);
            }

            foreach (var menu in userRibbon)
            {
                if (menu.RowType == RowsType.DeletedRow)
                {
                    deletedUserRibbonList.Add(menu);
                }
            }

            var cnt = updatedUserRibbonList.Count + newUserRibbonList.Count + deletedUserRibbonList.Count;
            if (cnt != 0)
            {
                var errorCode = await api.MultiCrud(newUserRibbonList, updatedUserRibbonList, deletedUserRibbonList);
                if (errorCode == ErrorCodes.Succes)
                {
                    //UserMenuItems.Load(api);
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SavedOBJ"), Uniconta.ClientTools.Localization.lookup("UserRibbonMenu")), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
                else
                    UtilDisplay.ShowErrorCode(errorCode);
            }
        }

        string moduleName;

        bool disableChild = false;
        private void SaveNode(Node tn, List<AppUserRibbonClient> newUserRibbonList, List<AppUserRibbonClient> updatedUserRibbonList)
        {
            foreach (var ctn in tn.Child)
            {
                if ((bool)ctn.SaveNode)
                {
                    if (ctn.DeleteEnable == Visibility.Visible && ctn.AddEnable == Visibility.Visible)
                    {
                        if (ctn.RowType == RowsType.NewRow)
                        {
                            var userMenuClient = new AppUserRibbonClient();
                            userMenuClient.SetMaster(Utility.GetCurrentApp());
                            userMenuClient.ModuleNode = Uniconta.ClientTools.Localization.lookup(moduleName);

                            if (ctn.Child.Count == 0)
                            {
                                userMenuClient.ControlType = ctn.ControlName;
                                userMenuClient._Control = ctn.Control;
                                userMenuClient._Args = ctn.Argument;
                                userMenuClient._Key = ctn.ShortcutKey;
                            }
                            else
                                disableChild = ctn.Disable; //for disable/enable child items

                            userMenuClient._Prompt = ctn.Text;
                            userMenuClient._LineNumber = ctn.LineNumber;
                            userMenuClient._Disable = ctn.Disable;
                            userMenuClient._RibbonMenu = selectedControlName;
                            userMenuClient._MenuId = (byte)ctn.MenuId;

                            newUserRibbonList.Add(userMenuClient);
                        }

                        if (ctn.RowType == RowsType.Default)
                        {
                            foreach (var menu in userRibbon)
                            {
                                if (menu.RowId == ctn.RowId)
                                {
                                    menu._Prompt = ctn.Text;
                                    menu._Control = ctn.Control;
                                    menu.ControlType = ctn.ControlName;
                                    menu._LineNumber = ctn.LineNumber;
                                    menu._Args = ctn.Argument;
                                    menu._Key = ctn.ShortcutKey;
                                    menu._Disable = ctn.Disable;
                                    if (ctn.Child.Count > 0)
                                        disableChild = ctn.Disable; //for disable/enable child items
                                    menu._RibbonMenu = selectedControlName;
                                    menu._MenuId = (byte)ctn.MenuId;

                                    updatedUserRibbonList.Add(menu);
                                }
                            }
                        }
                    }

                    if (ctn.DeleteEnable == Visibility.Visible && ctn.AddEnable == Visibility.Collapsed)
                    {
                        if (ctn.RowType == RowsType.NewRow)
                        {
                            var userMenuClient = new AppUserRibbonClient();
                            userMenuClient.SetMaster(Utility.GetCurrentApp());
                            userMenuClient.ModuleNode = Uniconta.ClientTools.Localization.lookup(moduleName);
                            userMenuClient.ControlType = ctn.ControlName;
                            if (ctn.ChildNodeIndex >= 0)
                                userMenuClient._ChildNode = NumberConvert.ToString(ctn.ChildNodeIndex);
                            userMenuClient._Prompt = ctn.Text;
                            userMenuClient._Control = ctn.Control;
                            userMenuClient._LineNumber = ctn.LineNumber;
                            userMenuClient._Args = ctn.Argument;
                            userMenuClient._Key = ctn.ShortcutKey;
                            if (disableChild)   //for disable/enable if parent is disable/
                                userMenuClient._Disable = disableChild;
                            else if (!disableChild && ctn.Disable)
                                userMenuClient._Disable = ctn.Disable;
                            else
                                userMenuClient._Disable = ctn.Disable;

                            userMenuClient._RibbonMenu = selectedControlName;
                            userMenuClient._MenuId = (byte)ctn.MenuId;

                            newUserRibbonList.Add(userMenuClient);
                        }

                        if (ctn.RowType == RowsType.Default)
                        {
                            foreach (var menu in userRibbon)
                            {
                                if (menu.RowId == ctn.RowId)
                                {
                                    menu._ChildNode = NumberConvert.ToString(ctn.ChildNodeIndex);
                                    menu._Prompt = ctn.Text;
                                    menu.ControlType = ctn.ControlName;
                                    menu._Control = ctn.Control;
                                    menu._LineNumber = ctn.LineNumber;
                                    menu._Args = ctn.Argument;
                                    menu._Key = ctn.ShortcutKey;
                                    if (disableChild)
                                        menu._Disable = disableChild;
                                    else if (!disableChild && ctn.Disable)
                                        menu._Disable = ctn.Disable;
                                    else
                                        menu._Disable = ctn.Disable;

                                    updatedUserRibbonList.Add(menu);
                                }
                            }
                        }

                    }
                    if (ctn.Child.Count > 0)
                    {
                        SaveNode(ctn, newUserRibbonList, updatedUserRibbonList);
                    }
                }
            }
        }

        void BindControlType()
        {
            var lst = new List<string>(UserControlProperties.GetControlNames);
            lst.Sort();
            cmbControlName.ItemsSource = lst;
            cmbControlType.IsEditable = false;
            cmbControlType.ItemsSource = GetControlTypeItemsSource();
            cmbControl.IsEditable = false;
            cmbControl.Visibility = Visibility.Collapsed;
            cmbPluginName.IsEditable = false;
            cmbPluginName.Visibility = Visibility.Collapsed;
            cmbTableHeader.IsEditable = false;
            cmbTableHeader.Visibility = Visibility.Collapsed;
            LoadBusinessRules();
        }


        private string[] GetControlTypeItemsSource()
        {
            var retVal = AppEnums.ControlTypes.Values;

            if (!Api.CompanyEntity.BusinessRulesEnabled())
            {
                retVal = retVal.Where(x => x != AppEnums.ControlType_BusinessRule).ToArray();
            }

            return retVal;
        }

        private ObservableCollection<Node> _nodes;
        public ObservableCollection<Node> Nodes
        {
            get { return _nodes; }
            set { _nodes = value; }
        }

        List<AppUserRibbonClient> userRibbon;

        List<TreeNode> categories;

        List<AppUserMenu> GetAllRibbonMenuItems(string controlName)
        {
            if (allAppMenus != null)
            {
                List<AppUserMenu> lst = null;
                foreach (var p in allAppMenus)
                {
                    if (p._RibbonMenu == controlName)
                    {
                        if (lst == null)
                            lst = new List<AppUserMenu>();
                        lst.Add(p);
                    }
                }
                return lst;
            }
            return null;
        }
        async void LoadRibbonMenu(string controlName)
        {
            var list = new List<TreeNode>();
            var treeNode = new TreeNode();
            var treeChildList = new List<TreeNode>();
            treeNode.Child = treeChildList;
            treeNode.Text = Uniconta.ClientTools.Localization.lookup("UserMenu");
            treeNode.RowType = true;
            treeNode.IsExpanded = true;
            userRibbon = new List<AppUserRibbonClient>();
            var menus = GetAllRibbonMenuItems(controlName);
            if (menus != null)
            {
                userRibbon.Capacity = menus.Count;
                foreach (var item in menus)
                {
                    var userMenu = new AppUserRibbonClient();
                    userMenu.Copy(item);
                    userRibbon.Add(userMenu);
                }
                treeChildList.AddRange(GetUserRibbonMenus());
            }
            list.Add(treeNode);
            categories = list;
            FillTree(categories);
        }

        private List<TreeNode> GetUserRibbonMenus()
        {
            var list = new List<TreeNode>();
            if (userRibbon.Count == 0)
                return list;
            List<AppUserRibbonClient> sorted;
            if (userRibbon.Count > 1)
                sorted = userRibbon.OrderBy(x => x.LineNumber).ToList();
            else
                sorted = userRibbon;
            foreach (var ur in sorted)
            {
                if (ur._ChildNode == null)
                {
                    var ribbonNode = new TreeNode();
                    ribbonNode.Text = ur._Prompt;
                    ribbonNode.ControlName = AppEnums.ControlTypes.ToString(ur._ControlType);
                    ribbonNode.Control = ur._Control;
                    ribbonNode.Argument = ur._Args;
                    ribbonNode.ShortcutKey = ur._Key;
                    ribbonNode.LineNumber = ur._LineNumber;
                    ribbonNode.Disable = ur._Disable;
                    ribbonNode.MenuId = ur._MenuId;
                    ribbonNode.Child = new List<TreeNode>();
                    ribbonNode.RowType = false;
                    ribbonNode.RowId = ur.RowId;
                    ribbonNode.ModuleChild = true;
                    ribbonNode.IsChecked = ur._Disable;
                    ribbonNode.IsExpanded = true;
                    list.Add(ribbonNode);
                }
            }

            foreach (var ribbonItem in list)
            {
                foreach (var item in sorted)
                {
                    if (item._ChildNode != null && item._MenuId == ribbonItem.MenuId)
                    {
                        var ribbonNode = new TreeNode();
                        ribbonNode.Text = item._Prompt;
                        ribbonNode.ControlName = AppEnums.ControlTypes.ToString(item._ControlType);
                        ribbonNode.Control = item._Control;
                        ribbonNode.Argument = item._Args;
                        ribbonNode.ShortcutKey = item._Key;
                        ribbonNode.LineNumber = item._LineNumber;
                        ribbonNode.Disable = item._Disable;
                        ribbonNode.MenuId = item._MenuId;
                        ribbonNode.ChildNodeIndex = item._ChildNode;
                        ribbonNode.Child = new List<TreeNode>();
                        ribbonNode.RowType = false;
                        ribbonNode.RowId = item.RowId;
                        ribbonNode.IsChecked = item._Disable;
                        ribbonNode.IsExpanded = true;
                        ribbonItem.Child.Add(ribbonNode);
                    }
                }
            }
            return list;
        }

        void FillTree(List<TreeNode> userCategory)
        {
            try
            {
                _nodes?.Clear();
                _nodes = new ObservableCollection<Node>();
                for (int i = 0; i < userCategory.Count; i++)
                {
                    var level1Category = userCategory[i];
                    var level1Items = CreateNode(userCategory[i]);
                    level1Items.IsSelected = true;
                    for (int j = 0; j < level1Category.Child.Count; j++)
                    {
                        var level2Category = level1Category.Child[j];
                        var level2Items = new Node();
                        bool addCategory = true;

                        if (level2Category.Child.Count > 0)
                        {
                            level2Items = CreateNode(level1Category.Child[j]);
                            level2Items.ChildCount = level2Category.Child.Count;
                            level2Items.Parent.Add(level1Items);
                            level1Items.Child.Add(level2Items);
                            addCategory = false;
                        }

                        if (!level2Category.RowType)
                        {
                            if (addCategory)
                            {
                                level2Items = CreateNode(level1Category.Child[j]);
                                level2Items.ChildCount = level2Category.Child.Count;
                                level2Items.Parent.Add(level1Items);
                                level1Items.Child.Add(level2Items);
                            }
                        }

                        for (int n = 0; n < level2Category.Child.Count; n++)
                        {
                            var level3Category = level2Category.Child[n];
                            var level3Items = new Node();
                            if (!level3Category.RowType)
                            {
                                level3Items = CreateNode(level2Category.Child[n]);
                                level3Items.ChildCount = level3Category.Child.Count;
                                level3Items.Parent.Add(level2Items);
                                level2Items.Child.Add(level3Items);
                            }

                            if (level3Category.Child.Count > 0)
                            {
                                level3Items.ChildCount = level3Category.Child.Count;
                                level3Items.Parent.Add(level2Items);
                            }

                            for (int k = 0; k < level3Category.Child.Count; k++)
                            {
                                var level4Category = level3Category.Child[k];
                                var level4Items = new Node();
                                level4Items = CreateNode(level3Category.Child[k]);
                                level4Items.ChildCount = level4Category.Child.Count;
                                level4Items.Parent.Add(level3Items);
                                level3Items.Child.Add(level4Items);

                                if (level4Category.Child.Count > 0)
                                {
                                    level4Items.ChildCount = level4Category.Child.Count;
                                    level4Items.Parent.Add(level3Items);
                                }
                            }
                        }
                    }
                    _nodes.Add(level1Items);
                }
                ribbonTreeView.ItemsSource = _nodes;
            }
            catch (Exception ex)
            {
                api.ReportException(ex, string.Format("CustomRibbonMenu.FillTree(), CompanyId={0}", api.CompanyId));
            }
        }

        static private Node CreateNode(TreeNode itemNode)
        {
            var node = new Node();
            node.ModuleName = itemNode.Text;

            if (itemNode.RowId != 0)
                node.Text = itemNode.Text;
            else
                // node.Text = itemNode.Text == null ? Uniconta.ClientTools.Localization.lookup(itemNode.Text.ToString()) : string.Format(Uniconta.ClientTools.Localization.lookup(itemNode.Text.ToString()), UtilFunctions.ToLowerConditional(itemNode.Text.ToString()));
                node.Text = Uniconta.ClientTools.Localization.lookup(itemNode.Text);

            node.Picture = itemNode.Picture;
            node.ControlName = itemNode.ControlName;
            node.Control = itemNode.Control;
            node.ControlCountryId = itemNode.ControlCountryId;
            node.ControlParm = itemNode.ControlParm;
            node.IsExpanded = itemNode.IsExpanded;
            node.Module = itemNode.Module;
            node.Argument = itemNode.Argument;
            node.ShortcutKey = itemNode.ShortcutKey;
            node.LineNumber = itemNode.LineNumber;
            node.DeleteEnable = itemNode.RowType == true ? Visibility.Collapsed : Visibility.Visible;
            node.AddEnable = NodeAddEnable(node, itemNode.RowType, itemNode) == true ? Visibility.Visible : Visibility.Collapsed;
            node.MenuId = itemNode.MenuId;
            node.ChildNodeIndex = (int)NumberConvert.ToInt(itemNode.ChildNodeIndex);
            node.RowType = RowsType.Default;
            node.SaveNode = !itemNode.RowType;
            node.RowId = itemNode.RowId;
            node.Disable = itemNode.Disable;
            node.IsChecked = itemNode.Disable;
            return node;
        }

        static private bool NodeAddEnable(Node node, bool rowType, TreeNode itemNode)
        {
            if (rowType)
                return true;
            else if (node.ModuleName != null && itemNode.Child.Count != 0)
                return true;
            else
                return itemNode.ModuleChild;
        }

        private static string GetAssemblyName()
        {
            string assemblyName;
            assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            return assemblyName;
        }

        private Node selectedNode;

        private void ribbonTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            object SelectedItem = null;
            var treeView = (System.Windows.Controls.TreeView)sender;
            SelectedItem = treeView.SelectedItem;
            selectedNode = (Node)SelectedItem;
            if (SelectedItem == null) return;

            const int countCheck = 0;
            if (selectedNode.SaveNode)
            {
                if (selectedNode.Parent.Count == 0 && selectedNode.Child.Count != 0)
                {
                    SaveTreeParent(selectedNode.Child, selectedNode.SaveNode);
                }

                if (selectedNode.Parent.Count > 0 && selectedNode.Child.Count > 0)
                {
                    SaveTreeChildMiddle(selectedNode.Parent, selectedNode.Child, selectedNode.SaveNode);
                }

                if (selectedNode.Parent.Count > 0 && selectedNode.Child.Count == 0)
                {
                    SaveTreeChild(selectedNode.Parent, countCheck);
                }
            }
        }

        private void SaveTreeChild(IEnumerable<Node> items, int countCheck)
        {
            bool isNull = false;
            foreach (Node paren in items)
            {
                foreach (Node child in paren.Child)
                {
                    if (child.SaveNode == true || child.SaveNode == true)
                    {
                        countCheck++;
                        if (child.SaveNode == true)
                        {
                            isNull = true;
                        }
                    }
                }
                if (countCheck != paren.Child.Count && countCheck != 0)
                {
                    paren.SaveNode = true;
                }
                else if (countCheck == 0)
                {
                    paren.SaveNode = false;
                }
                else if (countCheck == paren.Child.Count && isNull)
                {
                    paren.SaveNode = true;
                }
                else if (countCheck == paren.Child.Count && !isNull)
                {
                    paren.SaveNode = true;
                }

                if (paren.Parent.Count != 0)
                {
                    this.SaveTreeChild(paren.Parent, 0);
                }
            }
        }

        private void SaveTreeChildMiddle(IEnumerable<Node> itemsParent, IEnumerable<Node> itemsChild, bool isNodeToBeSaved)
        {
            const int CountSaveNode = 0;
            this.SaveTreeParent(itemsChild, isNodeToBeSaved);
            this.SaveTreeChild(itemsParent, CountSaveNode);
        }

        private void SaveTreeParent(IEnumerable<Node> items, bool isNodeToBeSaved)
        {
            foreach (Node item in items)
            {
                item.SaveNode = isNodeToBeSaved;
                if (item.Child.Count != 0)
                    SaveTreeParent(item.Child, isNodeToBeSaved);
            }
        }

        private void ribbonTreeView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string newNode = Uniconta.ClientTools.Localization.lookup("NewMenu");

            var button = (System.Windows.Controls.Button)sender;
            selectedNode = (Node)button.DataContext;
            selectedNode.IsExpanded = true;

            if (selectedNode != null)
            {
                var node = new Node();
                node.Text = newNode;
                node.DeleteEnable = Visibility.Visible;
                node.AddEnable = Visibility.Collapsed;
                node.SaveNode = true;
                node.RibbonMenu = selectedControlName;
                node.Parent.Add(selectedNode);
                node.ParentNode = selectedNode.Text;
                node.LineNumber = 1;
                node.IsSelected = true;

                if (selectedNode.ModuleName != null)
                {
                    node.AddEnable = Visibility.Visible;
                }

                if (selectedNode.AddEnable == Visibility.Visible && selectedNode.DeleteEnable == Visibility.Visible)
                {
                    node.AddEnable = Visibility.Collapsed;
                }

                if (selectedNode.AddEnable == Visibility.Visible && selectedNode.DeleteEnable == Visibility.Visible)
                {
                    node.MenuId = selectedNode.MenuId;
                    node.ChildNodeIndex = selectedNode.Child.Count + 1;
                }
                else
                {
                    node.MenuId = selectedNode.Child.Count;
                    node.ChildNodeIndex = -1;
                }

                node.RowType = RowsType.NewRow;
                selectedNode.Child.Add(node);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = (System.Windows.Controls.Button)sender;
            Node item = (Node)btn.DataContext;

            if (item.SaveNode)
            {
                DeleteNode(Nodes, item.Id);
            }

            foreach (var menu in userRibbon)
            {
                if (menu.RowId == item.RowId)
                {
                    menu.RowType = RowsType.DeletedRow;
                    break;
                }
            }

            if (item.Child.Count != 0)
            {
                foreach (var child in item.Child)
                {
                    foreach (var menu in userRibbon)
                    {
                        if (menu.RowId == child.RowId)
                        {
                            menu.RowType = RowsType.DeletedRow;
                        }
                    }
                }
            }
        }

        private void DeleteNode(IList<Node> nodes, string id)
        {
            Node nodeToDelete = null;
            foreach (var node in nodes)
            {
                if (node.Id == id)
                {
                    nodeToDelete = node;
                    break;
                }
                DeleteNode(node.Child, id);
            }
            if (nodeToDelete != null)
                nodes.Remove(nodeToDelete);
        }

        string selectedControlName;

        private void cmbControlName_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var selectedItem = cmbControlName.SelectedItem;
            if (selectedItem != null)
            {
                selectedControlName = selectedItem.ToString();
                LoadRibbonMenu(selectedItem.ToString());
            }
        }

        private void cmbControlType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbControlName.SelectedItem == null || cmbControlType.SelectedItem == null) return;
            var selectedItem = cmbControlType.SelectedItem;
            cmbBusinessRule.Visibility = Visibility.Collapsed;
            lblBusinessRule.Visibility = Visibility.Collapsed;
            lblControl.Visibility = Visibility.Visible;
            txtControl.Visibility = Visibility.Visible;
            cmbControl.Visibility = Visibility.Collapsed;
            cmbTableHeader.Visibility = Visibility.Collapsed;
            cmbPluginName.Visibility = Visibility.Collapsed;
            txtControl.IsReadOnly = false;

            var item = ribbonTreeView.SelectedItem as Node;

            if (selectedItem != null && item != null)
            {
                switch (selectedItem.ToString())
                {
                    case "ExportControl":
                        item.Control = "ExportPage2";
                        txtControl.IsReadOnly = true;
                        break;
                    case "ImportControl":
                        item.Control = "ImportPage";
                        txtControl.IsReadOnly = true;
                        break;
                    case "Form":
                        LoadControlNames(item.Control);
                        cmbControl.Visibility = Visibility.Visible;
                        txtControl.Visibility = Visibility.Collapsed;
                        cmbPluginName.Visibility = Visibility.Collapsed;
                        cmbTableHeader.Visibility = Visibility.Collapsed;
                        break;
                    case "Plugin":
                        LoadPluginDLLNames(item.Control);
                        txtControl.Visibility = Visibility.Collapsed;
                        cmbControl.Visibility = Visibility.Collapsed;
                        cmbTableHeader.Visibility = Visibility.Collapsed;
                        cmbPluginName.Visibility = Visibility.Visible;
                        break;
                    case "UserDefinedTableControl":
                        LoadTable(item.Control);
                        txtControl.Visibility = Visibility.Collapsed;
                        cmbControl.Visibility = Visibility.Collapsed;
                        cmbPluginName.Visibility = Visibility.Collapsed;
                        cmbTableHeader.Visibility = Visibility.Visible;
                        break;
                    case AppEnums.ControlType_BusinessRule:
                        lblControl.Visibility = Visibility.Collapsed;
                        lblBusinessRule.Visibility = Visibility.Visible;
                        cmbBusinessRule.Visibility = Visibility.Visible;
                        txtControl.Visibility = Visibility.Collapsed;
                        break;
                    case "ReportControl":

                        break;
                }
            }
        }

        async void LoadPluginDLLNames(string control)
        {
            var names = new List<string>();
            var pluginList = await api.Query<AppPluginClient>(Utility.GetCurrentApp());
            if (pluginList == null)
                return;
            foreach (var x in pluginList)
                if (x._Name != null && (x._ForCompanyId == 0 || x._ForCompanyId == api.CompanyId))
                    names.Add(x._Name);
            names.Sort();
            cmbPluginName.Text = string.Empty;
            cmbPluginName.ItemsSource = null;
            cmbPluginName.ItemsSource = names;

            if (control != null)
                cmbPluginName.SelectedIndex = names.IndexOf(control);
        }

        void LoadControlNames(string control)
        {
            var controls = new List<string>(UserControlProperties.GetControlNames);
            controls.Sort();
            cmbControl.Text = string.Empty;
            cmbControl.ItemsSource = null;
            cmbControl.ItemsSource = controls;

            if (control != null)
                cmbControl.SelectedIndex = controls.IndexOf(control);
        }

        private void LoadBusinessRules()
        {
            var businessRules = Api.Query<BusinessRuleClient>().Result;
            cmbBusinessRule.ItemsSource = businessRules.Select(r => new { Value = r.RowId.ToString(), Display = (string.IsNullOrEmpty(r.Comment) ? r.RowId.ToString() : r.Comment) + (r.Disable ? " [DISABLED]" : string.Empty) });
        }

        async void LoadTable(string control)
        {
            var tableLst = await api.Query<Uniconta.DataModel.AppTableHeader>();
            var controls = new List<string>(tableLst.Length);
            foreach (var item in tableLst)
                controls.Add(item._Name);

            controls.Sort();
            cmbTableHeader.Text = string.Empty;
            cmbTableHeader.ItemsSource = null;
            cmbTableHeader.ItemsSource = controls;

            if (control != null)
                cmbTableHeader.SelectedIndex = controls.IndexOf(control);
        }

        private void cmbControl_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbControl.SelectedIndex >= 0)
            {
                var item = ribbonTreeView.SelectedItem as Node;
                if (item != null && item.ControlName == "Form")
                    item.Control = cmbControl.SelectedItem.ToString();
            }
        }

        private void cmbPluginName_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbPluginName.SelectedIndex >= 0)
            {
                var item = ribbonTreeView.SelectedItem as Node;
                if (item != null && item.ControlName == "Plugin")
                    item.Control = cmbPluginName.SelectedItem.ToString();
            }
        }

        private void cmbTableHeader_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbTableHeader.SelectedIndex >= 0)
            {
                var item = ribbonTreeView.SelectedItem as Node;
                if (item != null && item.ControlName == "UserDefinedTableControl")
                    item.Control = cmbTableHeader.SelectedItem.ToString();
            }
        }

        private void disable_Checked(object sender, RoutedEventArgs e)
        {
            var selectedNode = ribbonTreeView.SelectedItem as Node;
            if (selectedNode != null)
            {
                if (selectedNode.DeleteEnable == Visibility.Collapsed)
                    return;

                var chkBox = sender as CheckEditor;
                if (chkBox != null)
                {
                    var isDisable = Uniconta.Common.Utility.Util.Value(chkBox.IsChecked);
                    foreach (var firstItem in selectedNode.Child)
                    {
                        firstItem.Disable = isDisable;
                        foreach (var secondItem in firstItem.Child)
                        {
                            secondItem.Disable = isDisable;
                            foreach (var thirdItem in secondItem.Child)
                            {
                                thirdItem.Disable = isDisable;
                                foreach (var fourthItem in thirdItem.Child)
                                    fourthItem.Disable = isDisable;
                            }
                        }
                    }
                }
            }
        }
    }
}
