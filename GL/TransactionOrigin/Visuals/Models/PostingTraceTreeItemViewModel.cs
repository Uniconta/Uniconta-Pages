using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public sealed class PostingTraceTreeItemViewModel : NotifyBase
    {
        private readonly ObservableCollection<PostingTraceTreeItemViewModel> _children;

        public PostingTraceTreeItemViewModel()
        {
            _children = new ObservableCollection<PostingTraceTreeItemViewModel>();
        }

        public string Title { get; set; }

        public string Detail { get; set; }

        public bool IsMatch { get; set; }

        public ResolutionConfidence Confidence { get; set; }

        public PostingTraceArea Area { get; set; }

        public PostingAccountReason Reason { get; set; }

        public string Account { get; set; }

        public ObservableCollection<PostingTraceTreeItemViewModel> Children
        {
            get { return _children; }
        }

        public string StatusText
        {
            get { return IsMatch ? "MATCH" : "tried"; }
        }

        public string ToolTipText
        {
            get
            {
                var lines = new List<string>();

                if (!string.IsNullOrWhiteSpace(Title))
                    lines.Add(Title);

                if (!string.IsNullOrWhiteSpace(Account))
                    lines.Add(Uniconta.ClientTools.Localization.lookup("Accountkey") + ": " + Account);

                lines.Add(Uniconta.ClientTools.Localization.lookup("Phase") + ": " + StatusText);
                lines.Add(Uniconta.ClientTools.Localization.lookup("GLTraceConfidence") + ": " + PostingTraceTextFormatter.Confidence(Confidence));

                if (!string.IsNullOrWhiteSpace(Detail))
                    lines.Add(Detail);

                return string.Join(Environment.NewLine, lines);
            }
        }
    }
}
