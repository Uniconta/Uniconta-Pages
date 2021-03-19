using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
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
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Pages;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DimensionGrid : CorasauDataGridClient
    {
        public int DimType;
        public override Type TableType
        {
            get
            {
                switch (DimType)
                {
                    case 1: return typeof(GLDimType1Client);
                    case 2: return typeof(GLDimType2Client);
                    case 3: return typeof(GLDimType3Client);
                    case 4: return typeof(GLDimType4Client);
                    case 5: return typeof(GLDimType5Client);
                    default: return null;
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
    }
    public partial class GL_Dimensions : GridBasePage
    {
        int NumberOfDimensions;

        public GL_Dimensions(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgDimension);
            localMenu.dataGrid = dgDimension;
            dgDimension.api = api;
            dgDimension.DimType = 1;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            BindDimensions();
        }

        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }

        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GL_SetupDimension)
            {
                var res = await api.Read(api.CompanyEntity);
                BindDimensions();
            }
        }

        private void BindDimensions()
        {
            var c = api.CompanyEntity;
            NumberOfDimensions = c.NumberOfDimensions;
            List<string> dimensions = new List<string>();
            dimensions.Add(c._Dim1);
            dimensions.Add(c._Dim2);
            dimensions.Add(c._Dim3);
            dimensions.Add(c._Dim4);
            dimensions.Add(c._Dim5);
            dimensions.RemoveRange(NumberOfDimensions, 5 - NumberOfDimensions);
            cmbDimensions.ItemsSource = dimensions;
            if (dimensions.Count > 0)
                cmbDimensions.SelectedIndex = 0;
        }

        void cmbDimensions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDimensions.SelectedItem != null)
                Save(cmbDimensions.SelectedIndex + 1);
        }

        public override string NameOfControl
        {
            get { return TabControls.GL_Dimensions.ToString(); }
        }

        void localMenu_OnItemClicked(string type)
        {
            switch (type)
            {
                case "AddRow":
                    if (NumberOfDimensions > 0)
                        dgDimension.AddRow();
                    else
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("OneDimensionNeeded"), Uniconta.ClientTools.Localization.lookup("Error"));
                    break;
                case "DeleteRow":
                    dgDimension.DeleteRow();
                    break;
                case "SaveGrid":
                    Save(0);
                    break;
                case "SetupDimension":
                    var compClient = new CompanyClient();
                    StreamingManager.Copy(api.CompanyEntity, compClient);
                    AddDockItem(TabControls.GL_SetupDimension, compClient, Uniconta.ClientTools.Localization.lookup("SetupDimension"), "Add_16x16.png");
                    break;
                case "JoinDimensions":
                    var cwJoinTwoDimension = new CWJoinTwoDimensions(api, cmbDimensions.SelectedItem as string);
                    cwJoinTwoDimension.Closed += async delegate
                     {
                         if (cwJoinTwoDimension.DialogResult == true)
                         {
                             var ret = await cwJoinTwoDimension.JoinResult;
                             UtilDisplay.ShowErrorCode(ret);
                             if (ret == ErrorCodes.Succes)
                                 dgDimension.Refresh();
                         }
                     };
                    cwJoinTwoDimension.Show();
                    break;
#if !SILVERLIGHT
                case "ReorganizeDim":
                    ReOrganizeDimensions();
                    break;
#endif
            }
        }

#if !SILVERLIGHT

        private void ReOrganizeDimensions()
        {
            var cwMoveDimensions = new CWMoveDimensions(api);
            cwMoveDimensions.Closed += delegate
            {
                if (cwMoveDimensions.DialogResult == true)
                {
                    if (cwMoveDimensions.Result == ErrorCodes.Succes)
                        dgDimension.Refresh();
                    else
                        UtilDisplay.ShowErrorCode(cwMoveDimensions.Result);
                }
            };
            cwMoveDimensions.Show();
        }

#endif

        private async void Save(int NewDim)
        {
            var err = await dgDimension.SaveData();
            if (NewDim != 0)
                dgDimension.DimType = NewDim;
            if (err == ErrorCodes.Succes)
                dgDimension.Filter(null);
        }
    }
}
