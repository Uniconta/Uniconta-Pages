using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class TileParametersStepView : System.Windows.Controls.UserControl
    {
        public TileParametersStepView() => InitializeComponent();
    }

    public sealed class TileNavigationTargetConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            => new StandardValuesCollection(new[] { string.Empty, Uniconta.ClientTools.UnicontaTabs.Vouchers }
                .Concat(UserControlProperties.ControlList?.Keys.AsEnumerable() ?? Enumerable.Empty<string>())
                .Distinct().OrderBy(target => target).ToArray());
    }

    public sealed class TileParameterViewModel : NotifyPropertyChangedViewModel, IDataErrorInfo
    {
        readonly object settings;
        readonly PropertyDescriptor property;
        readonly Action changed;
        readonly Func<bool> canEdit;
        string value, error;

        internal TileParameterViewModel(object settings, PropertyDescriptor property, Action changed, Func<bool> canEdit)
        {
            this.settings = settings;
            this.property = property;
            this.changed = changed;
            this.canEdit = canEdit;
            value = property.Converter.ConvertToString(null, CultureInfo.CurrentCulture, property.GetValue(settings));
            var converter = property.Converter;
            IsChoiceEditable = !converter.GetStandardValuesExclusive();
            if (converter.GetStandardValuesSupported())
                Choices = converter.GetStandardValues().Cast<object>()
                    .Select(item => converter.ConvertToString(null, CultureInfo.CurrentCulture, item)).ToArray();
        }

        public string Caption => property.DisplayName;
        public string Description => property.Description;
        public string Category => property.Category;
        public IReadOnlyList<string> Choices { get; }
        public bool HasChoices => Choices != null;
        public bool IsChoiceEditable { get; }
        public string Error => error;
        public string this[string columnName] => columnName == nameof(Value) ? error : null;
        public string Value
        {
            get => value;
            set
            {
                if (!canEdit() || this.value == value) return;
                this.value = value;
                error = null;
                try
                {
                    var converted = property.Converter.ConvertFromString(null, CultureInfo.CurrentCulture, value ?? string.Empty);
                    property.SetValue(settings, converted);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is NotSupportedException || ex is OverflowException || ex is System.Reflection.TargetInvocationException)
                {
                    error = Caption + ": " + (ex.InnerException ?? ex).Message;
                }
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(Error));
                changed();
            }
        }
    }
}
