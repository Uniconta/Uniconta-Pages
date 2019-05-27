using UnicontaClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using System.Collections;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvVariantCombiGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvVariantCombiClient); } }
        public override IComparer GridSorting
        {
            get
            {
                return new InvVariantCombiSort();
            }
        }
    }

    public partial class InvVariantCombiPage : GridBasePage
    {
        public InvVariantCombiPage(BaseAPI API):base(API, string.Empty)
        {
            InitializeComponent();
            InitControls();
        }
        public InvVariantCombiPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitializeComponent();
            InitControls();
        }
        public override string NameOfControl { get { return TabControls.InvVariantCombiPage; } }

        public InvVariantCombiPage(UnicontaBaseEntity sourceMaster) : base(sourceMaster)
        {
            InitializeComponent();
            InitControls(sourceMaster);
        }

        private void InitControls(UnicontaBaseEntity master = null)
        {
            dgStandardVariantLines.UpdateMaster(master);
            Utilities.Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, null, null, null, null, null);
            SetRibbonControl(localMenu, dgStandardVariantLines);
            if (master != null)
                RemoveMenuItem();

            dgStandardVariantLines.api = api;
            dgStandardVariantLines.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgStandardVariantLines.masterRecords == null);
            colItem.Visible = showFields;
            Name.Visible = showFields;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            gridRibbon_BaseActions(ActionType);
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "Filter", "ClearFilter" });
        }
    }
}
