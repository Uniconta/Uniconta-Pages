using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.API.System;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Reflection;
using Uniconta.ClientTools.Controls;
using System.Collections;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class FieldChangeLogPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TableFieldChangeLogClientLocal); } }
        public override IComparer GridSorting { get { return new TableFieldChangeLogTimeSort(); } }
    }

    public partial class FieldChangeLogPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.FieldChangeLogPage; } }

        DateTime filterDate;
        protected override Filter[] DefaultFilters()
        {
            if (filterDate != DateTime.MinValue)
            {
                Filter dateFilter = new Filter() { name = "Time", value = String.Format("{0:d}..", filterDate) };
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }

        public FieldChangeLogPage(UnicontaBaseEntity table, CrudAPI api) : this(table, null, api) { }
        public FieldChangeLogPage(UnicontaBaseEntity table, Uniconta.DataModel.TableChangeLog log, CrudAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgFieldChangeLog;
            SetRibbonControl(localMenu, dgFieldChangeLog);
            dgFieldChangeLog.api = api;
            if (log != null)
                filterDate = log._Time;
            else
                filterDate = BasePage.GetSystemDefaultDate().AddMonths(-3);
            dgFieldChangeLog.UpdateMaster(table);
            dgFieldChangeLog.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += gridRibbon_BaseActions;
            if (table is IdKey)
                api.LoadCacheInBackground(table.BaseEntityType());
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var selectedItem = dg.SelectedItem as TableFieldChangeLogClientLocal;
            if (selectedItem != null)
            {
                var tableType = dgFieldChangeLog.masterRecord.GetType();
                lookup.TableType = tableType;
            }
            return lookup;
        }
    }

    public class TableFieldChangeLogClientLocal : TableFieldChangeLogClient
    {
        [Display(Name = "OldClientValue", ResourceType = typeof(TableFieldChangeLogText))]
        public string OldClientValue { get { return GetValues(oldRec, newRec, false); } }

        [Display(Name = "NewClientValue", ResourceType = typeof(TableFieldChangeLogText))]
        public string NewClientValue { get { return GetValues(oldRec, newRec, true); } }

        string GetValues(UnicontaBaseEntity oldRecord, UnicontaBaseEntity newRecord, bool IsNewValue)
        {
            if (oldRecord == null)
                return null;
            var prop = oldRecord.GetType().GetProperty(this._PropName);
            if (prop != null)
            {
                return CheckProperty(oldRecord, newRecord, IsNewValue, prop);
            }
            StringBuilderReuse values = null;
            string singleVal = null;
            foreach (var RecProperty in oldRecord.GetType().GetProperties())
            {
                var val = CheckProperty(oldRecord, newRecord, IsNewValue, RecProperty);
                if (val != null)
                {
                    if (singleVal == null)
                        singleVal = val;
                    else
                    {
                        if (values == null)
                        {
                            values =  StringBuilderReuse.Create();
                            values.Append(singleVal);
                        }
                        values.Append(';').Append(val);
                    }
                }
            }
            return (values != null) ? values.ToStringAndRelease() : singleVal;
        }

        string CheckProperty(UnicontaBaseEntity oldRecord, UnicontaBaseEntity newRecord, bool IsNewValue, PropertyInfo RecProperty)
        {
#if !SILVERLIGHT
            var oldValue = RecProperty.GetValue(oldRecord);
            var newValue = RecProperty.GetValue(newRecord);
#else
            var oldValue = RecProperty.GetValue(oldRecord, null);
            var newValue = RecProperty.GetValue(newRecord, null);
#endif
            if (oldValue == null && newValue == null)
                return null;
            if (oldValue != null && newValue != null)
            {
                if (ValuesAreEqual(oldValue, newValue, RecProperty.PropertyType))
                    return null;
            }
            string name;
            if (!RecProperty.Name.StartsWith("Dim"))
            {
                var attribute = RecProperty.GetCustomAttributes(typeof(DisplayAttribute), true);
                if (attribute.Count() == 0)
                    return null;
                var displayAttribute = (DisplayAttribute)attribute[0];
                name = Uniconta.ClientTools.Localization.lookup(displayAttribute.Name);
            }
            else
            {
                name = RecProperty.Name;
            }

            if (IsNewValue)
                return name + "=" + newValue?.ToString();
            else
                return name + "=" + oldValue?.ToString();
        }

        bool ValuesAreEqual(object oldValue, object newValue, Type propType)
        {
            if (propType == typeof(bool) || propType == typeof(bool?))
                return (bool)oldValue == (bool)newValue;
            else if (propType == typeof(string))
                return (string)oldValue == (string)newValue;
            else if (propType == typeof(DateTime))
                return (DateTime)oldValue == (DateTime)newValue;
            else if (propType == typeof(double))
                return (double)oldValue == (double)newValue;
            else if (propType == typeof(long))
                return (long)oldValue == (long)newValue;
            else if (propType == typeof(short))
                return (short)oldValue == (short)newValue;
            else if (propType == typeof(int))
                return (int)oldValue == (int)newValue;
            else if (propType == typeof(byte))
                return (byte)oldValue == (byte)newValue;
            else if (propType == typeof(float))
                return (float)oldValue == (float)newValue;
            else
                return Convert.ToString(oldValue) == Convert.ToString(newValue);
        }
    }
}
