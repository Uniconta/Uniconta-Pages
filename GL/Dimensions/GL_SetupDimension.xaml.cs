using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Controls;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class GL_SetupDimension : FormBasePage
    {
        CompanyClient editrow;

        public override string NameOfControl { get { return TabControls.GL_SetupDimension.ToString(); } }

        public override Type TableType { get { return typeof(CompanyClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanyClient)value; } }

        public GL_SetupDimension(UnicontaBaseEntity sourceData)
            : base(sourceData, true)
        {
            InitializeComponent();
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            CompanyClient compClient = new CompanyClient();
            StreamingManager.Copy(api.CompanyEntity, compClient);
            editrow =compClient as CompanyClient;
            layoutItems.DataContext = editrow;
            SetDimensions(editrow.NumberOfDimensions);
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save" && !ValidateForm())
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Dimensions")), 
                    Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }
            frmRibbon_BaseActions(ActionType);
        }

        private bool ValidateForm()
        {
            if (txtDim5.Visibility == Visibility.Visible)
                return string.IsNullOrWhiteSpace(txtDim5.Text) ? false : string.IsNullOrWhiteSpace(txtDim4.Text) ? false : string.IsNullOrWhiteSpace(txtDim3.Text) ?
                   false : string.IsNullOrWhiteSpace(txtDim2.Text) ? false : string.IsNullOrWhiteSpace(txtDim1.Text) ? false : true;
            else if (txtDim4.Visibility == Visibility.Visible)
                return string.IsNullOrWhiteSpace(txtDim4.Text) ? false : string.IsNullOrWhiteSpace(txtDim3.Text) ? false : string.IsNullOrWhiteSpace(txtDim2.Text) ?
                    false : string.IsNullOrWhiteSpace(txtDim1.Text) ? false : true;
            else if (txtDim3.Visibility == Visibility.Visible)
                return string.IsNullOrWhiteSpace(txtDim3.Text) ? false : string.IsNullOrWhiteSpace(txtDim2.Text) ? false : string.IsNullOrWhiteSpace(txtDim1.Text) ? false : true;
            else if (txtDim2.Visibility == Visibility.Visible)
                return string.IsNullOrWhiteSpace(txtDim2.Text) ? false : string.IsNullOrWhiteSpace(txtDim1.Text) ? false : true;
            else if (txtDim1.Visibility == Visibility.Visible)
                return string.IsNullOrWhiteSpace(txtDim1.Text) ? false : true;
            return true;
        }

        private void SetDimensions(int noofDimensions)
        {
            lbldim5.Visibility = txtDim5.Visibility = (noofDimensions < 5) ? Visibility.Collapsed : Visibility.Visible;
            lbldim4.Visibility = txtDim4.Visibility = (noofDimensions < 4) ? Visibility.Collapsed : Visibility.Visible;
            lbldim3.Visibility = txtDim3.Visibility = (noofDimensions < 3) ? Visibility.Collapsed : Visibility.Visible;
            lbldim2.Visibility = txtDim2.Visibility = (noofDimensions < 2) ? Visibility.Collapsed : Visibility.Visible;
            lbldim1.Visibility = txtDim1.Visibility = (noofDimensions < 1) ? Visibility.Collapsed : Visibility.Visible;
        }

        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl);
        }

        private void NumericUpDownEditor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            SetDimensions(Convert.ToInt32(e.NewValue));
        }
    }
}
