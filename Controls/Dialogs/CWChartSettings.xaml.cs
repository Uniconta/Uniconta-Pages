using DevExpress.Xpf.Charts;
using System;
using System.Collections.Generic;
using System.Windows;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWChartSettings : ChildWindow
    {
        public  Diagram ChartDaigram { get; set; }
        public  bool labelVisibility { get; set; }
        public  bool crossHairEnabled { get; set; }
        public  bool chartProvideDataByColumns { get; set; }
        public  bool ChartSelectionOnly { get; set; }
        public  bool ChartProvideColumnGrandTotals { get; set; }
        public  bool ChartProvideRowGrandTotals { get; set; }

        public  int SeriesIndex { get; set; }
        public  bool IsChartVisible { get; set;}

        public CWChartSettings(bool isChartSelectionOnly, bool isChartProvideColumnGrandTotals,
            bool isChartProvideRowGrandTotals, bool labelVisibility, int seriesIndex, bool isChartProvideDataByColumns, bool isChartVisible)
        {
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("ChartSettings");
            var chartData = new string[] { Uniconta.ClientTools.Localization.lookup("GenerateSeriesFromColumns"), Uniconta.ClientTools.Localization.lookup("GenerateSeriesFromRows") };
            crChartDataVertical.ItemsSource = chartData;
            BindComboBox();
            cbChartType.SelectedIndex = seriesIndex;
            ceShowPointsLabels.IsChecked = labelVisibility;
            if (isChartProvideDataByColumns)
                crChartDataVertical.SelectedIndex = 1;
            else
                crChartDataVertical.SelectedIndex = 0;
            ceChartSelectionOnly.IsChecked = isChartSelectionOnly;
            ceChartShowColumnGrandTotals.IsChecked = isChartProvideColumnGrandTotals;
            ceChartShowRowGrandTotals.IsChecked = isChartProvideRowGrandTotals;
            ceEnableChart.IsChecked = isChartVisible;
            this.Loaded += CW_Loaded;
        }

        void BindComboBox()
        {
            List<UnicontaComboBoxEditor> itemsList = new List<UnicontaComboBoxEditor>();
            UnicontaComboBoxEditor item, selectedItem = null;
            foreach (Type seriesType in UnicontaChartFactory.SeriesTypes.Keys)
            {
                UnicontaChartFactory.SeriesTypeDescriptor sd = UnicontaChartFactory.SeriesTypes[seriesType];
               
                    item = new UnicontaComboBoxEditor();
                    item.Content = sd.DisplayText;
                    item.Tag = seriesType;
                    itemsList.Add(item);
            }
            cbChartType.Items.AddRange(itemsList.ToArray());
            cbChartType.SelectedItem = selectedItem;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cbChartType.Focus(); }));
        }
        
        private void cbChartType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cbChartType.SelectedIndex < 0)
                return;
            ChartDaigram = UnicontaChartFactory.GenerateDiagram((Type)((UnicontaComboBoxEditor)cbChartType.SelectedItem).Tag, ceShowPointsLabels.IsChecked);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ChartSelectionOnly = (bool)ceChartSelectionOnly.IsChecked;
            ChartProvideColumnGrandTotals = (bool)ceChartShowColumnGrandTotals.IsChecked;
            ChartProvideRowGrandTotals = (bool)ceChartShowRowGrandTotals.IsChecked;
            SeriesIndex = cbChartType.SelectedIndex;
            IsChartVisible = (bool)ceEnableChart.IsChecked;
            SetDialogResult(true);
        }

        private void ceShowPointsLabels_Checked(object sender, RoutedEventArgs e)
        {
            labelVisibility = object.Equals(ceShowPointsLabels.IsChecked, true);
            crossHairEnabled = object.Equals(ceShowPointsLabels.IsChecked, false);
        }
        
        private void crChartDataVertical_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            chartProvideDataByColumns = crChartDataVertical.SelectedIndex == 1;
        }
    }  
}
