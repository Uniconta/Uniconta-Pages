using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
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
using System.Windows;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ClosingSheetPage2 : FormBasePage
    {
        GLClosingSheetClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.ClosingSheetPage2.ToString(); } }
        public override Type TableType { get { return typeof(GLClosingSheetClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (GLClosingSheetClient)value; } }
        public ClosingSheetPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public ClosingSheetPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtName, txtName);
#endif
        }
        void InitPage(CrudAPI crudapi)
        {
            leFromAccount.api = dim1lookupeditior.api = dim2lookupeditior.api = dim3lookupeditior.api = dim4lookupeditior.api = dim5lookupeditior.api =
            leNumberSerie.api = leOffsetAccount.api = leToAccount.api = leTransType.api = crudapi;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as GLClosingSheetClient;
            }
            layoutItems.DataContext = editrow;
            Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, useDim);
            setIncludeAll();
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            AcItem.ButtonClicked += AcItem_ButtonClicked;
        }

        void AcItem_ButtonClicked(object sender)
        {
            btnGoNumberSeries(null, null);
        }
        void setIncludeAll()
        {
            int noofDimensions =api.CompanyEntity.NumberOfDimensions;
            if (noofDimensions < 5)
                chkIncludeAll5.Visibility = Visibility.Collapsed;
            if (noofDimensions < 4)
                chkIncludeAll4.Visibility = Visibility.Collapsed;
            if (noofDimensions < 3)
                chkIncludeAll3.Visibility = Visibility.Collapsed;
            if (noofDimensions < 2)
                chkIncludeAll2.Visibility = Visibility.Collapsed;
            if (noofDimensions < 1)
                chkIncludeAll1.Visibility = Visibility.Collapsed;
        }
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
		private async void btnGoNumberSeries(object sender, RoutedEventArgs e)
		{
			NumberSerieClient nsc = (NumberSerieClient)await GetReference(editrow.NumberSerie, typeof(NumberSerieClient));
			if (nsc != null)
				AddDockItem(TabControls.NumberSeriePage2, nsc);
		}

	}
}
