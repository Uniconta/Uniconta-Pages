using System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    /// <summary>
    /// A problem found while reading a SIE file. Detail is the part of the file it concerns (a raw line, a voucher
    /// or an item), so that the page can show it in a localized message.
    /// </summary>
    public class SIEException : Exception
    {
        public SIEException(string description, string detail = null) : base(description) { Detail = detail; }

        public string Detail { get; }
    }

    public class SIEInvalidChecksumException : SIEException
    {
        public SIEInvalidChecksumException(string description) : base(description) { }
    }

    /// <summary>
    /// The file announced a control total (#KSUMMA) but the closing #KSUMMA item is missing: the file is truncated.
    /// </summary>
    public class SIETruncatedFileException : SIEException
    {
        public SIETruncatedFileException(string description) : base(description) { }
    }

    /// <summary>
    /// The file is not in PC8 (codepage 437) as the standard prescribes (5.8), but in the encoding of its byte order
    /// mark. Detail is the name of that encoding.
    /// </summary>
    public class SIEEncodingException : SIEException
    {
        public SIEEncodingException(string description, string detail = null) : base(description, detail) { }
    }

    public class SIEInvalidFileException : SIEException
    {
        public SIEInvalidFileException(string description) : base(description) { }
    }

    [Serializable]
    public class SIEDateException : SIEException
    {
        public SIEDateException(string description, string detail = null) : base(description, detail) { }
    }

    public class SIEInvalidFieldException : SIEException
    {
        public SIEInvalidFieldException(string description, string detail = null) : base(description, detail) { }
    }

    public class SIEInvalidFeatureException : SIEException
    {
        public SIEInvalidFeatureException(string description, string detail = null) : base(description, detail) { }
    }

    public class SIEMissingMandatoryDateException : SIEException
    {
        public SIEMissingMandatoryDateException(string description, string detail = null) : base(description, detail) { }
    }

    public class SIEMissingObjectException : SIEException
    {
        public SIEMissingObjectException(string description, string detail = null) : base(description, detail) { }
    }

    public class SIEVoucherMissmatchException : SIEException
    {
        public SIEVoucherMissmatchException(string description, string detail = null) : base(description, detail) { }
    }

    public class SIEStructureException : SIEException
    {
        public SIEStructureException(string description, string detail = null) : base(description, detail) { }
    }
}
