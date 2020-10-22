using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
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
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Settings;
using DevExpress.Xpf.Grid.LookUp;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools.Controls;
using Uniconta.API.System;
using Uniconta.DataModel;
using System.Windows.Markup;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TableValuePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TableValueClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class TableValuePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.TableValue; } }
        string tableName;
        int tableId = 0;
        List<Type> tablestype;
        bool _fromDropDown = false;
        public TableValuePage(Type tableType, CrudAPI api) : base(api, string.Empty)
        {
            InitPage();
            InitializeLocalFields(tableType);
        }

        public TableValuePage(Type tableType, bool fromDropDown, Company company) : base(company)
        {
            InitPage();
            _fromDropDown = fromDropDown;
            InitializeLocalFields(tableType);
        }

        public TableValuePage(int _tableId, Type tableType, Company company) : base(company)
        {
            this.tableId = _tableId;
            InitPage();
            GetFields(tableType);
        }

        void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgTableValueGrid;
            SetRibbonControl(localMenu, dgTableValueGrid);
            dgTableValueGrid.api = api;
            dgTableValueGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        void InitializeLocalFields(Type tableType)
        {
            var obj = Activator.CreateInstance(tableType) as UnicontaBaseEntity;
            if (obj != null)
            {
                tableId = obj.ClassId();
                if (tableId == Uniconta.DataModel.TableData.CLASS_ID)
                    tableId = ((Uniconta.DataModel.TableData)obj).GetClassIdSpecial();
            }
            GetFields(tableType);
        }

        void BindGrid()
        {
            var propValuePair = new List<PropValuePair>() { PropValuePair.GenereteWhereElements("TableId", typeof(int), Convert.ToString(tableId)) };
            Filter(propValuePair);
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgTableValueGrid.Filter(propValuePair);
        }

        public override Task InitQuery()
        {
            BindGrid();
            return null;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    TableValueClient objTableValue = new TableValueClient();
                    objTableValue.TableNo = tableId;
                    if (_fromDropDown)
                        objTableValue.ShowInDropdown = true;
                    dgTableValueGrid.AddRow(objTableValue);
                    break;
                case "DeleteRow":
                    dgTableValueGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    dgTableValueGrid.SelectedItem = null;
                    var t = dgTableValueGrid.SaveData();
                    t.ContinueWith((e) => api.CompanyEntity.LoadTableValues(api));
                    break;
                case "RefreshGrid":
                    BindGrid();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void GetFields(Type dialogType = null)
        {
            Type sltype = null;
            if (dialogType != null)
            {
                var usertype = api.CompanyEntity.GetUserType(dialogType);
                if (usertype != null)
                    sltype = usertype;
            }

            tablestype = Global.GetTables(api.CompanyEntity);
            if (dialogType == null)
                sltype = (from l in tablestype where l.Name.Split('.').Last() == tableName select l).FirstOrDefault();
            if (sltype == null)
                sltype = dialogType;

            FilterSortHelper FilterSortingHelper = new FilterSortHelper(sltype, null, null, api, null);
            var displayProperties = FilterSortingHelper.GetDisplayProperties(true, true);
            displayProperties.AddRange(GetAllInputPropertiesFromType(sltype, api.CompanyEntity));
            dgTableValueGrid.Tag = displayProperties;
            var customDictionary = UtilFunctions.GetCustomFormattedNonReadOnlyDisplayPropertyNames(sltype, null);
            var dimProp = (from p in displayProperties where p.PropertyName.StartsWith("Dim") select p);
            foreach (var dim in dimProp)
            {
                var prName = dim.PropertyName;
                if (!customDictionary.ContainsKey(prName))
                    customDictionary.Add(prName, string.Concat(prName, " (", dim.DisplayName, ")"));
            }
            cmbTableProperties.ItemsSource = customDictionary.OrderBy(s => s.Value);
        }

        public List<DisplayProperties> GetAllInputPropertiesFromType(Type type, Company comp)
        {
            List<DisplayProperties> names = new List<DisplayProperties>();
            var propertyNames = (from p in type.GetProperties()
                                 where p.GetCustomAttributes(typeof(InputFieldDataAttribute), true).Length > 0
                                 select p).ToList();
            if (propertyNames == null)
                return names;
            names.Capacity = propertyNames.Count;
            foreach (var p in propertyNames)
            {
                var prop = new DisplayProperties() { PropertyName = p.Name, DisplayName = p.Name, PropertyType = p.PropertyType };
                names.Add(prop);
                var ForeignKeys = p.GetCustomAttributes(typeof(ForeignKeyAttribute), true);
                if (ForeignKeys.Length > 0)
                    prop.ForeignKey = ((ForeignKeyAttribute)ForeignKeys[0]).ForeignKeyTable;
                var AppEnumNames = p.GetCustomAttributes(typeof(AppEnumAttribute), true);
                if (AppEnumNames.Length > 0)
                    prop.EnumName = ((AppEnumAttribute)AppEnumNames[0]).EnumName;
            }
            return names;
        }
    }

    public class UserInputDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate EnumTypeTemplate { get; set; }
        public DataTemplate DateTypeTemplate { get; set; }
        public DataTemplate AppEnumTypeTemplate { get; set; }
        public DataTemplate GenericTypeTemplate { get; set; }
        public DataTemplate BooleanTypeTemplate { get; set; }
        public DataTemplate ForeignKeyTypeDataTemplate { get; set; }
        public DataTemplate IntergerTypeTemplate { get; set; }
        public DataTemplate DoubleTypeTemplate { get; set; }

        /// <summary>
        /// Method Returns the Template
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var data = item as EditGridCellData;
            var tableValue = data.RowData.Row as TableValueClient;
            var propName = tableValue?._Property;
            if (string.IsNullOrEmpty(propName))
                return null;
            var properties = data.View.DataControl.Tag as List<DisplayProperties>;
            if (properties == null)
                return null;
            var prop = properties.Where(p => p.PropertyName == propName).ToList();
            var property = prop?.FirstOrDefault();
            if (property == null)
                return null;
            if (property.PropertyType == typeof(DateTime))
                return DateTypeTemplate;
            else if (property.PropertyType.IsEnum)
