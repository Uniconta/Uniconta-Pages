using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Base class for CreationResult.
    /// </summary>
    public class AltinnCreationResult
    {
        private readonly XmlDocument altinnDocument;
        private readonly XmlDocument responsDocument;
        private readonly bool hasPreCheckErrors;
        private readonly IEnumerable<AltinnResponse> responseInfo;
        private readonly IEnumerable<AltinnPrecheckError> precheckErrors;

        public AltinnCreationResult(XmlDocument altinnDocument, XmlDocument responsDocument, bool hasPreCheckErrors, IEnumerable<AltinnResponse> responseInfo, IEnumerable<AltinnPrecheckError> precheckErrors)
        {
            this.altinnDocument = altinnDocument;
            this.responsDocument = responsDocument;
            this.hasPreCheckErrors = hasPreCheckErrors;
            this.responseInfo = responseInfo;
            this.precheckErrors = precheckErrors;
        }

        public AltinnCreationResult(XmlDocument responsDocument, bool hasPreCheckErrors, IEnumerable<AltinnResponse> responseInfo, IEnumerable<AltinnPrecheckError> precheckErrors)
        {
            this.responsDocument = responsDocument;
            this.hasPreCheckErrors = hasPreCheckErrors;
            this.responseInfo = responseInfo;
            this.precheckErrors = precheckErrors;
        }

        public XmlDocument AltinnDocument
        {
            get
            {
                return altinnDocument;
            }
        }

        public XmlDocument ResponsDocument
        {
            get
            {
                return responsDocument;
            }
        }

        public bool HasPreCheckErrors
        {
            get
            {
                return hasPreCheckErrors;
            }
        }

        public IEnumerable<AltinnResponse> ResponseInfo
        {
            get
            {
                return responseInfo;
            }
        }

        public IEnumerable<AltinnPrecheckError> PrecheckErrors
        {
            get
            {
                return precheckErrors;
            }
        }
    }
}
