using System;
using System.ServiceModel;
using System.Xml.Serialization;

namespace RSK.Verktakamidi
{
    public class ContrClient : IDisposable
    {
        public GagnaskilServiceClient Client { get; set; }
        //private const string EndpointUri = "https://vefurp.rsk.is/ws/Gagnaskil/GagnaskilService.svc";       // test
        private const string EndpointUri = "https://vefur.rsk.is/ws/Gagnaskil/GagnaskilService.svc";

        public ContrClient()
        {
            var binding = new BasicHttpsBinding(BasicHttpsSecurityMode.Transport);
            Client = new GagnaskilServiceClient(binding, new EndpointAddress(EndpointUri));
        }

        public void Dispose()
        {
            Client.Close();
            ((IDisposable)Client)?.Dispose();
        }
    }

    [SerializableAttribute()]
    [XmlTypeAttribute(AnonymousType = true)]
    [XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Verktakagreidsluskra
    {
        private string kennitalaLaunagreidandaField;
        private string tekjuarField;
        private string veflykillField;
        private string notandiField;
        private TegTegund tegundField;
        private TegAdgerd adgerdField;
        private long bunkanumerField;
        private bool bunkanumerFieldSpecified;
        private TegString_J_N tilForskraningarField;
        private string forritUtgafaField;
        private Midi[] midiField;
        private Samtalningsblad samtalningsbladField;

        public string KennitalaLaunagreidanda
        {
            get { return this.kennitalaLaunagreidandaField; }
            set { this.kennitalaLaunagreidandaField = value; }
        }

        public string Tekjuar
        {
            get { return this.tekjuarField; }
            set { this.tekjuarField = value; }
        }

        public string Veflykill
        {
            get { return this.veflykillField; }
            set { this.veflykillField = value; }
        }

        public string Notandi
        {
            get { return this.notandiField; }
            set { this.notandiField = value; }
        }

        public TegTegund Tegund
        {
            get { return this.tegundField; }
            set { this.tegundField = value; }
        }

        public TegAdgerd Adgerd
        {
            get { return this.adgerdField; }
            set { this.adgerdField = value; }
        }

        public long Bunkanumer
        {
            get { return this.bunkanumerField; }
            set { this.bunkanumerField = value; }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool BunkanumerSpecified
        {
            get
            {
                return this.bunkanumerFieldSpecified;
            }
            set
            {
                this.bunkanumerFieldSpecified = value;
            }
        }

        public TegString_J_N TilForskraningar
        {
            get { return this.tilForskraningarField; }
            set { this.tilForskraningarField = value; }
        }

        public string ForritUtgafa
        {
            get { return this.forritUtgafaField; }
            set { this.forritUtgafaField = value; }
        }

        [System.Xml.Serialization.XmlElementAttribute("Midi")]
        public Midi[] Midi
        {
            get { return this.midiField; }
            set { this.midiField = value; }
        }

        public Samtalningsblad Samtalningsblad
        {
            get { return this.samtalningsbladField; }
            set { this.samtalningsbladField = value; }
        }
    }

    [SerializableAttribute()]
    [XmlTypeAttribute(AnonymousType = true)]
    [XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Midi
    {
        private string kennitalaField;
        private long verktakagreidslaField;
        private bool verktakagreidslaFieldSpecified;
        private ErlendurAdili erlendurAdiliField;
        private long idField;

        public string Kennitala
        {
            get { return this.kennitalaField; }
            set { this.kennitalaField = value; }
        }

        public long Verktakagreidsla
        {
            get { return this.verktakagreidslaField; }
            set { this.verktakagreidslaField = value; }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool VerktakagreidslaSpecified
        {
            get { return this.verktakagreidslaFieldSpecified; }
            set { this.verktakagreidslaFieldSpecified = value; }
        }

        public ErlendurAdili ErlendurAdili
        {
            get { return this.erlendurAdiliField; }
            set { this.erlendurAdiliField = value; }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public long id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }
    }

    [SerializableAttribute()]
    [XmlTypeAttribute(AnonymousType = true)]
    [XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class ErlendurAdili
    {
        private string nafnField;
        private string tinField;
        private string gataField;
        private string borgField;
        private string fylkiField;
        private TegLandKodi landField;

        public string Nafn
        {
            get { return this.nafnField; }
            set { this.nafnField = value; }
        }

        public string Tin
        {
            get { return this.tinField; }
            set { this.tinField = value; }
        }

        public string Gata
        {
            get { return this.gataField; }
            set { this.gataField = value; }
        }

        public string Borg
        {
            get { return this.borgField; }
            set { this.borgField = value; }
        }

        public string Fylki
        {
            get { return this.fylkiField; }
            set { this.fylkiField = value; }
        }

        public TegLandKodi Land
        {
            get { return this.landField; }
            set { this.landField = value; }
        }
    }

    [SerializableAttribute()]
    public enum TegLandKodi
    {       
        AF,
        AX,
        AL,
        DZ,
        AS,
        AD,
        AO,
        AI,
        AQ,
        AG,
        AR,
        AM,
        AW,
        AU,
        AT,
        AZ,
        BS,
        BH,
        BD,
        BB,
        BY,
        BE,
        BZ,
        BJ,
        BM,
        BT,
        BO,
        BQ,
        BA,
        BW,
        BV,
        BR,
        IO,
        BN,
        BG,
        BF,
        BI,
        KH,
        CM,
        CA,
        CV,
        KY,
        CF,
        TD,
        CL,
        CN,
        CX,
        CC,
        CO,
        KM,
        CG,
        CD,
        CK,
        CR,
        CI,
        HR,
        CU,
        CW,
        CY,
        CZ,
        DK,
        DJ,
        DM,
        DO,
        EC,
        EG,
        SV,
        GQ,
        ER,
        EE,
        ET,
        FK,
        FO,
        FJ,
        FI,
        FR,
        GF,
        PF,
        TF,
        GA,
        GM,
        GE,
        DE,
        GH,
        GI,
        GR,
        GL,
        GD,
        GP,
        GU,
        GT,
        GG,
        GN,
        GW,
        GY,
        HT,
        HM,
        VA,
        HN,
        HK,
        HU,
        IS,
        IN,
        ID,
        IR,
        IQ,
        IE,
        IM,
        IL,
        IT,
        JM,
        JP,
        JE,
        JO,
        KZ,
        KE,
        KI,
        KP,
        KR,
        KW,
        KG,
        LA,
        LV,
        LB,
        LS,
        LR,
        LY,
        LI,
        LT,
        LU,
        MO,
        MK,
        MG,
        MW,
        MY,
        MV,
        ML,
        MT,
        MH,
        MQ,
        MR,
        MU,
        YT,
        MX,
        FM,
        MD,
        MC,
        MN,
        ME,
        MS,
        MA,
        MZ,
        MM,
        NA,
        NR,
        NP,
        NL,
        NC,
        NZ,
        NI,
        NE,
        NG,
        NU,
        NF,
        MP,
        NO,
        OM,
        PK,
        PW,
        PS,
        PA,
        PG,
        PY,
        PE,
        PH,
        PN,
        PL,
        PT,
        PR,
        QA,
        RE,
        RO,
        RU,
        RW,
        BL,
        SH,
        KN,
        LC,
        MF,
        PM,
        VC,
        WS,
        SM,
        ST,
        SA,
        SN,
        RS,
        SC,
        SL,
        SG,
        SX,
        SK,
        SI,
        SB,
        SO,
        ZA,
        GS,
        SS,
        ES,
        LK,
        SD,
        SR,
        SJ,
        SZ,
        SE,
        CH,
        SY,
        TW,
        TJ,
        TZ,
        TH,
        TL,
        TG,
        TK,
        TO,
        TT,
        TN,
        TR,
        TM,
        TC,
        TV,
        UG,
        UA,
        AE,
        GB,
        US,
        UM,
        UY,
        UZ,
        VU,
        VE,
        VN,
        VG,
        VI,
        WF,
        EH,
        YE,
        ZM,
        ZW,
    }

    [SerializableAttribute()]
    [XmlRootAttribute("Tegund", Namespace = "", IsNullable = false)]
    public enum TegTegund
    {
        Vefur,
        RSK,
        Beint,
    }

    [SerializableAttribute()]
    [XmlRootAttribute("Adgerd", Namespace = "", IsNullable = false)]
    public enum TegAdgerd
    {
        Nyskra,
        Breyta,
        Eyda,
    }

    [SerializableAttribute()]
    [XmlRootAttribute("TilForskraningar", Namespace = "", IsNullable = false)]
    public enum TegString_J_N
    {
        J,
        N,
    }

    [SerializableAttribute()]
    [XmlTypeAttribute(AnonymousType = true)]
    [XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Samtalningsblad
    {
        private long fjoldiLaunthegaField;
        private long fjoldiMidaField;
        private long verktakagreidslaField;
        private bool verktakagreidslaFieldSpecified;

        public long FjoldiLaunthega
        {
            get { return this.fjoldiLaunthegaField; }
            set { this.fjoldiLaunthegaField = value; }
        }

        public long FjoldiMida
        {
            get { return this.fjoldiMidaField; }
            set { this.fjoldiMidaField = value; }
        }

        public long Verktakagreidsla
        {
            get { return this.verktakagreidslaField; }
            set { this.verktakagreidslaField = value; }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool VerktakagreidslaSpecified
        {
            get { return this.verktakagreidslaFieldSpecified; }
            set { this.verktakagreidslaFieldSpecified = value; }
        }
    }
}