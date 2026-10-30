using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public sealed class ConfidenceToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ResolutionConfidence))
                return GetBrush("PostingTraceConfidenceUnknownBrush");

            var confidence = (ResolutionConfidence)value;

            switch (confidence)
            {
                case ResolutionConfidence.High:
                    return GetBrush("PostingTraceConfidenceHighBrush");

                case ResolutionConfidence.Medium:
                    return GetBrush("PostingTraceConfidenceMediumBrush");

                case ResolutionConfidence.Low:
                    return GetBrush("PostingTraceConfidenceLowBrush");

                case ResolutionConfidence.Unknown:
                default:
                    return GetBrush("PostingTraceConfidenceUnknownBrush");
            }
        }

        private static System.Windows.Media.Brush GetBrush(string resourceKey)
        {
            return (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource(resourceKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
