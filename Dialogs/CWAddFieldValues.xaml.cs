using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.API.Plugin;

namespace UnicontaClient.Pages
{
    /// <summary>
    /// Interaction logic for CWAddFieldValues.xaml
    /// </summary>
    public partial class CWAddFieldValues : ChildWindow
    {
        public PropertyInfo Property { get; set; }
        public string Value { get; set; }
        public Visibility ScriptVisibility { get; set; } = Visibility.Visible;
        PropertyInfo[] _propInfos;
        public UnicontaCompileResult Expr;
        CrudAPI api;
        static public bool isCSharp;

        public CWAddFieldValues(CrudAPI api, PropertyInfo[] propertyInfos)
        {
            this.api = api;
            InitializeComponent();
            this.DataContext = this;
            _propInfos = propertyInfos;
            cmbPropertyTypes.ItemsSource = PrepareSource(propertyInfos);
            Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Values"));
            this.Loaded += CW_Loaded;
            lblScript.Text = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Properties"));
            cmbExternType.ItemsSource = new List<string> { "rec", "Company" };
            cmbExternType.SelectedIndex = 0;
#if !SILVERLIGHT
            chkIsCsharp.IsChecked = isCSharp;
#endif
        }

        public CWAddFieldValues(CrudAPI api, PropertyInfo[] propertyInfos, bool showScriptOptions) : this(api, propertyInfos)
        {
            if (!showScriptOptions)
                ScriptVisibility = Visibility.Collapsed;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cmbPropertyTypes.Focus(); }));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Property = null;
            Value = null;
            DialogResult = false;
        }

        private Dictionary<string, string> PrepareSource(PropertyInfo[] propertyInfo)
        {
            var dict = new Dictionary<string, string>();
            foreach (var pi in propertyInfo)
            {
                var displayName = UtilFunctions.GetDisplayNameFromPropertyInfo(pi);
                var propertName = pi.Name;
                dict.Add(propertName, displayName);
            }
            return dict;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPropertyTypes.SelectedItem == null)
                return;
            var selectedItem = (KeyValuePair<string, string>)cmbPropertyTypes.SelectedItem;

            if (!string.IsNullOrEmpty(selectedItem.Key) && !string.IsNullOrEmpty(selectedItem.Value))
            {
                Property = _propInfos.Where(p => p.Name == selectedItem.Key).Single();
                Value = CheckValue();
                if (Value == null)
                    return;
                if (Property != null)
                    DialogResult = true;
                else
                    Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FieldCannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
            }
        }

        string CheckValue()
        {
            if (rbValue.IsChecked == true)
                return txtPropertyValue.Text;
            else
            {
#if !SILVERLIGHT
                isCSharp = (bool)this.chkIsCsharp.IsChecked;
#endif
                var res = CWAddScript.Compile(api, _propInfos[0].ReflectedType, txtScript.Text, null, isCSharp, true);
                if (res == null)
                    return null;
                return res.expr?.ToString() ?? Uniconta.ClientTools.Localization.lookup("ok");
            }
        }

        private void rbValue_Click(object sender, RoutedEventArgs e)
        {
            btnScriptHelper.IsEnabled = txtScript.IsEnabled = false;
            txtPropertyValue.IsEnabled = true;
            CompileButton.Visibility = Visibility.Collapsed;
        }

        private void rbScript_Click(object sender, RoutedEventArgs e)
        {
            btnScriptHelper.IsEnabled = true;
            txtScript.IsEnabled = true;
            txtPropertyValue.IsEnabled = false;
            CompileButton.Visibility = Visibility.Visible;
        }

        private void Compile_Click(object sender, RoutedEventArgs e)
        {
#if !SILVERLIGHT
            isCSharp = (bool)this.chkIsCsharp.IsChecked;
#endif
            Expr = CWAddScript.Compile(api, _propInfos[0].ReflectedType, txtScript.Text, null, isCSharp, true);
            if (Expr != null)
                Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(Expr.expr?.ToString() ?? Uniconta.ClientTools.Localization.lookup("ok"), Uniconta.ClientTools.Localization.lookup("Message"));
        }

        private void cmbExternType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            Type type;
            if (cmbExternType.SelectedIndex != 0 || _propInfos.Length == 0)
                type = typeof(CompanyClient);
            else
                type = _propInfos[0].ReflectedType;
            List<string> propertyNames = new List<string>();
            foreach (var p in type.GetProperties())
            {
                if (p.PropertyType == typeof(string) || typeof(IEnumerable).IsAssignableFrom(p.PropertyType) == false)
                    propertyNames.Add(p.Name + " (" + p.PropertyType + ")");
            }
            propertyNames = propertyNames.OrderBy(p => p).ToList();
            cmbProperties.ItemsSource = propertyNames;
            cmbProperties.SelectedIndex = 0;
        }

        private void btnScriptHelper_Click(object sender, RoutedEventArgs e)
        {
            var olSelectionStart = txtScript.SelectionStart;
            var selectedText = Convert.ToString(cmbProperties.SelectedItem);
            var propName = selectedText.Substring(0, selectedText.IndexOf(" ("));
            propName = string.Format("{0}.{1}", cmbExternType.SelectedItem, propName);
            if (string.IsNullOrEmpty(txtScript.Text))
                txtScript.Text = propName;
            else
                txtScript.Text = txtScript.Text.Insert(txtScript.SelectionStart, propName);

            txtScript.Focus();
            txtScript.SelectionStart = olSelectionStart + propName.Length;
            txtScript.Select(txtScript.SelectionStart, 0);
            txtScript.Focus();
        }
#if SILVERLIGHT
        private void txtScript_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                var olSelectionStart = txtScript.SelectionStart;
                txtScript.Text = txtScript.Text.Insert(txtScript.SelectionStart, "    ");

                txtScript.Focus();
                txtScript.SelectionStart = olSelectionStart + 4;
                txtScript.Select(txtScript.SelectionStart, 0);
                txtScript.Focus();
                e.Handled = true;
            }
        }
#endif
    }
}
