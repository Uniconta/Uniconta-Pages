using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
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
    public class DebtorMessagesPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorMessagesClient); } }
    }
    public partial class DebtorMessagesPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorMessagesPage; } }
        public DebtorMessagesPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgDebtorMessageGrid);
            localMenu.dataGrid = dgDebtorMessageGrid;
            dgDebtorMessageGrid.api = api;
            dgDebtorMessageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorMessageGrid.SelectedItem as DebtorMessagesClient;

            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.DebtorMessagesPage2, api, Uniconta.ClientTools.Localization.lookup("Message"), ";component/Assets/img/Add_16x16.png");
                    break;

                case "CopyRow":
                    if (selectedItem == null)
                        return;
                    object[] copyParam = new object[2];
                    copyParam[0] = StreamingManager.Clone(selectedItem);
                    copyParam[1] = false;
                    AddDockItem(TabControls.DebtorMessagesPage2, copyParam, string.Format("{0}: {1}", string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Message")), selectedItem._Name),
                        ";component/Assets/img/Copy_16x16.png");
                    break;

                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] editParam = new object[2];
                    editParam[0] = selectedItem;
                    editParam[1] = true;
                    AddDockItem(TabControls.DebtorMessagesPage2, editParam, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Message"), selectedItem._Name), ";component/Assets/img/Edit_16x16.png");
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorMessagesPage2)
                dgDebtorMessageGrid.UpdateItemSource(argument);
        }
    }
 }
