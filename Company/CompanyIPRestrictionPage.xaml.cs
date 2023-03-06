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
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CompanyIPRestrictionPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CompanyIPRestrictionClient); } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave => false;
    }

    public partial class CompanyIPRestrictionPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.CompanyIPRestrictionPage; } }

        public CompanyIPRestrictionPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgCompanyIPRestriction;
            dgCompanyIPRestriction.api = api;
            dgCompanyIPRestriction.UpdateMaster(master);
            dgCompanyIPRestriction.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCompanyIPRestriction);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCompanyIPRestriction.SelectedItem as CompanyIPRestrictionClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgCompanyIPRestriction.AddRow();
                    break;
                case "DeleteRow":
                    dgCompanyIPRestriction.DeleteRow();
                    break;
                case "SaveGrid":
                    dgCompanyIPRestriction.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
