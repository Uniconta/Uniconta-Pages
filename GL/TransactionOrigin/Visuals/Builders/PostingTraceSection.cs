using System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    internal sealed class PostingTraceSection : IEquatable<PostingTraceSection>
    {
        public PostingTraceSection(int sortOrder, string title, string rangeText, string description)
        {
            SortOrder = sortOrder;
            Title = title;
            RangeText = rangeText;
            Description = description;
        }

        public int SortOrder { get; private set; }
        public string Title { get; private set; }
        public string RangeText { get; private set; }
        public string Description { get; private set; }

        public bool Equals(PostingTraceSection other)
        {
            if (other == null)
                return false;

            return SortOrder == other.SortOrder;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PostingTraceSection);
        }

        public override int GetHashCode()
        {
            return SortOrder.GetHashCode();
        }
    }
}
