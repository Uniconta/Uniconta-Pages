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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorPaymentFormatGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorPaymentFormatClient); } }
    }
    public partial class CreditorPaymentFormatPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.CreditorPaymentFormatPage.ToString(); }
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public CreditorPaymentFormatPage(BaseAPI API)
            : base(API, string.Empty)
        {
            Init();
        }
        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgCreditorPymtFrmtGrid);
            dgCreditorPymtFrmtGrid.api = api;
            dgCreditorPymtFrmtGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorPymtFrmtGrid.SelectedItem as CreditorPaymentFormatClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.CreditorPaymentFormatPage2, api, Uniconta.ClientTools.Localization.lookup("PaymentFormats"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;
                    AddDockItem(TabControls.CreditorPaymentFormatPage2, EditParam, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("PaymentFormats"), selectedItem.Name));
                    break;
                case "CopyRow":
                    if (selectedItem == null)
                        return;
                    object[] copyParam = new object[2];
                    copyParam[0] = selectedItem;
                    copyParam[1] = false;
                    string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem.Format);
                    AddDockItem(TabControls.CreditorPaymentFormatPage2, copyParam, header);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorPaymentFormatPage2)
                dgCreditorPymtFrmtGrid.UpdateItemSource(argument);
        }
        
        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount)});
        }

    }
}
