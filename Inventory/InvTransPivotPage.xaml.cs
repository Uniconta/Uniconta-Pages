using UnicontaClient.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InvTransPivotPage : PivotBasePage
    {
        CWServerFilter filterDialog = null;
        bool filterCleared;
        TableField[] UserFields { get; set; }
        public override string NameOfControl { get { return TabControls.InvTransPivotPage; } }
        bool pivotIsLoaded = false;
        public InvTransPivotPage(UnicontaBaseEntity master) : base(master)
        {
            init(master);
        }   

        public InvTransPivotPage(BaseAPI API) : base(API, string.Empty)
        {
            init(null);
        }

        void init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetPageControl(pivotDgInvTrans, chartControl);
            layoutControl = layoutItems;
            pivotDgInvTrans.BusyIndicator = busyIndicator;
            ribbonControl = localMenu;
            pivotDgInvTrans.BeginUpdate();
            pivotDgInvTrans.AllowCrossGroupVariation = false;
            pivotDgInvTrans.api = api;
            pivotDgInvTrans.TableType = typeof(InvTransClient);
            pivotDgInvTrans.UpdateMaster(master);
            BindGrid();
            pivotDgInvTrans.EndUpdate();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            if (!api.CompanyEntity.Warehouse)
                fieldWarehouse.Visible = false;
            else if (!api.CompanyEntity.Location)
                fieldLocation.Visible = false;
            tbGrdTtlRow.Text = Uniconta.ClientTools.Localization.lookup("ShowRowGrandTotals");
            tbGrdTtlCol.Text = Uniconta.ClientTools.Localization.lookup("ShowColumnGrandTotals");
            tbTtlOnFlds.Text = Uniconta.ClientTools.Localization.lookup("ShowTotals");
            chkGrdTtlCol.IsChecked = chkGrdTtlRow.IsChecked = chkTtlOnFlds.IsChecked = true;
            if (!string.IsNullOrEmpty(api.CompanyEntity._BrandGroup))
                fieldBrandGroup.Caption = api.CompanyEntity._BrandGroup;
            if (!string.IsNullOrEmpty(api.CompanyEntity._CategoryGroup))
                fieldCategoryGroup.Caption = api.CompanyEntity._CategoryGroup;
        }

        protected override void OnLayoutLoaded()
        {
            if (chartControl.Diagram != null)
            {
                chartControl.Visibility = Visibility.Visible;
                labelVisibility = chartControl.Diagram.SeriesTemplate.LabelsVisibility;
                seriesIndex = GetSeriesId();
            }
            fieldBrandGroup.Visible = fieldCategoryGroup.Visible = fieldStatisticsGroup.Visible = api.CompanyEntity._InvGroups;
        }

        int seriesIndex = 0;
        bool labelVisibility = true;
        bool chartEnable = true;
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "RefreshGrid":
                    BindGrid(true);
                    pivotDgInvTrans.RefreshData();
                    break;
                case "LocalFilter":
                    if (filterDialog == null)
                    {
                        if (filterCleared)
                            filterDialog = new CWServerFilter(api, typeof(InvTransClient), null, null, UserFields);
                        else
                            filterDialog = new CWServerFilter(api, typeof(InvTransClient), null, null, UserFields);
                        filterDialog.GridSource = pivotDgInvTrans.DataSource as IList<UnicontaBaseEntity>;
                        filterDialog.Closing += filterDialog_Closing;
                        filterDialog.Show();
                    }
                    else
                    {
                        filterDialog.GridSource = pivotDgInvTrans.DataSource as IList<UnicontaBaseEntity>;
                        filterDialog.Show(true);
                    }
                    break;
                case "ClearLocalFilter":
                    filterDialog = null;
                    filterValues = null;
                    filterCleared = true;
                    BindGrid();
                    pivotDgInvTrans.RefreshData();
                    break;
                case "ChartSettings":
                    CWChartSettings cwChartSettingDialog = new CWChartSettings(pivotDgInvTrans.ChartSelectionOnly, pivotDgInvTrans.ChartProvideColumnGrandTotals,
                        pivotDgInvTrans.ChartProvideRowGrandTotals, labelVisibility, seriesIndex, pivotDgInvTrans.ChartProvideDataByColumns, chartEnable);
                    cwChartSettingDialog.Closed += delegate
                    {
                        if (cwChartSettingDialog.DialogResult == true)
                        {
                            if (cwChartSettingDialog.IsChartVisible)
                            {
                                chartControl.Diagram = cwChartSettingDialog.ChartDaigram;
                                pivotDgInvTrans.ChartProvideEmptyCells = IsPivotTableProvideEmptyCells();
                                chartControl.Diagram.SeriesTemplate.LabelsVisibility = cwChartSettingDialog.labelVisibility;
                                chartControl.CrosshairEnabled = cwChartSettingDialog.crossHairEnabled;
                                pivotDgInvTrans.ChartProvideDataByColumns = cwChartSettingDialog.chartProvideDataByColumns;
                                pivotDgInvTrans.ChartSelectionOnly = cwChartSettingDialog.ChartSelectionOnly;
                                pivotDgInvTrans.ChartProvideColumnGrandTotals = cwChartSettingDialog.ChartProvideColumnGrandTotals;
                                pivotDgInvTrans.ChartProvideRowGrandTotals = cwChartSettingDialog.ChartProvideRowGrandTotals;
                                seriesIndex = cwChartSettingDialog.SeriesIndex;
                                chartControl.Visibility = Visibility.Visible;
                                if (rowgridSplitter.Height.Value == 0 && rowChartControl.Height.Value == 0)
                                {
                                    rowgridSplitter.Height = new GridLength(5);
                                    var converter = new GridLengthConverter();
                                    rowChartControl.Height = (GridLength)converter.ConvertFrom("Auto");
                                }
                            }
                            else
                            {
                                chartControl.Visibility = Visibility.Collapsed;
                                rowgridSplitter.Height = new GridLength(0);
                                rowChartControl.Height = new GridLength(0);
                            }
                            chartEnable = cwChartSettingDialog.IsChartVisible;
                            labelVisibility = cwChartSettingDialog.labelVisibility;
                        }
                    };
                    cwChartSettingDialog.Show();
                    break;
                case "ImportPivotTableLayout":
                    controlRibbon_BaseActions(ActionType);
                    if (chartControl.Diagram != null)
                    {
                        chartControl.Visibility = Visibility.Visible;
                        labelVisibility = chartControl.Diagram.SeriesTemplate.LabelsVisibility;
                        seriesIndex = GetSeriesId();
                    }
                    break;
                default:
                    controlRibbon_BaseActions(ActionType);
                    break;
            }     
        }

        private Task Filter(bool refreshData)
        {
            return pivotDgInvTrans.Filter(refreshData ? null : filterValues);
        }
        private void BindGrid(bool refreshData = false)
        {
            var t = Filter(refreshData);
        }

        public IEnumerable<PropValuePair> filterValues;
        public FilterSorter PropSort;

        void filterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (filterDialog.DialogResult == true)
            {
                filterValues = filterDialog.PropValuePair;
                PropSort = filterDialog.PropSort;
                BindGrid();
                pivotDgInvTrans.RefreshData();
            }
            e.Cancel = true;
            filterDialog.Hide();
        }

        private void PivotDgInvTrans_Loaded(object sender, RoutedEventArgs e)
        {
            if (!pivotIsLoaded)
            {
                pivotDgInvTrans.BestFit();
                pivotIsLoaded = true;
            }
        }

        private void chkGrdTtlRow_Checked(object sender, RoutedEventArgs e)
        {
            pivotDgInvTrans.ShowRowGrandTotalHeader =
            pivotDgInvTrans.ShowRowGrandTotals = chkGrdTtlRow.IsChecked.GetValueOrDefault();
        }

        private void chkGrdTtlCol_Checked(object sender, RoutedEventArgs e)
        {
            var value = chkGrdTtlCol.IsChecked.GetValueOrDefault();
            pivotDgInvTrans.ShowColumnGrandTotalHeader = value;
            pivotDgInvTrans.ShowColumnGrandTotals = value;
            pivotDgInvTrans.ShowColumnTotals = value;
        }

        private void chkTtlOnFlds_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkTtlOnFlds.IsChecked;
            pivotDgInvTrans.ShowRowTotals = value;
        }
    }
}
