using UnicontaClient.Models;
using DevExpress.Xpf.Grid;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class StandardVariantGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvStandardVariantClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class StandardVariantPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.StandardVariantPage; } }
        int numberOfVarients;
        public StandardVariantPage(BaseAPI API):base(API, string.Empty)
        {
            Init();
        }
        public StandardVariantPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgStandardVariant;
            SetRibbonControl(localMenu, dgStandardVariant);
            dgStandardVariant.api = api;
            dgStandardVariant.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            numberOfVarients = api.CompanyEntity.NumberOfVariants;
            dgStandardVariant.View.DataControl.CurrentItemChanged += StandardVariant_CurrentItemChanged;
            HideVariantColumn();
        }

        Company comp;
        void HideVariantColumn()
        {
            comp = api?.CompanyEntity;
            if (comp == null)
                return;
            if (comp.NumberOfVariants == 1)
            {
                Variant2Name.Visible = false;
                Variant3Name.Visible = false;
                Variant4Name.Visible = false;
                Variant5Name.Visible = false;
            }
            else if (comp.NumberOfVariants == 2)
            {
                Variant3Name.Visible = false;
                Variant4Name.Visible = false;
                Variant5Name.Visible = false;
            }
            else if (comp.NumberOfVariants == 3)
            {
                Variant4Name.Visible = false;
                Variant5Name.Visible = false;
            }
            else if (comp.NumberOfVariants == 4)
            {
                Variant5Name.Visible = false;
            }
            else if (comp.NumberOfVariants == 0)
            {
                Variant1Name.Visible = false;
                Variant2Name.Visible = false;
                Variant3Name.Visible = false;
                Variant4Name.Visible = false;
                Variant5Name.Visible = false;
            }
        }

        private void StandardVariant_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            var oldSelectedItem = e.OldItem as InvStandardVariantClient;
            if (oldSelectedItem != null)
                oldSelectedItem.PropertyChanged -= StandardVariant_CurrentItemChanged_PropertyChanged;

            var selectedItem = e.NewItem as InvStandardVariantClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += StandardVariant_CurrentItemChanged_PropertyChanged;
        }

        private void StandardVariant_CurrentItemChanged_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as InvStandardVariantClient;
            switch (e.PropertyName)
            {
                case "Nvariants":
                    if (rec.Nvariants > numberOfVarients)
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ValueMayNoBeGreater"), Uniconta.ClientTools.Localization.lookup("Count"), numberOfVarients), Uniconta.ClientTools.Localization.lookup("Error"));
                        rec.Nvariants = numberOfVarients;
                        rec.NotifyPropertyChanged("Nvariants");
                    }
                    else if (rec.Nvariants <= numberOfVarients)
                        SetVariantValue(rec);
                    break;
                case "Variant1Name":
                    SetVariantValue(rec);
                    break;
                case "Variant2Name":
                    SetVariantValue(rec);
                    break;
                case "Variant3Name":
                    SetVariantValue(rec);
                    break;
                case "Variant4Name":
                    SetVariantValue(rec);
                    break;
                case "Variant5Name":
                    SetVariantValue(rec);
                    break;
            }
        }

        void SetVariantValue(InvStandardVariantClient rec)
        {
            if (rec.Nvariants == 1 || comp.NumberOfVariants ==1)
            {
                if (!string.IsNullOrEmpty(rec.Variant2Name))
                    rec.Variant2Name = string.Empty;
                if (!string.IsNullOrEmpty(rec.Variant3Name))
                    rec.Variant3Name = string.Empty;
                if (!string.IsNullOrEmpty(rec.Variant4Name))
                    rec.Variant4Name = string.Empty;
                if (!string.IsNullOrEmpty(rec.Variant5Name))
                    rec.Variant5Name = string.Empty;
            }
            if (rec.Nvariants == 2 || comp.NumberOfVariants == 2)
            {
                if (!string.IsNullOrEmpty(rec.Variant3Name))
                    rec.Variant3Name = string.Empty;
                if (!string.IsNullOrEmpty(rec.Variant4Name))
                    rec.Variant4Name = string.Empty;
                if (!string.IsNullOrEmpty(rec.Variant5Name))
                    rec.Variant5Name = string.Empty;
            }
            if (rec.Nvariants == 3 || comp.NumberOfVariants == 3)
            {
                if (!string.IsNullOrEmpty(rec.Variant4Name))
                    rec.Variant4Name = string.Empty;
                if (!string.IsNullOrEmpty(rec.Variant5Name))
                    rec.Variant5Name = string.Empty;
            }
            if (rec.Nvariants == 4 || comp.NumberOfVariants == 4)
            {
                if(!string.IsNullOrEmpty(rec.Variant5Name))
                  rec.Variant5Name = string.Empty;
            }
            if (rec.Nvariants == 0)
            {
                if (!string.IsNullOrEmpty(rec.Variant1Name))
                    rec.Variant1Name = string.Empty;
                if (!string.IsNullOrEmpty(rec.Variant2Name))
                    rec.Variant2Name = string.Empty;
                if (!string.IsNullOrEmpty(rec.Variant3Name))
                    rec.Variant3Name = string.Empty;
                if (!string.IsNullOrEmpty(rec.Variant4Name))
                    rec.Variant4Name = string.Empty;
                if (!string.IsNullOrEmpty(rec.Variant5Name))
                    rec.Variant5Name = string.Empty;
            }
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgStandardVariant.SelectedItem as InvStandardVariantClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgStandardVariant.AddRow();
                    break;
                case "SaveGrid":
                    saveGrid(selectedItem);
                    break;
                case "DeleteRow":
                    if (selectedItem == null) return;
                    if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        dgStandardVariant.DeleteRow(false);
                    break;
                case "Lines":
                    if (selectedItem != null)
                        SaveAndOpenLines(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        async void SaveAndOpenLines(InvStandardVariantClient selectedItem)
        {
            if (dgStandardVariant.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && selectedItem.RowId == 0)
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.StandardVariantCombiPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StandardVariant"), selectedItem._Name));
        }
    }
}
