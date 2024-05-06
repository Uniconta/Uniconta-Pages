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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CompanyAddressGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CompanyAddressClient); } }
    }

    public partial class CompanyAddressPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.CompanyAddressPage; } }

        public CompanyAddressPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage(null);
        }

        public CompanyAddressPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        public CompanyAddressPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master as Debtor);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgCompanyAddress.UpdateMaster(master);
            SetRibbonControl(localMenu, dgCompanyAddress);
            dgCompanyAddress.api = api;
            dgCompanyAddress.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CompanyAddressPage2)
                dgCompanyAddress.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCompanyAddress.SelectedItem as CompanyAddressClient;
            switch (ActionType)
            {
                case "AddRow":
                    var newItem = dgCompanyAddress.GetChildInstance();
                    object[] param = new object[3];
                    param[0] = newItem;
                    param[1] = false;
                    param[2] = dgCompanyAddress.masterRecord;
                    var header = Uniconta.ClientTools.Localization.lookup("CompanyAddress");
                    AddDockItem(TabControls.CompanyAddressPage2, param, header, "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                    {
                        object[] para = new object[3];
                        para[0] = selectedItem;
                        para[1] = true;
                        para[2] = dgCompanyAddress.masterRecord;
                        AddDockItem(TabControls.CompanyAddressPage2,para, selectedItem.Name, null,true);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }
    }
}
