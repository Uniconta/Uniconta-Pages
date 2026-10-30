using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public sealed class DiagramConnectorViewModel : NotifyBase
    {
        private DiagramNodeViewModel _fromNode;
        private DiagramNodeViewModel _toNode;

        public string Id { get; set; }

        public DiagramConnectorKind Kind { get; set; }

        public ResolutionConfidence Confidence { get; set; }

        public DiagramNodeViewModel FromNode
        {
            get { return _fromNode; }
            set
            {
                if (_fromNode != null)
                    _fromNode.PropertyChanged -= Node_PropertyChanged;

                _fromNode = value;

                if (_fromNode != null)
                    _fromNode.PropertyChanged += Node_PropertyChanged;

                RaiseConnectorPropertiesChanged();
            }
        }

        public DiagramNodeViewModel ToNode
        {
            get { return _toNode; }
            set
            {
                if (_toNode != null)
                    _toNode.PropertyChanged -= Node_PropertyChanged;

                _toNode = value;

                if (_toNode != null)
                    _toNode.PropertyChanged += Node_PropertyChanged;

                RaiseConnectorPropertiesChanged();
            }
        }

        public double X1
        {
            get { return FromNode != null ? FromNode.RightCenterX : 0.0; }
        }

        public double Y1
        {
            get { return FromNode != null ? FromNode.MiddleY : 0.0; }
        }

        public double X2
        {
            get { return ToNode != null ? ToNode.LeftCenterX : 0.0; }
        }

        public double Y2
        {
            get { return ToNode != null ? ToNode.MiddleY : 0.0; }
        }

        private void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaiseConnectorPropertiesChanged();
        }

        private void RaiseConnectorPropertiesChanged()
        {
            OnPropertyChanged(nameof(X1));
            OnPropertyChanged(nameof(Y1));
            OnPropertyChanged(nameof(X2));
            OnPropertyChanged(nameof(Y2));
        }
    }
}
