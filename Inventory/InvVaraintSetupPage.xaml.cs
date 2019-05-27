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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InvVaraintSetupPage : FormBasePage
    {
        CompanyClient editrow;

        public override string NameOfControl { get { return TabControls.InvVaraintSetupPage; } }
        public override Type TableType { get { return typeof(CompanyClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanyClient)value; } }

        public InvVaraintSetupPage(UnicontaBaseEntity sourceData) : base(sourceData, true)
        {
            InitializeComponent();
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            CompanyClient companyClient = new CompanyClient();
            StreamingManager.Copy(api.CompanyEntity, companyClient);
            editrow = companyClient;
            layoutItems.DataContext = editrow;
            SetVariants(editrow.NumberOfVariants);
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            if(ActionType == "Save" && !ValidateForm())
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Variants")), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }
            frmRibbon_BaseActions(ActionType);
        }

        private bool ValidateForm()
        {
            if (txtVar5.Visibility == Visibility.Visible)
                return string.IsNullOrWhiteSpace(txtVar5.Text) ? false : string.IsNullOrWhiteSpace(txtVar4.Text) ? false : string.IsNullOrWhiteSpace(txtVar3.Text) ?
                   false : string.IsNullOrWhiteSpace(txtVar2.Text) ? false : string.IsNullOrWhiteSpace(txtVar1.Text) ? false : true;
            else if (txtVar4.Visibility == Visibility.Visible)
                return string.IsNullOrWhiteSpace(txtVar4.Text) ? false : string.IsNullOrWhiteSpace(txtVar3.Text) ? false : string.IsNullOrWhiteSpace(txtVar2.Text) ?
                    false : string.IsNullOrWhiteSpace(txtVar1.Text) ? false : true;
            else if (txtVar3.Visibility == Visibility.Visible)
                return string.IsNullOrWhiteSpace(txtVar3.Text) ? false : string.IsNullOrWhiteSpace(txtVar2.Text) ? false : string.IsNullOrWhiteSpace(txtVar1.Text) ? false : true;
            else if (txtVar2.Visibility == Visibility.Visible)
                return string.IsNullOrWhiteSpace(txtVar2.Text) ? false : string.IsNullOrWhiteSpace(txtVar1.Text) ? false : true;
            else if (txtVar1.Visibility == Visibility.Visible)
                return string.IsNullOrWhiteSpace(txtVar1.Text) ? false : true;
            return true;
        }

        private void SetVariants(int noofVariants)
        {
            lblVar5.Visibility = txtVar5.Visibility = (noofVariants < 5) ? Visibility.Collapsed : Visibility.Visible;
            lblVar4.Visibility = txtVar4.Visibility = (noofVariants < 4) ? Visibility.Collapsed : Visibility.Visible;
            lblVar3.Visibility = txtVar3.Visibility = (noofVariants < 3) ? Visibility.Collapsed : Visibility.Visible;
            lblVar2.Visibility = txtVar2.Visibility = (noofVariants < 2) ? Visibility.Collapsed : Visibility.Visible;
            lblVar1.Visibility = txtVar1.Visibility = (noofVariants < 1) ? Visibility.Collapsed : Visibility.Visible;
        }

        public override void OnClosePage(object[] refreshParams)
        {
            globalEvents.OnRefresh(NameOfControl);
        }

        private void NumericUpDownEditor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            SetVariants(Convert.ToInt32(e.NewValue));
        }
    }
}
