using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PaymentGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(PaymentTermClient); }
        }
    }

    public partial class GL_PaymentTerms : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.GL_PaymentTerms; }
        }
        public GL_PaymentTerms(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitializeComponent();
            Init();
        }
        public GL_PaymentTerms(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            localMenu.dataGrid = dgPayment;
            SetRibbonControl(localMenu, dgPayment);
            dgPayment.api = api;
            dgPayment.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.PaymentTermsPage2)
                dgPayment.UpdateItemSource(argument);
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgPayment.SelectedItem as PaymentTermClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.PaymentTermsPage2, api, Uniconta.ClientTools.Localization.lookup("PaymentTerms"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.PaymentTermsPage2, selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
