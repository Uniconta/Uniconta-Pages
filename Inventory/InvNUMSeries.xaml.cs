using Uniconta.API.System;
using Corasau.Client.Models;
using Corasau.Client.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Corasau.Client.Pages
{
    public partial class InvNUMSeries : FormBasePage
    {
        CompanySettingsClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.CreditorInvoicenumberseries.ToString(); } }
        public override Type TableType { get { return typeof(CompanySettingsClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanySettingsClient)value; } }
        public InvNUMSeries(UnicontaBaseEntity sourceData)
            : base(sourceData, true)
        {
            InitializeComponent();
            cmbInventoryJournalSerie.api = api;
            layoutControl = layoutItems;

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save": this.saveForm();
                    var company = api.CompanyEntity;
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
