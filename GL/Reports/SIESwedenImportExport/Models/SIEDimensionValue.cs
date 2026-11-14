using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.Models
{
    /// <summary>
    /// Dimension value (SIE #OBJEKT) as shown on the import page. DimType is the Uniconta dimension (1..5)
    /// the SIE dimension is mapped to. New values are inserted as GLDimType1Client..GLDimType5Client.
    /// </summary>
    public class SIEDimensionValue
    {
        [Display(Name = nameof(Dimension), ResourceType = typeof(SIEDimensionValueText))]
        public string Dimension { get; set; }

        [Display(Name = nameof(Name), ResourceType = typeof(SIEDimensionValueText))]
        public string Name { get; set; }

        [Display(Name = nameof(DimType), ResourceType = typeof(SIEDimensionValueText))]
        public string DimType { get; set; }

        [Display(Name = nameof(IsNew), ResourceType = typeof(SIEDimensionValueText))]
        public bool IsNew { get; set; }
    }

    public static class SIEDimensionValueText
    {
        public static string Dimension { get { return Localization.lookup("Key"); } }
        public static string Name { get { return Localization.lookup("Name"); } }
        public static string DimType { get { return Localization.lookup("Type"); } }
        public static string IsNew { get { return Localization.lookup("New"); } }
    }
}
