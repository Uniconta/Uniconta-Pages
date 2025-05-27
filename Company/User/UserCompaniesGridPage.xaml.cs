using UnicontaClient.Controls;
using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using DevExpress.Xpf.Docking;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserCompaniesGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CompanyClient); } }
        public override bool Readonly { get { return true; } }
    }

    /// <summary>
    /// Interaction logic for UserCompanies.xaml
    /// </summary>
    public partial class UserCompaniesGridPage : GridBasePage
    {
        public override string NameOfControl { get { return Uniconta.ClientTools.UnicontaTabs.UserCompaniesGridPage; } }

        UnicontaBaseEntity selectedCompany;
        bool queryLocal;
        public UserCompaniesGridPage(UnicontaBaseEntity baseEntity) : base(null)
        {
            InitPage(baseEntity);
        }

        public UserCompaniesGridPage(CrudAPI api) : base(api, string.Empty)
        {
            InitPage(null);
        }

        void InitPage(UnicontaBaseEntity baseEntity)
        {
            this.DataContext = this;
            InitializeComponent();
            queryLocal = true;
            selectedCompany = baseEntity;
            dgUserCompaniesGridClient.api = api;
            dgUserCompaniesGridClient.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgUserCompaniesGridClient);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgUserCompaniesGridClient.RowDoubleClick += DgUserCompaniesGridClient_RowDoubleClick;
            dgUserCompaniesGridClient.Loaded += DgUserCompaniesGridClient_Loaded;
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                localMenu.PerformRibbonAction("JumpTo");
            }
            else
                base.OnPreviewKeyDown(e);
        }

        static System.Windows.Point position = new System.Windows.Point();
        static double pageHeight = 600.0d, pageWidth = 900.0d;
        protected override void OnLayoutLoaded()
        {
            if (BasePage.session.User._Role >= (byte)Uniconta.Common.User.UserRoles.Accountant)
            {
                var currPanel = dockCtrl?.Activpanel;
                if (currPanel != null && currPanel.IsFloating)
                {
                    SetFloatGroup();
                    currPanel.Parent.FloatSize = new System.Windows.Size(pageWidth, pageHeight);
                    currPanel.SizeChanged += CurrPanel_SizeChanged;
                    currPanel.UpdateLayout();
                }

                this.NInbox.Visible = this.NInbox.ShowInColumnChooser = true;
                this.LastBankTrans.Visible = this.LastBankTrans.ShowInColumnChooser = true;
                this.LastPosting.Visible = this.LastPosting.ShowInColumnChooser = true;
            }
            base.OnLayoutLoaded();
        }

        private void SetFloatGroup()
        {
            var floatgrp = GetFloatGroup();
            if (floatgrp != null)
                floatgrp.FloatLocation = position;
        }

        private FloatGroup GetFloatGroup()
        {
            var curPanel = dockCtrl?.Activpanel;

            if (curPanel != null && curPanel.IsFloating && curPanel.Parent is FloatGroup)
                return curPanel.Parent as FloatGroup;

            return null;
        }

        private void CurrPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var size = e.NewSize;
            pageHeight = size.Height;
            pageWidth = size.Width;
        }
        private void DgUserCompaniesGridClient_Loaded(object sender, RoutedEventArgs e)
        {
            localMenu.SearchControl.KeyUp += SearchControl_KeyUp;
            localMenu.SearchControl.Focus();
        }

        private void SearchControl_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (dgUserCompaniesGridClient.VisibleRowCount > 0)
                {
                    dgUserCompaniesGridClient.Focus();
                    dgUserCompaniesGridClient.View.FocusedRowHandle = 0;
                    e.Handled = true;
                }
            }
        }

        private void DgUserCompaniesGridClient_RowDoubleClick()
        {
            localMenu.PerformRibbonAction("JumpTo");
        }

        public override Task InitQuery()
        {
            if (queryLocal)
            {
                GetCompanies();
                return null;
            }
            return base.InitQuery();
        }

        private async void GetCompanies(bool OnGridRefresh = false)
        {
            busyIndicator.IsBusy = true;
            dgUserCompaniesGridClient.Visibility = Visibility.Visible;
            dgUserCompaniesGridClient.ItemsSource = null;
            Company[] loadedCompanies;
            if (OnGridRefresh)
            {
                loadedCompanies = await BasePage.session.GetCompanies();
                UnicontaClient.Controls.CWDefaultCompany.loadedCompanies = loadedCompanies;
            }
            else
                loadedCompanies = CWDefaultCompany.loadedCompanies;

            var companies = new CompanyClient[loadedCompanies.Length];

            int i = 0;
            foreach (var comp in CWDefaultCompany.loadedCompanies)
            {
                var cmp = comp as CompanyClient;
                if (cmp == null)
                {
                    cmp = new CompanyClient();
                    StreamingManager.Copy(comp, cmp);
                }
                companies[i] = cmp;
                i++;
            }

            dgUserCompaniesGridClient.SetSource(companies);
            dgUserCompaniesGridClient.SelectedItem = selectedCompany;
            busyIndicator.IsBusy = false;
        }

        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            localMenu.PerformRibbonAction("JumpTo");
        }

        private void NInbox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            localMenu.PerformRibbonAction("Inbox");
        }

        private void NJournal_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            localMenu.PerformRibbonAction("GL_DailyJournal");
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserCompaniesGridClient.SelectedItem as CompanyClient;
            switch (ActionType)
            {
                case "JumpTo":
                    if (selectedItem != null)
                    {
                        if (!selectedItem._Delete)
                        {
                            globalEvents?.NotifyCompanyChange(this, new Uniconta.ClientTools.Util.CompanyEventArgs(selectedItem));
                            CloseDockItem();
                        }
                        else
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CompanyDeleted"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    }
                    break;
                case "OpenNewWindow":
                    if (selectedItem != null)
                    {
                        if (!selectedItem._Delete)
                        {
                            var mainWindow = new HomePage(selectedItem.CompanyId, null);
                            mainWindow.Show();
                            CloseDockItem();
                        }
                        else
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CompanyDeleted"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    }
                    break;
                case "Cancel":
                    CloseDockItem();
                    break;
                case "RefreshGrid":
                    GetCompanies(true);
                    break;
                case "Inbox":
                    if (selectedItem != null)
                    {
                        if (!selectedItem._Delete)
                        {
                            var inboxNode = new UnicontaClient.TreeNode()
                            {
                                Text = UtilFunctions.ToLowerConditional("PhysicalVouchersInbox", null),
                                ControlName = TabControls.Vouchers,
                                Module = UtilFunctions.Intern("PhycicalVoucher"),
                                ControlType = (int)ControlTypes.Form,
                            };
                            globalEvents?.NotifyCompanyChange(this, new Uniconta.ClientTools.Util.CompanyEventArgs(selectedItem) { OpenPage = inboxNode });
                            CloseDockItem();
                        }
                        else
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CompanyDeleted"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    }
                    break;
                case "GL_DailyJournal":
                    if (selectedItem != null)
                    {
                        if (!selectedItem._Delete)
                        {
                            var PostingNode = new UnicontaClient.TreeNode()
                            {
                                Text = UtilFunctions.ToLowerConditional("Posting", null),
                                ControlName = TabControls.GL_DailyJournal,
                                ControlType = (int)ControlTypes.Form,
                            };
                            globalEvents?.NotifyCompanyChange(this, new Uniconta.ClientTools.Util.CompanyEventArgs(selectedItem) { OpenPage = PostingNode });
                            CloseDockItem();
                        }
                        else
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CompanyDeleted"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
