using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public sealed class ConfidenceToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ResolutionConfidence))
                return 1.5;

            var confidence = (ResolutionConfidence)value;

            switch (confidence)
            {
                case ResolutionConfidence.High:
                    return 3.0;

                case ResolutionConfidence.Medium:
                    return 2.2;

                case ResolutionConfidence.Low:
                    return 1.4;

                case ResolutionConfidence.Unknown:
                default:
                    return 1.0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
