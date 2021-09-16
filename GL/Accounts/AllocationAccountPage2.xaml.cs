using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class AllocationAccountPage2 : FormBasePage
    {
        GLSplitAccountClient editrow;
        public override string NameOfControl {get { return TabControls.AllocationAccountPage2.ToString(); }}
        public override Type TableType { get { return typeof(GLSplitAccountClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (GLSplitAccountClient)value; } }
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);           
        }
        public AllocationAccountPage2(UnicontaBaseEntity sourcedata, bool isEdit)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            InitPage(api);
        }
        public AllocationAccountPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtnumber, txtnumber);
#endif
        }
        void InitPage(CrudAPI crudapi)
        {
            if(LoadedRow== null)
                frmRibbon.DisableButtons(new string[] { "Delete" });
            leOffsetAccount.api =
            leTransType.api = leAlloc.api = leAllocdim1.api = leAllocdim2.api = leAllocdim3.api = leAllocdim4.api
            = leAllocdim5.api = leFrmdim1.api = leFrmdim2.api = leFrmdim3.api = leFrmdim4.api = leFrmdim5.api = crudapi;
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
            setDimensions();
            setVisible(editrow.Accrual);
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void CheckEditor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var selectedValue = ((ComboBoxEditor)sender).SelectedIndex;
            if (selectedValue >= 0)
                setVisible(selectedValue == 1);
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        private void setVisible(bool accrual)
        {
            grpAccrual.Visibility = accrual ? Visibility.Visible : Visibility.Collapsed;
            grpPCt.Visibility = accrual ? Visibility.Collapsed : Visibility.Visible;
        }

        private void setDimensions()
        {
            var c = api.CompanyEntity;

            lblAllocdim1.Label = lblfrmdim1.Label = c._Dim1;
            lblAllocdim2.Label = lblfrmdim2.Label = c._Dim2;
            lblAllocdim3.Label = lblfrmdim3.Label = c._Dim3;
            lblAllocdim4.Label = lblfrmdim4.Label = c._Dim4;
            lblAllocdim5.Label = lblfrmdim5.Label = c._Dim5;

            int noofDimensions = c.NumberOfDimensions;
            if (noofDimensions < 5)
                lblAllocdim5.Visibility = lblfrmdim5.Visibility = leFrmdim5.Visibility = leAllocdim5.Visibility = Visibility.Collapsed;
            if (noofDimensions < 4)
                lblAllocdim4.Visibility = lblfrmdim4.Visibility = leFrmdim4.Visibility = leAllocdim4.Visibility = Visibility.Collapsed;
            if (noofDimensions < 3)
                lblAllocdim3.Visibility = lblfrmdim3.Visibility = leFrmdim3.Visibility = leAllocdim3.Visibility = Visibility.Collapsed;
            if (noofDimensions < 2)
                lblAllocdim2.Visibility = lblfrmdim2.Visibility = leFrmdim2.Visibility = leAllocdim2.Visibility = Visibility.Collapsed;
            if (noofDimensions < 1)
                lblAllocdim1.Visibility = lblfrmdim1.Visibility = leFrmdim1.Visibility = leAllocdim1.Visibility = Visibility.Collapsed;
        }      

    }
}
