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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.User;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class EditCompanyUser : FormBasePage
    {
        CompanyUserAccessClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }

        public override Type TableType { get { return typeof(CompanyUserAccessClient); } }
        public override string NameOfControl { get { return TabControls.EditCompanyUser.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanyUserAccessClient)value; } }
        public EditCompanyUser(UnicontaBaseEntity sourcedata, bool IsEdit) :  base(sourcedata, IsEdit)
        {
            InitializeComponent();
            layoutControl = layoutItems;
            cbProfile.ItemsSource = AppEnums.FixedProfiles.Values;
            var Rights = editrow._Rights;
            var profile = AccessLevel.GetFixedProfile(Rights);
            cbProfile.SelectedIndex = (int)profile;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            LoadMenus();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        async void LoadMenus()
        {
            List<CompanyMainMenu> compMainMenuLst = new List<CompanyMainMenu>();
            var Comp = api.CompanyEntity;
            var menus = Comp.GetCache(typeof(Uniconta.DataModel.CompanyMainMenu)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CompanyMainMenu), api);
            var menuLst= menus?.GetRecords as IEnumerable<CompanyMainMenu>;
            if(menuLst?.Count() > 0)
            {
                compMainMenuLst.Capacity = menuLst.Count() + 1;
                compMainMenuLst.Add(new CompanyMainMenu() { _Name = "" });
                compMainMenuLst.AddRange(menuLst);
                compMainMenuLst.Sort(SQLCache.KeyStrSorter);
            }
            cmbMenu.ItemsSource = compMainMenuLst;
            cmbMenu.SelectedItem = menus.Get(editrow._Menu);
        }

        private void cbProfile_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cbProfile.SelectedIndex == -1)
            return;
            var index = cbProfile.SelectedIndex;
            var profile = (FixedProfiles)index;
            editrow._Rights = AccessLevel.SetFixedProfile(editrow._Rights, profile);
        }

        private void cmbMenu_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var selectedMenu = cmbMenu.SelectedItem as CompanyMainMenu;
            if (selectedMenu!= null)
            {
                editrow.Menu = selectedMenu.RowId;
                editrow.NotifyPropertyChanged("Menu");
            }
            else
            {
                editrow.Menu = 0;
                editrow.NotifyPropertyChanged("Menu");
            }
        }

        public override bool CheckCompany
        {
            get
            {
                    return false;
            }
        }
    }
}
