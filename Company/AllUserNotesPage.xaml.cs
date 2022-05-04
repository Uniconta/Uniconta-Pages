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
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AllUserNotesPageGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(UserNotesClient); }
        }
        public override IComparer GridSorting { get { return new SortNote(); } }

        public override bool Readonly { get { return true; } }
    }

public partial class AllUserNotesPage : GridBasePage
    {
        public AllUserNotesPage(UnicontaBaseEntity rec, CrudAPI api) : base(api, string.Empty)
        {
            InitPage(new List<UnicontaBaseEntity>() { rec });
        }

        public AllUserNotesPage(List<UnicontaBaseEntity> masters, CrudAPI api) : base(api, string.Empty)
        {
            InitPage(masters);
        }

        public AllUserNotesPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        void InitPage(List<UnicontaBaseEntity> rec)
        {
            InitializeComponent();
            dgNotesGrid.BusyIndicator = busyIndicator;
            dgNotesGrid.api = api;
            SetRibbonControl(localMenu, dgNotesGrid);
            dgNotesGrid.masterRecords = rec;
            localMenu.OnItemClicked += gridRibbon_BaseActions;
            RemoveMenuItem();
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var Comp = api.CompanyEntity;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "ViewDownloadRow"});
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var note = dg.SelectedItem as UserNotesClient;
            if (note != null && dg.CurrentColumn?.Name == "KeyStr")
                lookup.TableType = Global.ClassId2BaseType(note.CompanyId, note._TableId);
            return lookup;
        }
    }
}
