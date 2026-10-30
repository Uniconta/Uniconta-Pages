using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.WindowsAPI.ClientTools.DataModel.DC;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DCGroupCurrencySummeryGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DCGroupCurrencySummeryAccountClient); } }
        public override bool Readonly => false;
        public override bool IsAutoSave => false;
    }

    public partial class DCGroupCurrencySummeryPage : GridBasePage
    {
        public override string NameOfControl => TabControls.DCGroupCurrencySummeryPage;

        public DCGroupCurrencySummeryPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();

            dgGrid.api = api;
            dgGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            dgGrid.UpdateMaster(master);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgGrid.AddRow();
                    break;
                case "DeleteRow":
                    dgGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
