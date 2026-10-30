using System;
using System.Collections.Generic;
using System.Linq;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    public class PostingTraceResult
    {
        private readonly List<PostingTraceStep> _checkedPostingSources;

        public PostingTraceResult(GLTransClient glTrans)
        {
            GLTrans = glTrans;
            _checkedPostingSources = new List<PostingTraceStep>();
        }

        public GLTransClient GLTrans { get; private set; }

        public IEnumerable<PostingTraceStep> MatchedPostingSources => _checkedPostingSources.Where(o => o != null && o.IsMatch);
        public IEnumerable<PostingTraceStep> CheckedPostingSources => _checkedPostingSources;

        public bool HasPostingSourceMatch => _checkedPostingSources.Any(o => o != null && o.IsMatch);

        public void Add(PostingTraceStep step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            _checkedPostingSources.Add(step);
        }
    }
}
