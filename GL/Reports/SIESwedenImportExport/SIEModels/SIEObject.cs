using System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    [Serializable]
    public class SIEObject
    {
        public SIEDimension Dimension { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// True when the object was declared with #OBJEKT, false when it was only referenced in an object list.
        /// </summary>
        public bool IsDeclared { get; set; } = true;
    }
}
