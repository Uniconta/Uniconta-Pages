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
    public class NumberSerieGrid : CorasauDataGridClient
    {
        public override bool SingleBufferUpdate { get { return false; } }
        public override Type TableType { get { return typeof(NumberSerieClient); } }
    }

    public partial class NumberSerie : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.NumberSerie; } }
        protected override bool IsLayoutSaveRequired() { return false; }

        public NumberSerie(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgNumberSerie);
            dgNumberSerie.api = this.api;
            dgNumberSerie.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        public NumberSerie(BaseAPI API) : this(API, string.Empty)
        {
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.NumberSeriePage2)
                dgNumberSerie.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgNumberSerie.SelectedItem as NumberSerieClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.NumberSeriePage2, api, Uniconta.ClientTools.Localization.lookup("NumberSerie"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.NumberSeriePage2, selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
