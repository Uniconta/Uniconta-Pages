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
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DeliveryTermTypesPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DeliveryTermClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class DeliveryTermTypes : GridBasePage
    {
        protected override bool IsLayoutSaveRequired() { return false; }

        public DeliveryTermTypes(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgPaymentTermTypePageGrid);
            dgPaymentTermTypePageGrid.api = api;
            dgPaymentTermTypePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgPaymentTermTypePageGrid.AddRow();
                    break;
                case "CopyRow":
                    dgPaymentTermTypePageGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgPaymentTermTypePageGrid.SaveData();
                    break;
                case "DeleteRow":
                    dgPaymentTermTypePageGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
