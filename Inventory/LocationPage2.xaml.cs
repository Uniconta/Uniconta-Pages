using Uniconta.API.System;
using Corasau.Client.Models;
using Corasau.Client.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
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
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;

namespace Corasau.Client.Pages
{
    public partial class LocationPage2 : FormBasePage
    {
        InvLocationClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            Utility.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.LocationPage2.ToString(); } }

        public override Type TableType { get { return typeof(InvLocationClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (InvLocationClient)value; } }
        public LocationPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata,true)
        {
            InitializeComponent();
            InitPage();
        }
        public LocationPage2(CrudAPI crudApi, UnicontaBaseEntity master)
            :base(crudApi, null)
        {
            InitializeComponent();
            InitPage(master);
        }
        void InitPage(UnicontaBaseEntity master=null)
        {           
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as InvLocationClient;
                editrow.SetMaster(master);
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
    }
}
