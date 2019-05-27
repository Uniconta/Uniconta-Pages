using System.IO;

namespace UnicontaISO20022CreditTransfer
{
    /// <summary>
    /// Interface for XML documents
    /// </summary>
    public interface IDocument
    {
        #region Properties

        /// <summary>
        /// The type of document.
        /// </summary>
        DocumentType DocumentType { get; }

     /*
        /// <summary>
        /// The document profile.
        /// </summary>
        Profile Profile { get; }
*/
        #endregion

        #region Methods

        /// <summary>
        /// Writes the document to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        void ToStream(Stream stream);

        #endregion
    }
}