#if !SILVERLIGHT
                return CreateTemplateforEnumType(property.PropertyType);
#else
                return GenericTypeTemplate;
#endif
            else if (property.PropertyType == typeof(bool))
            {
                if (tableValue.Value == null)
                    tableValue.Value = "false";
                return BooleanTypeTemplate;
            }
            else if (property.PropertyType == typeof(string))
            {
                if (!string.IsNullOrEmpty(property.EnumName))
                    return CreateTemplateForAppEnumType(property.EnumName);
                else if (property.ForeignKey != null)
                {
                    var grd = data.View.DataControl as CorasauDataGrid;
                    var api = grd?.api;
#if !SILVERLIGHT
                    return CreateTemplateForForeignKeyType(property.ForeignKey, api);

#else
                    return GenericTypeTemplate;
#endif
                }
                else
                    return GenericTypeTemplate;
            }
            else if (property.PropertyType == typeof(double))
                return CreateTemplateforDouble(property.PropertyType);
            else if (property.PropertyType == typeof(int))
                    return GenericTypeTemplate;

            return base.SelectTemplate(item, container);
        }

#if !SILVERLIGHT
        /// <summary>
        /// Template for Foreign Key Type data
        /// </summary>
        /// <param name="foreigntype"></param>
        /// <returns></returns>
        private DataTemplate CreateTemplateForForeignKeyType(Type foreigntype, CrudAPI api)
        {

            ForeignKeyTypeDataTemplate = new DataTemplate();
            var typeInstance = Activator.CreateInstance(foreigntype);
            var itemsSource = api?.QuerySync(typeInstance as Uniconta.Common.UnicontaBaseEntity, null, null);
            var lookupEdit = new FrameworkElementFactory(typeof(LookUpEdit));
            lookupEdit.Name = "PART_Editor";
            lookupEdit.SetValue(LookUpEdit.DisplayMemberProperty, "KeyStr");
            lookupEdit.SetValue(LookUpEdit.ValueMemberProperty, "KeyStr");
            lookupEdit.SetValue(LookUpEdit.AutoPopulateColumnsProperty, false);
            lookupEdit.SetValue(LookUpEdit.ValidateOnTextInputProperty, true);
            lookupEdit.SetValue(LookUpEdit.IsTextEditableProperty, true);
            lookupEdit.SetValue(LookUpEdit.PopupContentTemplateProperty, (Application.Current).Resources["LookupEditTemplateSelectorPopupContent"] as ControlTemplate);
            lookupEdit.SetValue(LookUpEdit.ItemsSourceProperty, itemsSource);
            ForeignKeyTypeDataTemplate.VisualTree = lookupEdit;
            return ForeignKeyTypeDataTemplate;
        }
