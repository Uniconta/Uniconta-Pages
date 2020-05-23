using UnicontaClient.Models;
using System;
using System.Collections;
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
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvVariantGridClient : CorasauDataGridClient
    {
        public int InvVariantType;

        public override Type TableType
        {
            get
            {
                switch (InvVariantType)
                {
                    case 1: return typeof(InvVariant1Client);
                    case 2: return typeof(InvVariant2Client);
                    case 3: return typeof(InvVariant3Client);
                    case 4: return typeof(InvVariant4Client);
                    case 5: return typeof(InvVariant5Client);
                    default: return typeof(InvVariant1Client);
                }
            }
        }
        
        public override bool Readonly
        {
            get
            {
                return false;
            }
        }
        public override string LineNumberProperty
        {
            get
            {
                return "_LineNumber";
            }
        }

        public override IComparer GridSorting
        {
            get
            {
                return new InvVariantSort();
            }
        }
    }
    public partial class InvVariantsPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvVariantsPage; } }
        protected override bool IsLayoutSaveRequired() { return false; }

        public InvVariantsPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public InvVariantsPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            dgInvVariant.api = api;
            dgInvVariant.InvVariantType = 1;
            SetRibbonControl(localMenu, dgInvVariant);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            BindVariants();
            
        }
        private void BindVariants()
        {
            var company = api.CompanyEntity;
            var variants = new List<string>();
            if (!string.IsNullOrEmpty(company._Variant1))
                variants.Add(company._Variant1);
            if (!string.IsNullOrEmpty(company._Variant2))
                variants.Add(company._Variant2);
            if (!string.IsNullOrEmpty(company._Variant3))
                variants.Add(company._Variant3);
            if (!string.IsNullOrEmpty(company._Variant4))
                variants.Add(company._Variant4);
            if (!string.IsNullOrEmpty(company._Variant5))
                variants.Add(company._Variant5);
            if (variants.Count > 0)
            {
                cmbVariants.SelectedIndex = 0;
                cmbVariants.IsEnabled = true;
                cmbVariants.ItemsSource = variants;
            }
            else
            {
                cmbVariants.IsEnabled = false;
                cmbVariants.SelectedIndex = -1;
            }
        }

        async public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (TabControls.InvVaraintSetupPage == screenName)
            {
                await api.Read(api.CompanyEntity);
                BindVariants();
            }
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedVariants = cmbVariants.SelectedItem as string;
            switch (ActionType)
            {
                case "AddRow":
                    if (string.IsNullOrEmpty(selectedVariants))
                        return;
                    dgInvVariant.AddRow();
                    break;

                case "DeleteRow":
                    if (string.IsNullOrEmpty(selectedVariants))
                        return;
                    dgInvVariant.DeleteRow();
                    break;

                case "SaveGrid":
                    if (string.IsNullOrEmpty(selectedVariants))
                        return;
                    dgInvVariant.SaveData();
                    break;
                case "SetupVariants":
                    var compnClient = new CompanyClient();
                    StreamingManager.Copy(api.CompanyEntity, compnClient);
                    AddDockItem(TabControls.InvVaraintSetupPage, compnClient, string.Format(Uniconta.ClientTools.Localization.lookup("SetupOBJ"), Uniconta.ClientTools.Localization.lookup("Variants")) , "Add_16x16.png");
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }


        private void cmbVariants_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbVariants.SelectedItem != null)
                Save(cmbVariants.SelectedIndex + 1);
        }

        private async void Save(int newVariant)
        {
            var err = await dgInvVariant.SaveData();
            if (newVariant != 0)
                dgInvVariant.InvVariantType = newVariant;
            if (err == ErrorCodes.Succes)
                await dgInvVariant.Filter(null);
        }
    }
}
