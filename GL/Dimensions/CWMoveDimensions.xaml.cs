using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.Common;
using Uniconta.Common.Utility;
using UnicontaClient.Pages.Maintenance;

namespace Uniconta.ClientTools.Controls
{
    /// <summary>
    /// Interaction logic for CWMoveDimensions.xaml
    /// </summary>
    public partial class CWMoveDimensions : ChildWindow
    {
        CrudAPI _api;
        List<DimensionItemDataModel> _dimensionItemDataModel;
        public ErrorCodes Result;
        public CWMoveDimensions(CrudAPI api)
        {
            InitializeComponent();
            this.DataContext = this;
            Title = Uniconta.ClientTools.Localization.lookup("ReorganizeDim");
            _api = api;
            Loaded += CWMoveDimensions_Loaded;
        }

        async private void CWMoveDimensions_Loaded(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;
            _dimensionItemDataModel = await GetDimensions();
            busyIndicator.IsBusy = false;
            dimensionList.ItemsSource = _dimensionItemDataModel;
        }

        async private Task<List<DimensionItemDataModel>> GetDimensions()
        {
            var comp = _api.CompanyEntity;
            var dimensionCount = comp.NumberOfDimensions;
            var dimensionList = new List<DimensionItemDataModel>(dimensionCount);
            for (int i = 0; i < dimensionCount; i++)
            {
                string dimensionName = null;
                Type dimensionType = null;

                switch (i)
                {
                    case 0:
                        dimensionName = comp._Dim1;
                        dimensionType = typeof(Uniconta.DataModel.GLDimType1);
                        break;
                    case 1:
                        dimensionName = comp._Dim2;
                        dimensionType = typeof(Uniconta.DataModel.GLDimType2);
                        break;
                    case 2:
                        dimensionName = comp._Dim3;
                        dimensionType = typeof(Uniconta.DataModel.GLDimType3);
                        break;
                    case 3:
                        dimensionName = comp._Dim4;
                        dimensionType = typeof(Uniconta.DataModel.GLDimType4);
                        break;
                    case 4:
                        dimensionName = comp._Dim5;
                        dimensionType = typeof(Uniconta.DataModel.GLDimType5);
                        break;
                }

                var cache = comp.GetCache(dimensionType) ?? await _api.LoadCache(dimensionType);
                if (cache != null && cache.Count > 0)
                {
                    var lst = new List<IdKey>(cache.Count);
                    foreach (var dim in cache.GetKeyStrRecords)
                        lst.Add(new KeyNamePair() { _RowId = dim.RowId, _KeyStr = Uniconta.Common.Utility.Util.ConcatParenthesis(dim.KeyStr, dim.KeyName) });

                    dimensionList.Add(new DimensionItemDataModel(dimensionName, lst));
                }
            }

            return dimensionList;
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            var startDialog = new EraseYearWindow("", true);
            startDialog.Closed += async delegate
            {
                if (startDialog.DialogResult == true)
                {
                    busyIndicator.IsBusy = true;

                    var lst = _dimensionItemDataModel;
                    if (_api == null || lst == null || lst.Count == 0)
                    {
                        SetDialogResult(false);
                        return;
                    }

                    int? dim1 = null, dim2 = null, dim3 = null, dim4 = null, dim5 = null;
                    int? toDim1 = null, toDim2 = null, toDim3 = null, toDim4 = null, toDim5 = null;
                    int dimCount = lst.Count;

                    for (int i = 0; i < dimCount; i++)
                    {
                        var dx = lst[i].ActualDimensionValue;
                        var todx = lst[i].SelectedDimensionValue;
                        switch (i)
                        {
                            case 0:
                                dim1 = dx;
                                toDim1 = todx;
                                break;
                            case 1:
                                dim2 = dx;
                                toDim2 = todx;
                                break;
                            case 2:
                                dim3 = dx;
                                toDim3 = todx;
                                break;
                            case 3:
                                dim4 = dx;
                                toDim4 = todx;
                                break;
                            case 4:
                                dim5 = dx;
                                toDim5 = todx;
                                break;
                        }
                    }

                    Result = await (new Uniconta.API.GeneralLedger.PostingAPI(_api)).MoveDimensions(dim1, dim2, dim3, dim4, dim5, toDim1, toDim2, toDim3, toDim4, toDim5);
                    busyIndicator.IsBusy = false;
                    SetDialogResult(true);
                }
            };
            startDialog.Show();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                SetDialogResult(false);
            else if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                    CancelButton_Click(null, null);
                else if (MoveButton.IsFocused)
                    MoveButton_Click(null, null);
            }
        }
    }

    public class DimensionItemDataModel
    {
        public string FromDimension { get; set; }
        public string ToDimension { get; set; }
        public int? ActualDimensionValue { get; set; }
        public int? SelectedDimensionValue { get; set; }
        public List<IdKey> DimensionValues { get; set; }

        public DimensionItemDataModel(string dimension, List<IdKey> listOfDimensionValues)
        {
            FromDimension = string.Format(Uniconta.ClientTools.Localization.lookup("FromOBJ"), dimension);
            ToDimension = string.Format(Uniconta.ClientTools.Localization.lookup("ToOBJ"), dimension);
            DimensionValues = listOfDimensionValues;
        }
    }
}
