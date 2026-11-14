using System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    [Serializable]
    public class SIECompany
    {
        /// <summary>
        /// #BKOD - SNI industry code
        /// </summary>
        public string SNI { get; set; }

        /// <summary>
        /// #FNAMN
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// #FNR
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// #FTYP
        /// </summary>
        public string OrgType { get; set; }

        /// <summary>
        /// #ORGNR - corporate identification number (NNNNNN-NNNN)
        /// </summary>
        public string OrgIdentifier { get; set; }

        /// <summary>
        /// #ORGNR - acquisition number (optional)
        /// </summary>
        public string AcquisitionNumber { get; set; }

        /// <summary>
        /// #ORGNR - activity number (optional)
        /// </summary>
        public string ActivityNumber { get; set; }

        /// <summary>
        /// #ADRESS
        /// </summary>
        public string Contact { get; set; }
        public string Street { get; set; }
        public string ZipCity { get; set; }
        public string Phone { get; set; }
    }
}
