using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public sealed class NodeKindToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var kind = value is DiagramNodeKind ? (DiagramNodeKind)value : DiagramNodeKind.Reason;
            return GetAccent(kind);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static System.Windows.Media.Color GetAccent(DiagramNodeKind kind)
        {
            switch (kind)
            {
                case DiagramNodeKind.Voucher:
                    return FindColor("PostingTraceVoucherAccentColor");

                case DiagramNodeKind.Transaction:
                    return FindColor("PostingTraceTransactionAccentColor");

                case DiagramNodeKind.Section:
                    return FindColor("PostingTraceSectionAccentColor");

                case DiagramNodeKind.Reason:
                default:
                    return FindColor("PostingTraceReasonAccentColor");
            }
        }

        private static System.Windows.Media.Color FindColor(string resourceKey)
        {
            return (System.Windows.Media.Color)System.Windows.Application.Current.FindResource(resourceKey);
        }
    }
}
