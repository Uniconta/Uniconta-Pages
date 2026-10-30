using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public class DiagramNodeViewModel : NotifyBase
    {
        private double _x;
        private double _y;
        private double _width;
        private double _height;
        private Action<PostingTraceStep> _action;
        private PostingTraceStep _postingTraceStep;

        public string Id { get; set; }

        public DiagramNodeKind Kind { get; set; }

        public string Title { get; set; }

        public string Subtitle { get; set; }

        public string Detail { get; set; }

        public ResolutionConfidence Confidence { get; set; }

        public PostingTraceArea Area { get; set; }

        public PostingAccountReason Reason { get; set; }


        public PostingTraceStep PostingTraceStep
        {
            get { return _postingTraceStep; }
            set { SetProperty(ref _postingTraceStep, value); }
        }

        public Action<PostingTraceStep> Action
        {
            get { return _action; }
            set
            {
                if (SetProperty(ref _action, value))
                {
                    OnPropertyChanged(nameof(HasAction));
                    OnPropertyChanged(nameof(CardCursor));
                }
            }
        }

        public bool HasAction => Action != null;

        public System.Windows.Input.Cursor CardCursor => HasAction ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.SizeAll;

        public double MinWidth { get; set; } = 180;
        public double MinHeight { get; set; } = 120;

        public double X
        {
            get { return _x; }
            set
            {
                if (SetProperty(ref _x, value))
                    RaiseGeometryPropertiesChanged();
            }
        }

        public double Y
        {
            get { return _y; }
            set
            {
                if (SetProperty(ref _y, value))
                    RaiseGeometryPropertiesChanged();
            }
        }

        public double Width
        {
            get { return _width; }
            set
            {
                if (SetProperty(ref _width, value))
                    RaiseGeometryPropertiesChanged();
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (SetProperty(ref _height, value))
                    RaiseGeometryPropertiesChanged();
            }
        }

        public double CenterX
        {
            get { return X + Width / 2.0; }
        }

        public double CenterY
        {
            get { return Y + Height / 2.0; }
        }

        public double LeftCenterX
        {
            get { return X; }
        }

        public double RightCenterX
        {
            get { return X + Width; }
        }

        public double MiddleY
        {
            get { return Y + Height / 2.0; }
        }

        public virtual string ToolTipText
        {
            get
            {
                var lines = new List<string>();

                if (!string.IsNullOrWhiteSpace(Title))
                    lines.Add(Title);

                if (!string.IsNullOrWhiteSpace(Subtitle))
                    lines.Add(Subtitle);

                if (!string.IsNullOrWhiteSpace(Detail))
                    lines.Add(Detail);

                return string.Join(Environment.NewLine, lines);
            }
        }

        public void ExecuteAction()
        {
            if (Action != null)
                Action(PostingTraceStep);
        }

        private void RaiseGeometryPropertiesChanged()
        {
            OnPropertyChanged(nameof(CenterX));
            OnPropertyChanged(nameof(CenterY));
            OnPropertyChanged(nameof(LeftCenterX));
            OnPropertyChanged(nameof(RightCenterX));
            OnPropertyChanged(nameof(MiddleY));
        }
    }

    public sealed class VoucherDiagramNodeViewModel : DiagramNodeViewModel
    {
        public GLTransRefType DCType { get; set; }
        public string DCAccount { get; set; }
        public string DCName { get; set; }

        public string DCAccountInfo => !string.IsNullOrEmpty(DCAccount) ? $"{DCType} {DCAccount} {DCName}" : Uniconta.ClientTools.Localization.lookup("GLTraceNoAccount");
        public string PostingCountText { get; set; }
    }

    public sealed class TransactionDiagramNodeViewModel : DiagramNodeViewModel
    {
        public GLAccountClient GLAccount { get; set; }

        public string AccountText => $"{GLAccount.Account} {GLAccount.Name} ({GLAccount.AccountType})";
        public string BookedDateText { get; set; }

        public string SystemAccountText => GLAccount.SystemAccount;
        public System.Windows.Visibility SystemAccountTextVisibility => string.IsNullOrWhiteSpace(GLAccount?.SystemAccount) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

        public string AmountText => $"{CurrencyCode} {Amount.ToString("N2", Thread.CurrentThread.CurrentCulture)}";

        public string CurrencyCode { get; set; }
        public double Amount { get; set; }

        public string Origin { get; set; }
        public string OriginText => Origin;
        public System.Windows.Visibility OriginTextVisibility => string.IsNullOrWhiteSpace(Origin) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

        public string Invoice { get; set; }

        public override string ToolTipText
        {
            get
            {
                var lines = new List<string>
                {
                    AccountText,
                    OriginText
                };

                if (!string.IsNullOrWhiteSpace(Detail))
                    lines.Add(Detail);

                return base.ToolTipText + Environment.NewLine +
                       string.Join(Environment.NewLine, lines);
            }
        }
    }

    public sealed class SectionDiagramNodeViewModel : DiagramNodeViewModel
    {
        public string SectionTitle { get; set; }
        public string RangeText { get; set; }
        public string Description { get; set; }
    }

    public sealed class ReasonDiagramNodeViewModel : DiagramNodeViewModel
    {
        public string SourceRecordInfo => $"{SourceTable?.Name ?? Uniconta.ClientTools.Localization.lookup("GLTraceNoTableSpecified")} '{SourceTableKey}'";
        public string SourceFieldInfo => SourceField;
        public string SourceField { get;set; }
        public Type SourceTable { get; set; }
        public string SourceTableKey { get; set; }

        public bool IsManual { get; set; }
    }
}
