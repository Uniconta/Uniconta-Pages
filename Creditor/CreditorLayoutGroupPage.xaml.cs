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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorLayoutGroupGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorLayoutGroupClient); } }
    }
    /// <summary>
    /// Interaction logic for CreditorLayoutGroup.xaml
    /// </summary>
    public partial class CreditorLayoutGroupPage : GridBasePage
    {
        public override string NameOfControl { get { return UnicontaTabs.CreditorLayoutGroupPage; } }

        public CreditorLayoutGroupPage(BaseAPI api) : base(api, string.Empty)
        {
            Init();
        }

        public CreditorLayoutGroupPage(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgCreditorLayoutGroupGridClient);
            dgCreditorLayoutGroupGridClient.api = api;
            dgCreditorLayoutGroupGridClient.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorLayoutGroupGridClient.SelectedItem as CreditorLayoutGroupClient;

            switch (ActionType)
            {
                case "AddRow":
                    var newItem = dgCreditorLayoutGroupGridClient.GetChildInstance();
                    object[] param = new object[2];
                    param[0] = newItem;
                    param[1] = false;
                    AddDockItem(UnicontaTabs.CreditorLayoutGroupPage2, param, Uniconta.ClientTools.Localization.lookup("CreditorLayoutGroups"), "Add_16x16");
                    break;

                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] para = new object[2];
                    para[0] = selectedItem;
                    para[1] = true;
                    AddDockItem(UnicontaTabs.CreditorLayoutGroupPage2, para, string.Format(string.Concat(Uniconta.ClientTools.Localization.lookup("Creditor"), " ",
                        Uniconta.ClientTools.Localization.lookup("LayoutGroup"), ": {0}"), selectedItem.Name),
                        null, true);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == UnicontaTabs.CreditorLayoutGroupPage2)
                dgCreditorLayoutGroupGridClient.UpdateItemSource(argument);
        }
    }
}
