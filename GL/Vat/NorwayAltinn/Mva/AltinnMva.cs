using System;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class melding
    {
        private meldingSkattepliktig skattepliktigField;
        private meldingMeldingsopplysning meldingsopplysningField;
        private meldingTilleggsopplysning tilleggsopplysningField;
        private meldingMvaSumAvgift mvaSumAvgiftField;
        private meldingMvaAvgift mvaAvgiftField;
        private meldingMvaGrunnlag mvaGrunnlagField;
        private string tjenesteTypeField;
        private string dataFormatProviderField;
        private int dataFormatIdField;
        private uint dataFormatVersionField;
        /// <remarks/>
        public meldingSkattepliktig skattepliktig
        {
            get
            {
                return this.skattepliktigField;
            }
            set
            {
                this.skattepliktigField = value;
            }
        }

        /// <remarks/>
        public meldingMeldingsopplysning meldingsopplysning
        {
            get
            {
                return this.meldingsopplysningField;
            }
            set
            {
                this.meldingsopplysningField = value;
            }
        }

        /// <remarks/>
        public meldingTilleggsopplysning tilleggsopplysning
        {
            get
            {
                return this.tilleggsopplysningField;
            }
            set
            {
                this.tilleggsopplysningField = value;
            }
        }

        /// <remarks/>
        public meldingMvaSumAvgift mvaSumAvgift
        {
            get
            {
                return this.mvaSumAvgiftField;
            }
            set
            {
                this.mvaSumAvgiftField = value;
            }
        }

        /// <remarks/>
        public meldingMvaAvgift mvaAvgift
        {
            get
            {
                return this.mvaAvgiftField;
            }
            set
            {
                this.mvaAvgiftField = value;
            }
        }

        /// <remarks/>
        public meldingMvaGrunnlag mvaGrunnlag
        {
            get
            {
                return this.mvaGrunnlagField;
            }
            set
            {
                this.mvaGrunnlagField = value;
            }
        }

        /// <remarks/>
        public string tjenesteType
        {
            get
            {
                return this.tjenesteTypeField;
            }
            set
            {
                this.tjenesteTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string dataFormatProvider
        {
            get
            {
                return this.dataFormatProviderField;
            }
            set
            {
                this.dataFormatProviderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int dataFormatId
        {
            get
            {
                return this.dataFormatIdField;
            }
            set
            {
                this.dataFormatIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint dataFormatVersion
        {
            get
            {
                return this.dataFormatVersionField;
            }
            set
            {
                this.dataFormatVersionField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class meldingSkattepliktig
    {
        private string organisasjonsnummerField;
        private string organisasjonsnavnField;
        private string kontonummerField;
        private string endretKontonummerField;
        private string kIDnummerField;
        private string ibanField;
        private string swiftBicField;
        private string ibanEndretField;
        private string swiftBicEndretField;
        /// <remarks/>
        public string organisasjonsnummer
        {
            get
            {
                return organisasjonsnummerField;
            }
            set
            {
                this.organisasjonsnummerField = value;
            }
        }

        /// <remarks/>
        public string organisasjonsnavn
        {
            get
            {
                return this.organisasjonsnavnField;
            }
            set
            {
                this.organisasjonsnavnField = value;
            }
        }

        /// <remarks/>
        public string kontonummer
        {
            get
            {
                return this.kontonummerField;
            }
            set
            {
                this.kontonummerField = value;
            }
        }

        /// <remarks/>
        public string endretKontonummer
        {
            get
            {
                return this.endretKontonummerField;
            }
            set
            {
                this.endretKontonummerField = value;
            }
        }

        /// <remarks/>
        public string KIDnummer
        {
            get
            {
                return this.kIDnummerField;
            }
            set
            {
                this.kIDnummerField = value;
            }
        }

        /// <remarks/>
        public string iban
        {
            get
            {
                return this.ibanField;
            }
            set
            {
                this.ibanField = value;
            }
        }

        /// <remarks/>
        public string swiftBic
        {
            get
            {
                return this.swiftBicField;
            }
            set
            {
                this.swiftBicField = value;
            }
        }

        /// <remarks/>
        public string ibanEndret
        {
            get
            {
                return this.ibanEndretField;
            }
            set
            {
                this.ibanEndretField = value;
            }
        }

        /// <remarks/>
        public string swiftBicEndret
        {
            get
            {
                return this.swiftBicEndretField;
            }
            set
            {
                this.swiftBicEndretField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class meldingMeldingsopplysning
    {

        private int meldingstypeField;

        private int termintypeField;

        private string terminField;

        private ushort aarField;

        /// <remarks/>
        public int meldingstype
        {
            get
            {
                return this.meldingstypeField;
            }
            set
            {
                this.meldingstypeField = value;
            }
        }

        /// <remarks/>
        public int termintype
        {
            get
            {
                return this.termintypeField;
            }
            set
            {
                this.termintypeField = value;
            }
        }

        /// <remarks/>
        public string termin
        {
            get
            {
                return this.terminField;
            }
            set
            {
                this.terminField = value;
            }
        }

        /// <remarks/>
        public ushort aar
        {
            get
            {
                return this.aarField;
            }
            set
            {
                this.aarField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class meldingTilleggsopplysning
    {

        private string forklaringField;

        private bool forklaringSendtField;

        /// <remarks/>
        public string forklaring
        {
            get
            {
                return this.forklaringField;
            }
            set
            {
                this.forklaringField = value;
            }
        }

        /// <remarks/>
        public bool forklaringSendt
        {
            get
            {
                return this.forklaringSendtField;
            }
            set
            {
                this.forklaringSendtField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class meldingMvaSumAvgift
    {

        private int aaBetaleField;

        private int tilGodeField;

        /// <remarks/>
        public int aaBetale
        {
            get
            {
                return this.aaBetaleField;
            }
            set
            {
                this.aaBetaleField = value;
            }
        }

        /// <remarks/>
        public int tilGode
        {
            get
            {
                return this.tilGodeField;
            }
            set
            {
                this.tilGodeField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class meldingMvaAvgift
    {
        private int innlandOmsetningUttakHoeySatsField;
        private int innlandOmsetningUttakMiddelsSatsField;
        private int innlandOmsetningUttakLavSatsField;
        private int innfoerselVareHoeySatsField;
        private int innfoerselVareMiddelsSatsField;
        private int kjoepUtlandTjenesteHoeySatsField;
        private int kjoepInnlandVareTjenesteHoeySatsField;
        private int fradragInnlandInngaaendeHoeySatsField;
        private int fradragInnlandInngaaendeMiddelsSatsField;
        private int fradragInnlandInngaaendeLavSatsField;
        private int fradragInnfoerselMvaHoeySatsField;
        private int fradragInnfoerselMvaMiddelsSatsField;
        /// <remarks/>
        public int innlandOmsetningUttakHoeySats
        {
            get
            {
                return this.innlandOmsetningUttakHoeySatsField;
            }
            set
            {
                this.innlandOmsetningUttakHoeySatsField = value;
            }
        }

        /// <remarks/>
        public int innlandOmsetningUttakMiddelsSats
        {
            get
            {
                return this.innlandOmsetningUttakMiddelsSatsField;
            }
            set
            {
                this.innlandOmsetningUttakMiddelsSatsField = value;
            }
        }

        /// <remarks/>
        public int innlandOmsetningUttakLavSats
        {
            get
            {
                return this.innlandOmsetningUttakLavSatsField;
            }
            set
            {
                this.innlandOmsetningUttakLavSatsField = value;
            }
        }

        /// <remarks/>
        public int innfoerselVareHoeySats
        {
            get
            {
                return this.innfoerselVareHoeySatsField;
            }
            set
            {
                this.innfoerselVareHoeySatsField = value;
            }
        }

        /// <remarks/>
        public int innfoerselVareMiddelsSats
        {
            get
            {
                return this.innfoerselVareMiddelsSatsField;
            }
            set
            {
                this.innfoerselVareMiddelsSatsField = value;
            }
        }

        /// <remarks/>
        public int kjoepUtlandTjenesteHoeySats
        {
            get
            {
                return this.kjoepUtlandTjenesteHoeySatsField;
            }
            set
            {
                this.kjoepUtlandTjenesteHoeySatsField = value;
            }
        }

        /// <remarks/>
        public int kjoepInnlandVareTjenesteHoeySats
        {
            get
            {
                return this.kjoepInnlandVareTjenesteHoeySatsField;
            }
            set
            {
                this.kjoepInnlandVareTjenesteHoeySatsField = value;
            }
        }

        /// <remarks/>
        public int fradragInnlandInngaaendeHoeySats
        {
            get
            {
                return this.fradragInnlandInngaaendeHoeySatsField;
            }
            set
            {
                this.fradragInnlandInngaaendeHoeySatsField = value;
            }
        }

        /// <remarks/>
        public int fradragInnlandInngaaendeMiddelsSats
        {
            get
            {
                return this.fradragInnlandInngaaendeMiddelsSatsField;
            }
            set
            {
                this.fradragInnlandInngaaendeMiddelsSatsField = value;
            }
        }

        /// <remarks/>
        public int fradragInnlandInngaaendeLavSats
        {
            get
            {
                return this.fradragInnlandInngaaendeLavSatsField;
            }
            set
            {
                this.fradragInnlandInngaaendeLavSatsField = value;
            }
        }

        /// <remarks/>
        public int fradragInnfoerselMvaHoeySats
        {
            get
            {
                return this.fradragInnfoerselMvaHoeySatsField;
            }
            set
            {
                this.fradragInnfoerselMvaHoeySatsField = value;
            }
        }

        /// <remarks/>
        public int fradragInnfoerselMvaMiddelsSats
        {
            get
            {
                return this.fradragInnfoerselMvaMiddelsSatsField;
            }
            set
            {
                this.fradragInnfoerselMvaMiddelsSatsField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class meldingMvaGrunnlag
    {
        private int sumOmsetningUtenforMvaField;
        private int sumOmsetningInnenforMvaUttakOgInnfoerselField;
        private int innlandOmsetningUttakHoeySatsField;
        private int innlandOmsetningUttakMiddelsSatsField;
        private int innlandOmsetningUttakLavSatsField;
        private int innlandOmsetningUttakFritattMvaField;
        private int innlandOmsetningOmvendtAvgiftspliktField;
        private int utfoerselVareTjenesteFritattMvaField;
        private int innfoerselVareHoeySatsField;
        private int innfoerselVareMiddelsSatsField;
        private int innfoerselVareFritattMvaField;
        private int kjoepUtlandTjenesteHoeySatsField;
        private int kjoepInnlandVareTjenesteHoeySatsField;

        /// <remarks/>
        public int sumOmsetningUtenforMva
        {
            get
            {
                return this.sumOmsetningUtenforMvaField;
            }
            set
            {
                this.sumOmsetningUtenforMvaField = value;
            }
        }

        /// <remarks/>
        public int sumOmsetningInnenforMvaUttakOgInnfoersel
        {
            get
            {
                return this.sumOmsetningInnenforMvaUttakOgInnfoerselField;
            }
            set
            {
                this.sumOmsetningInnenforMvaUttakOgInnfoerselField = value;
            }
        }

        /// <remarks/>
        public int innlandOmsetningUttakHoeySats
        {
            get
            {
                return this.innlandOmsetningUttakHoeySatsField;
            }
            set
            {
                this.innlandOmsetningUttakHoeySatsField = value;
            }
        }

        /// <remarks/>
        public int innlandOmsetningUttakMiddelsSats
        {
            get
            {
                return this.innlandOmsetningUttakMiddelsSatsField;
            }
            set
            {
                this.innlandOmsetningUttakMiddelsSatsField = value;
            }
        }

        /// <remarks/>
        public int innlandOmsetningUttakLavSats
        {
            get
            {
                return this.innlandOmsetningUttakLavSatsField;
            }
            set
            {
                this.innlandOmsetningUttakLavSatsField = value;
            }
        }

        /// <remarks/>
        public int innlandOmsetningUttakFritattMva
        {
            get
            {
                return this.innlandOmsetningUttakFritattMvaField;
            }
            set
            {
                this.innlandOmsetningUttakFritattMvaField = value;
            }
        }

        /// <remarks/>
        public int innlandOmsetningOmvendtAvgiftsplikt
        {
            get
            {
                return this.innlandOmsetningOmvendtAvgiftspliktField;
            }
            set
            {
                this.innlandOmsetningOmvendtAvgiftspliktField = value;
            }
        }

        /// <remarks/>
        public int utfoerselVareTjenesteFritattMva
        {
            get
            {
                return this.utfoerselVareTjenesteFritattMvaField;
            }
            set
            {
                this.utfoerselVareTjenesteFritattMvaField = value;
            }
        }

        /// <remarks/>
        public int innfoerselVareHoeySats
        {
            get
            {
                return this.innfoerselVareHoeySatsField;
            }
            set
            {
                this.innfoerselVareHoeySatsField = value;
            }
        }

        /// <remarks/>
        public int innfoerselVareMiddelsSats
        {
            get
            {
                return this.innfoerselVareMiddelsSatsField;
            }
            set
            {
                this.innfoerselVareMiddelsSatsField = value;
            }
        }

        /// <remarks/>
        public int innfoerselVareFritattMva
        {
            get
            {
                return this.innfoerselVareFritattMvaField;
            }
            set
            {
                this.innfoerselVareFritattMvaField = value;
            }
        }

        /// <remarks/>
        public int kjoepUtlandTjenesteHoeySats
        {
            get
            {
                return this.kjoepUtlandTjenesteHoeySatsField;
            }
            set
            {
                this.kjoepUtlandTjenesteHoeySatsField = value;
            }
        }

        /// <remarks/>
        public int kjoepInnlandVareTjenesteHoeySats
        {
            get
            {
                return this.kjoepInnlandVareTjenesteHoeySatsField;
            }
            set
            {
                this.kjoepInnlandVareTjenesteHoeySatsField = value;
            }
        }
    }

}
