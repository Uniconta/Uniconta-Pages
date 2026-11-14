using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.Helpers;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.SIEModels;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Maps the SIE dimensions used by the transactions of a file to Uniconta dimensions.
    /// </summary>
    public partial class CWSIEMapDimensions : Uniconta.ClientTools.ChildWindow
    {
        private readonly List<KeyValuePair<string, ComboBoxEditor>> _editors = new List<KeyValuePair<string, ComboBoxEditor>>();

        /// <summary>
        /// SIE dimension number -> Uniconta dimension (1..5). Dimensions that are not imported are left out.
        /// </summary>
        public Dictionary<string, int> DimensionMap { get; private set; }

        public CWSIEMapDimensions(Company company, IList<SIEDimension> sieDimensions)
        {
            InitializeComponent();

            Title = Uniconta.ClientTools.Localization.lookup("SIEMapDimensions");

            var unicontaNames = new List<string>();
            for (int i = 1; i <= company.NumberOfDimensions; i++)
                unicontaNames.Add(GetDimensionName(company, i));

            //Item 0 is "not imported", item N is Uniconta dimension N
            var items = new List<string> { "(" + Uniconta.ClientTools.Localization.lookup("NotImported") + ")" };
            items.AddRange(unicontaNames.Select((name, i) => (i + 1) + " " + name));

            var suggested = SIEHelpers.SuggestDimensionMap(sieDimensions.Select(d => new KeyValuePair<string, string>(d.Number, d.Name)).ToList(), unicontaNames);

            foreach (var dim in sieDimensions)
            {
                var row = gridMappings.RowDefinitions.Count;
                gridMappings.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                var label = new TextBlock() { Text = dim.Number + " " + dim.Name, Margin = new Thickness(3), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(label, row);
                Grid.SetColumn(label, 0);
                gridMappings.Children.Add(label);

                var editor = new ComboBoxEditor() { ItemsSource = items, IsEditable = false, Margin = new Thickness(0, 3, 3, 3) };
                editor.SelectedIndex = suggested[dim.Number];
                Grid.SetRow(editor, row);
                Grid.SetColumn(editor, 2);
                gridMappings.Children.Add(editor);

                _editors.Add(new KeyValuePair<string, ComboBoxEditor>(dim.Number, editor));
            }
        }

        private static string GetDimensionName(Company company, int dimension)
        {
            string name = null;
            switch (dimension)
            {
                case 1: name = company._Dim1; break;
                case 2: name = company._Dim2; break;
                case 3: name = company._Dim3; break;
                case 4: name = company._Dim4; break;
                case 5: name = company._Dim5; break;
            }

            return String.IsNullOrWhiteSpace(name) ? Uniconta.ClientTools.Localization.lookup("Dimension") + " " + dimension : name;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var map = new Dictionary<string, int>();
            foreach (var entry in _editors)
            {
                var dimension = Math.Max(entry.Value.SelectedIndex, 0);
                if (dimension == 0)
                    continue;

                //Two SIE dimensions in one Uniconta dimension would mix their objects
                if (map.ContainsValue(dimension))
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SIEDimensionMappedTwice"), Uniconta.ClientTools.Localization.lookup("Error"));
                    return;
                }

                map[entry.Key] = dimension;
            }

            DimensionMap = map;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
