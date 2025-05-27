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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Script;
using Uniconta.API.Plugin;

namespace UnicontaClient.Pages
{
    /// <summary>
    /// Interaction logic for CWAddFieldValues.xaml
    /// </summary>
    public partial class CWAddScript : ChildWindow
    {
        Type objType;
        CrudAPI api;
        public UnicontaCompileResult Expr;
        UnicontaBaseEntity row;

        public CWAddScript(CrudAPI api, Type objType)
        {
            Init(api, objType, null);
        }
        public CWAddScript(CrudAPI api, UnicontaBaseEntity obj, string script)
        {
            row = obj;
            Init(api, obj.GetType(), script);
        }

        private void Init(CrudAPI api, Type objType, string script)
        {
            this.api = api;
            this.objType = objType;
            InitializeComponent();
            this.DataContext = this;
            Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Script"));
            this.Loaded += CW_Loaded;
            lblScript.Text = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Properties"));
            cmbExternType.ItemsSource = new List<string> { "rec", "Company" };
            cmbExternType.SelectedIndex = 0;
            if (!string.IsNullOrWhiteSpace(script))
                this.txtScript.Text = script;
            chkIsCsharp.IsChecked = CWAddFieldValues.isCSharp;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cmbExternType.Focus(); }));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            CWAddFieldValues.isCSharp = this.chkIsCsharp.IsChecked.GetValueOrDefault();
            Expr = Compile(api, objType, txtScript.Text, row, CWAddFieldValues.isCSharp, false);
            if (Expr != null)
                SetDialogResult(true);
        }

        private void cmbExternType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            Type type;
            if (cmbExternType.SelectedIndex == 0)
                type = objType;
            else
                type = typeof(CompanyClient);
            var props = type.GetProperties();
            var propertyNames = new List<string>(props.Length);
            foreach (var p in props)
            {
                if (p.PropertyType == typeof(string) || typeof(IEnumerable).IsAssignableFrom(p.PropertyType) == false)
                    propertyNames.Add(p.Name + " (" + p.PropertyType + ")");
            }
            propertyNames.Sort();
            cmbProperties.ItemsSource = propertyNames;
            cmbProperties.SelectedIndex = 0;
        }
        public static UnicontaCompileResult Compile(CrudAPI api, Type objType, string script, UnicontaBaseEntity row = null, bool isCSharp = false, bool returnValue = false)
        {
            return UtilDisplay.Compile(api, objType, script, row, isCSharp, returnValue);
        }

        private void btnScriptHelper_Click(object sender, RoutedEventArgs e)
        {
            var olSelectionStart = txtScript.SelectionStart;
            var selectedText = Convert.ToString(cmbProperties.SelectedItem);
            if (selectedText != null)
            {
                var pos = selectedText.IndexOf(" (");
                if (pos >= 0)
                {
                    var propName = string.Format("{0}.{1}", cmbExternType.SelectedItem, selectedText.Substring(0, pos));
                    if (string.IsNullOrEmpty(txtScript.Text))
                        txtScript.Text = propName;
                    else
                        txtScript.Text = txtScript.Text.Insert(txtScript.SelectionStart, propName);

                    txtScript.Focus();
                    txtScript.SelectionStart = olSelectionStart + propName.Length;
                    txtScript.Select(txtScript.SelectionStart, 0);
                    txtScript.Focus();
                }
            }
        }

        private void Compile_Click(object sender, RoutedEventArgs e)
        {
            CWAddFieldValues.isCSharp = this.chkIsCsharp.IsChecked.GetValueOrDefault();
            Expr = Compile(api, objType, txtScript.Text, row, CWAddFieldValues.isCSharp, false);
            if (Expr != null)
                UnicontaMessageBox.Show(Expr.expr?.ToString() ?? Uniconta.ClientTools.Localization.lookup("ok"), Uniconta.ClientTools.Localization.lookup("Message"));
        }
    }
}