#endif
        /// <summary>
        /// Templeat for App Enums type data
        /// </summary>
        /// <param name="appEnumName"></param>
        /// <returns></returns>
        private DataTemplate CreateTemplateForAppEnumType(string appEnumName)
        {
#if !SILVERLIGHT
            AppEnumTypeTemplate = new DataTemplate();
            var appEnum = typeof(AppEnums).GetField(appEnumName);
            var appEnumValues = (AppEnums)(appEnum.GetValue(null));
            var comboBoxEdit = new FrameworkElementFactory(typeof(ComboBoxEdit));
            comboBoxEdit.Name = "PART_Editor";
            comboBoxEdit.SetValue(ComboBoxEdit.ItemsSourceProperty, appEnumValues.Values);
            AppEnumTypeTemplate.VisualTree = comboBoxEdit;
            return AppEnumTypeTemplate;

           
#else
            return GetAppEnumXamlString(appEnumName);
#endif
        }


#if !SILVERLIGHT
        /// <summary>
        /// Creates a Template for Enum Type Property
        /// </summary>
        /// <param name="enumTypeProperty"></param>
        /// <returns></returns>
        private DataTemplate CreateTemplateforEnumType(Type enumTypeProperty)
        {
            EnumTypeTemplate = new DataTemplate();
            EnumTypeTemplate.DataType = enumTypeProperty;
            FrameworkElementFactory comboBoxEdit = new FrameworkElementFactory(typeof(ComboBoxEdit));
            comboBoxEdit.Name = "PART_Editor";
            comboBoxEdit.SetValue(ComboBoxEdit.ItemsSourceProperty, Enum.GetValues(enumTypeProperty));
            EnumTypeTemplate.VisualTree = comboBoxEdit;
            return EnumTypeTemplate;
        }
#endif
        private DataTemplate CreateTemplateforDouble(Type doubleTypeProperty)
        {
#if !SILVERLIGHT
            DoubleTypeTemplate = new DataTemplate();
            DoubleTypeTemplate.DataType = doubleTypeProperty;
            var doubleEdit = new FrameworkElementFactory(typeof(DoubleEditor));
            doubleEdit.Name = "PART_Editor";
            doubleEdit.SetValue(DoubleEditor.MarginProperty , new Thickness(-20,0,0,0));
            DoubleTypeTemplate.VisualTree = doubleEdit;
            return DoubleTypeTemplate;
#else
            return GetDoubleXamlString();
#endif
        }
#if SILVERLIGHT
        private DataTemplate GetDoubleXamlString()
        {
           
        const string assemblyName = "Uniconta.SLTools";
                    string cellTemplate = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
                                        " xmlns:CorasauControls=\"clr-namespace:Uniconta.ClientTools.Controls;assembly=" + assemblyName + "\" >" +
                                        "<CorasauControls:DoubleEditor x:Name=\"PART_Editor\" \"/></DataTemplate>";
            return (DataTemplate)XamlReader.Load(cellTemplate);
        }
        private DataTemplate GetAppEnumXamlString(string appEnumName)
        {
            const string assemblyName = "Uniconta.SLTools";
            string cellTemplate = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
                                        " xmlns:CorasauControls=\"clr-namespace:Uniconta.ClientTools.Controls;assembly=" + assemblyName + "\" >" +
                                        "<CorasauControls:ComboBoxEditor x:Name=\"PART_Editor\" AppEnumName=\"" + appEnumName + "\"/>" + "</DataTemplate>";
            return (DataTemplate)XamlReader.Load(cellTemplate);
        }
#endif
    }
}
