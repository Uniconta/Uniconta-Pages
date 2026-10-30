using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uniconta.API.System;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public sealed class PostingTraceDiagramViewModel : NotifyBase
    {
        private readonly ObservableCollection<PostingTraceTreeItemViewModel> _debugItems;
        private readonly ObservableCollection<PostingTraceMatchResult> _matchRows;

        private const double MinimumDiagramWidth = 1320.0;
        private const double MinimumDiagramHeight = 900.0;
        private const double DiagramPadding = 30.0;

        private readonly ObservableCollection<DiagramNodeViewModel> _nodes;
        private readonly ObservableCollection<DiagramConnectorViewModel> _connectors;
        private double _diagramWidth = MinimumDiagramWidth;
        private double _diagramHeight = MinimumDiagramHeight;

        private readonly Company _company;

        public PostingTraceDiagramViewModel(Company company)
        {
            _company = company;

            _nodes = new ObservableCollection<DiagramNodeViewModel>();
            _nodes.CollectionChanged += Nodes_CollectionChanged;
            _connectors = new ObservableCollection<DiagramConnectorViewModel>();
            _debugItems = new ObservableCollection<PostingTraceTreeItemViewModel>();
            _matchRows = new ObservableCollection<PostingTraceMatchResult>();
        }

        public ObservableCollection<PostingTraceTreeItemViewModel> DebugItems => _debugItems;
        public ObservableCollection<PostingTraceMatchResult> MatchRows => _matchRows;
        public ObservableCollection<DiagramNodeViewModel> Nodes => _nodes;
        public ObservableCollection<DiagramConnectorViewModel> Connectors => _connectors;

        public double DiagramWidth
        {
            get { return _diagramWidth; }
            private set { SetProperty(ref _diagramWidth, value); }
        }

        public double DiagramHeight
        {
            get { return _diagramHeight; }
            private set { SetProperty(ref _diagramHeight, value); }
        }

        public void Load(IEnumerable<PostingTraceResult> results)
        {
            _nodes.Clear();
            _connectors.Clear();
            _debugItems.Clear();
            _matchRows.Clear();

            if (results == null)
            {
                UpdateDiagramSize();
                return;
            }

            var resultList = results.Where(r => r != null).ToList();

            if (resultList.Count == 0)
            {
                UpdateDiagramSize();
                return;
            }

            PostingTraceDiagramBuilder.Build(_company, resultList, _nodes, _connectors);
            UpdateDiagramSize();

            PostingTraceTreeBuilder.Build(resultList, _debugItems);

            foreach (var result in resultList)
            {
                var transaction = result.GLTrans;
                if (transaction == null)
                    continue;

                foreach (var step in result.MatchedPostingSources)
                {
                    _matchRows.Add(new PostingTraceMatchResult
                    {
                        Voucher = transaction.Voucher,
                        VoucherLine = transaction.VoucherLine,
                        Date = transaction.Date,
                        IsManual = step.IsManual,
                        TransactionAccount = transaction.Account,
                        TransactionAccountName = transaction.Name,
                        TransactionText = transaction.Text,
                        TransactionAmount = transaction.Amount,
                        MatchingAccount = step.MatchingAccount,
                        AccountName = step.AccountName,
                        AccountType = step.AccountType,
                        Area = step.Area,
                        Reason = step.Reason,
                        Confidence = step.Confidence,
                        SourceTable = step.SourceTable == null ? null : step.SourceTable.Name,
                        SourceTableKey = step.SourceTableKey,
                        SourceField = step.SourceField,
                        Explanation = step.Explanation
                    });
                }
            }
        }

        private void Nodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (DiagramNodeViewModel node in e.OldItems)
                    node.PropertyChanged -= Node_PropertyChanged;
            }

            if (e.NewItems != null)
            {
                foreach (DiagramNodeViewModel node in e.NewItems)
                    node.PropertyChanged += Node_PropertyChanged;
            }

            UpdateDiagramSize();
        }

        private void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DiagramNodeViewModel.X) ||
                e.PropertyName == nameof(DiagramNodeViewModel.Y) ||
                e.PropertyName == nameof(DiagramNodeViewModel.Width) ||
                e.PropertyName == nameof(DiagramNodeViewModel.Height))
            {
                UpdateDiagramSize();
            }
        }

        private void UpdateDiagramSize()
        {
            var maxRight = _nodes.Count > 0 ? _nodes.Max(n => n.X + n.Width) + DiagramPadding : MinimumDiagramWidth;
            var maxBottom = _nodes.Count > 0 ? _nodes.Max(n => n.Y + n.Height) + DiagramPadding : MinimumDiagramHeight;

            DiagramWidth = System.Math.Max(MinimumDiagramWidth, maxRight);
            DiagramHeight = System.Math.Max(MinimumDiagramHeight, maxBottom);
        }
    }
}
