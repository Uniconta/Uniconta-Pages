using UnicontaClient.Models;
using UnicontaClient.Utilities;
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
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class GLSplitLinePage2 : FormBasePage
    {
        GLSplitLineClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.GlSplitLinePage2.ToString(); } }
        public override Type TableType { get { return typeof(GLSplitLineClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (GLSplitLineClient)value; } }
        public GLSplitLinePage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public GLSplitLinePage2(UnicontaBaseEntity sourcedata, CrudAPI crudApi)
            : base(null)
        {
            InitializeComponent();
            InitPage(crudApi, sourcedata);
        }
        void InitPage(CrudAPI crudapi,UnicontaBaseEntity master=null)
        {
            layoutControl = layoutItems;
            lkAllocAccount.api = lkTransType.api =cmbDim1.api=cmbDim2.api=cmbDim3.api=cmbDim4.api=cmbDim5.api=leOffsetAccount.api= crudapi;
            if (api.CompanyEntity.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as GLSplitLineClient;
                editrow._Accrual = true;
                editrow._Pct = 100d;
                if (master != null)
                    editrow.SetMaster(master);
            }
            layoutItems.DataContext = editrow;
            setVisible(editrow._Accrual);
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        private void ComboBoxEditor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var selectedValue = ((ComboBoxEditor)sender).SelectedIndex;
            if (selectedValue >= 0)
                setVisible(selectedValue == 1);
        }
        private void setVisible(bool accrual)
        {
            grpAccrual.Visibility = accrual ? Visibility.Visible : Visibility.Collapsed;
            grpPCt.Visibility = accrual ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
