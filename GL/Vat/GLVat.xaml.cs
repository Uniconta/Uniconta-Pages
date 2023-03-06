using Uniconta.API.Service;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLVatGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(GLVatClient); }
        }
    }
    public partial class GLVatPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.GL_Vat.ToString(); }
        }
        protected override Filter[] DefaultFilters()
        {
            Filter vatFilter = new Filter();
            vatFilter.name = "Vat";
            Filter nameFilter = new Filter();
            nameFilter.name = "Name";
            return new Filter[] { vatFilter, nameFilter };
        }
        public GLVatPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        private void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgVat;
            SetRibbonControl(localMenu, dgVat);
            dgVat.api = api;
            dgVat.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        public GLVatPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GLVatPage2)
                dgVat.UpdateItemSource(argument);
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgVat.SelectedItem as GLVatClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.GLVatPage2, api, Uniconta.ClientTools.Localization.lookup("Vat"), "Add_16x16.png");
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        CopyRecord(selectedItem);
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.GLVatPage2, selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyRecord(GLVatClient selectedItem)
        {
            var vat = Activator.CreateInstance(selectedItem.GetType()) as GLVatClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, vat);
            AddDockItem(TabControls.GLVatPage2, new object[2] { vat, false }, Uniconta.ClientTools.Localization.lookup("Vat"), "Add_16x16.png");
        }
    }
}
