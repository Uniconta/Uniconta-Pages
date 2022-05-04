using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class EmployeePage2 : FormBasePage
    {
        EmployeeClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.EmployeePage2.ToString(); } }
        public override Type TableType { get { return typeof(EmployeeClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (EmployeeClient)value; } }
        /*For Edit*/
        public EmployeePage2(UnicontaBaseEntity sourcedata, bool isEdit = true)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
            {
                editrow = (EmployeeClient)StreamingManager.Clone(sourcedata);
                editrow._Number = null;
            }
            InitPage(api);
        }

        public EmployeePage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtNumber, txtNumber);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            layoutControl = layoutItems;
            cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = leEmpGroup.api = lePayrollCategory.api = leApprover.api = leApprover2.api = leWarehouse.api= crudapi;
            Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as EmployeeClient;
                editrow.SetMaster(api.CompanyEntity);
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            editrow.PropertyChanged += Editrow_PropertyChanged;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ZipCode")
            {
                var city = await UtilDisplay.GetCityAndAddress(editrow.ZipCode, api.CompanyEntity._CountryId);
                if (city != null)
                {
                    editrow.City = city[0];
                    var add1 = city[1];
                    if (!string.IsNullOrEmpty(add1))
                        editrow.Address1 = add1;
                    var zip = city[2];
                    if (!string.IsNullOrEmpty(zip))
                        editrow.ZipCode = zip;
                }
            }
        }

        protected override void OnLayoutCtrlLoaded()
        {
            var comp = api.CompanyEntity;
            if (!comp.TimeManagement)
                grpTM.Visibility = Visibility.Collapsed;
            if (!comp.Warehouse)
                liWarehouse.Visibility = Visibility.Collapsed;
        }
    
#if !SILVERLIGHT
        private void Email_ButtonClicked(object sender)
        {
            OpenEmail(txtEmail.Text);
        }

        private void PrivateEmail_ButtonClicked(object sender)
        {
            OpenEmail(txtPrivateEmail.Text);
        }

        void OpenEmail(string email)
        {
            var mail = string.Concat("mailto:", email);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = mail;
            proc.Start();
        }

        private void LiZipCode_OnButtonClicked(object sender)
        {
            var location = editrow._Address1 + "+" + editrow._Address2 + "+" + editrow._ZipCode + "+" + editrow._City;
            Utility.OpenGoogleMap(location);
        }
#endif
    }
}