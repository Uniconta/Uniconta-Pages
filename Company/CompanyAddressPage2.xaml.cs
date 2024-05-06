using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CompanyAddressPage2 : FormBasePage
    {
        CompanyAddressClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override Type TableType { get { return typeof(CompanyAddressClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanyAddressClient)value; } }

        public CompanyAddressPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api, null);
        }

        public CompanyAddressPage2(UnicontaBaseEntity sourcedata, bool isEdit, UnicontaBaseEntity master = null) : base(sourcedata, isEdit)
        {
            InitializeComponent();
            InitPage(api, master);
        }

        public CompanyAddressPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi, null);
        }

        void InitPage(CrudAPI crudapi, UnicontaBaseEntity master)
        {
            ribbonControl = frmRibbon;
            var Comp = api.CompanyEntity;
            layoutControl = layoutItems;
            if (LoadedRow == null)
                frmRibbon.DisableButtons("Delete");
            cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
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
                var city = await UtilDisplay.GetCityAndAddress(editrow.ZipCode, editrow.Country);
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

        private bool onlyRunOnce;

        private void LiZipCode_OnButtonClicked(object sender)
        {
            var location = editrow._Address1 + "+" + editrow._Address2 + "+" + editrow._ZipCode + "+" + editrow._City + "+" + editrow.Country;
            Utility.OpenGoogleMap(location);
        }

        private void Email_ButtonClicked(object sender)
        {
            var mail = string.Concat("mailto:", txtEmail.Text);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = mail;
            proc.Start();
        }
    }
}
