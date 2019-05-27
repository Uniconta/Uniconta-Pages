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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class StandardVariantCombiGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvStandardVariantCombiClient); } }
        protected override IList ToList(UnicontaBaseEntity[] Arr)
        {
            var lstInvStandardVariantCombiClient = ((InvStandardVariantCombiClient[])Arr).ToList();

            foreach (InvStandardVariantCombiClient objCombiClient in lstInvStandardVariantCombiClient)
            {
                objCombiClient.IsEditable = false;
                objCombiClient.NotifyPropertyChanged("IsEditable");
            }

            return lstInvStandardVariantCombiClient;
        }
        public override bool Readonly { get { return false; } }
        public override IComparer GridSorting
        {
            get
            {
                return new InvStandardVariantCombiSort();
            }
        }
    }

    public partial class StandardVariantCombiPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.StandardVariantCombiPage; } }
        List<UnicontaBaseEntity> masterList;
        public StandardVariantCombiPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();

            if (master != null)
                masterList = new List<UnicontaBaseEntity>() { master };
            InitControls();
        }

        private void InitControls()
        {
            localMenu.dataGrid = dgStandardVariantCombi;
            SetRibbonControl(localMenu, dgStandardVariantCombi);
            dgStandardVariantCombi.masterRecords = masterList;
            dgStandardVariantCombi.api = api;
            dgStandardVariantCombi.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            var table = dgStandardVariantCombi.tableView;
            table.ShowingEditor += Table_ShowingEditor;
            UpdateColumn();
        }

        void UpdateColumn()
        {
            var invStdVarient = masterList.FirstOrDefault() as InvStandardVariantClient;
            var comp = api?.CompanyEntity;
            if (invStdVarient != null)
            {
                if (invStdVarient.Nvariants == 0)
                    HideVarientColumn(comp.NumberOfVariants);
                else
                    HideVarientColumn(invStdVarient.Nvariants);

                if (colVariant1.Visible)
                {
                    if (comp != null && !string.IsNullOrEmpty(comp._Variant1))
                        colVariant1.Header = comp._Variant1;
                }
                if (colVariant2.Visible)
                {
                    if (comp != null && !string.IsNullOrEmpty(comp._Variant2))
                        colVariant2.Header = comp._Variant2;
                }
                if (colVariant3.Visible)
                {
                    if (comp != null && !string.IsNullOrEmpty(comp._Variant3))
                        colVariant3.Header = comp._Variant3;
                }
                if (colVariant4.Visible)
                {
                    if (comp != null && !string.IsNullOrEmpty(comp._Variant4))
                        colVariant4.Header = comp._Variant4;
                }
                if (colVariant5.Visible)
                {
                    if (comp != null && !string.IsNullOrEmpty(comp._Variant5))
                        colVariant5.Header = comp._Variant5;
                }

            }
        }

        void HideVarientColumn(int NoOfVariant)
        {
            if (NoOfVariant == 1)
            {
                colVariant2.Visible = false;
                colVariant3.Visible = false;
                colVariant4.Visible = false;
                colVariant5.Visible = false;
                colVariant2Name.Visible = false;
                colVariant3Name.Visible = false;
                colVariant4Name.Visible = false;
                colVariant5Name.Visible = false;
            }
            else if (NoOfVariant == 2)
            {
                colVariant3.Visible = false;
                colVariant4.Visible = false;
                colVariant5.Visible = false;
                colVariant3Name.Visible = false;
                colVariant4Name.Visible = false;
                colVariant5Name.Visible = false;
            }
            else if (NoOfVariant == 3)
            {
                colVariant4.Visible = false;
                colVariant5.Visible = false;
                colVariant4Name.Visible = false;
                colVariant5Name.Visible = false;
            }
            else if (NoOfVariant == 4)
            {
                colVariant5.Visible = false;
                colVariant5Name.Visible = false;
            }
        }

        private void Table_ShowingEditor(object sender, DevExpress.Xpf.Grid.ShowingEditorEventArgs e)
        {
            var row = e.Row as InvStandardVariantCombiClient;
            if (row != null)
                if (row.IsEditable == false)
                    e.Cancel = true;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgStandardVariantCombi.AddRow();
                    break;
                case "CopyRow":
                    dgStandardVariantCombi.CopyRow();
                    break;
                case "DeleteRow":
                    dgStandardVariantCombi.DeleteRow();
                    break;
                case "SaveGrid":
                    if (ValidateDataForDuplicacy())
                    {
                        var lstStandardVariantCombi = (List<InvStandardVariantCombiClient>)dgStandardVariantCombi.ItemsSource;
                        foreach (InvStandardVariantCombiClient objCombiClient in lstStandardVariantCombi)// changing the IsEditable property to false so that after saving data user cannot edit
                        {
                            objCombiClient.IsEditable = false;
                            objCombiClient.NotifyPropertyChanged("IsEditable");
                        }
                        dgStandardVariantCombi.SaveData();
                    }
                    else
                    {
                        var s = Uniconta.ClientTools.Localization.lookup("DuplicateVariantCombi");
                        UnicontaMessageBox.Show(s, Uniconta.ClientTools.Localization.lookup("Warning"));
                    }
                    break;

            }
        }

        protected override async void LoadCacheInBackGround()
        {
            if (api.CompanyEntity.ItemVariants)
                LoadType(new Type[] { typeof(Uniconta.DataModel.InvVariant1), typeof(Uniconta.DataModel.InvVariant2), typeof(Uniconta.DataModel.InvVariant3), typeof(Uniconta.DataModel.InvVariant4), typeof(Uniconta.DataModel.InvVariant5) });
        }

        private bool ValidateDataForDuplicacy()
        {
            bool retVal = true;
            var lstStandardVariantCombi = (List<InvStandardVariantCombiClient>)dgStandardVariantCombi.ItemsSource;
            var duplicateValues = lstStandardVariantCombi.GroupBy(x => new { Var1 = x.Variant1, Var2 = x.Variant2, Var3 = x.Variant3, Var4 = x.Variant4, Var5 = x.Variant5 }).Where(g => g.Count() > 1).Select(y => y.Key)
              .ToList();

            if (duplicateValues.Count() > 0)
            {
                retVal = false;
                int index = 0;
                foreach (var objCombiClient in lstStandardVariantCombi)
                {
                    if (objCombiClient.Variant1 == duplicateValues[0].Var1 &&
                        objCombiClient.Variant2 == duplicateValues[0].Var2 &&
                         objCombiClient.Variant3 == duplicateValues[0].Var3 &&
                          objCombiClient.Variant4 == duplicateValues[0].Var4 &&
                           objCombiClient.Variant5 == duplicateValues[0].Var5 &&
                        objCombiClient.IsEditable == true)
                        break;
                    index++;
                }
                dgStandardVariantCombi.View.FocusedRowHandle = index;
            }
            else
            {
                retVal = true;
            }

            return retVal;
        }
    }
}
