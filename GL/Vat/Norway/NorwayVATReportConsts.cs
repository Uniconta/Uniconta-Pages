using System.Collections.Generic;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.Norway
{
    public static class NorwayVATReportConsts
    {
        //Note on client ids. It is registered and owned by PITSupport with Per- O. Andorsen at per@pitsupport.no

        //Authentication endpoints and consts
        public const string ProdClientId = "aacee743-e8ba-4bf7-b4d1-4f480e0ad920";
        public const string ProdAuthority = "https://idporten.no";

        //Norway public one: 23cc2587-ea4e-4a5f-aa5c-dfce3d6c5f09
        public const string TestClientId = "21ae3792-4d3f-4f8a-bfaa-c73270a6e50a";
        public const string TestAuthority = "https://test.idporten.no";

        //Must match the redirect URI registered on both ID-porten clients (test and production) exactly
        public const string RedirectUrl = "http://localhost:12345/token";

        //File names of the VAT return and its envelope. The envelope's vedlegg names the same file as the upload's Content-Disposition
        public const string VatReturnFileName = "mvaMelding.xml";
        public const string EnvelopeFileName = "mvaMeldingInnsending.xml";

        //Same scopes as the PIT Support plugin, whose submissions are accepted with the same clients. Without
        //skatteetaten:mvameldinginnsending, Send failed with 401 in TEST although validation and the Altinn exchange worked
        //(a March 2026 notice suggested the Altinn scopes replace it). Both ID-porten clients must be granted all of these scopes.
        public const string Scope = "openid skatteetaten:mvameldingvalidering skatteetaten:mvameldinginnsending altinn:instances.read altinn:instances.write";

        //Service endpoints
        public const string ProdValidationEndPoint = "https://idporten.api.skatteetaten.no/api/mva/grensesnittstoette/mva-melding/valider";
        public const string ProdAltinnTokenExchangeEndPoint = "https://platform.altinn.no/authentication/api/v1/exchange/id-porten";
        public const string ProdAltinnInstancesBaseEndPoint = "https://skd.apps.altinn.no/skd/mva-melding-innsending-v1/instances";

        public const string TestValidationEndPoint = "https://idporten-api-test.sits.no/api/mva/grensesnittstoette/mva-melding/valider";
        public const string TestAltinnTokenExchangeEndPoint = "https://platform.tt02.altinn.no/authentication/api/v1/exchange/id-porten";
        public const string TestAltinnInstancesBaseEndPoint = "https://skd.apps.tt02.altinn.no/skd/mva-melding-innsending-test/instances";

        //Stored references of submissions made in Skatteetaten's test environment start with this prefix, so Feedback and the
        //duplicate check always use the environment the instance was created in. Production references have no prefix
        public const string TestReferencePrefix = "test:";

        //Enums. The keys are the values sent to Skatteetaten, the texts are labels, read on each access so they follow the user's language
        //kompensasjon needs basis and rate on every line (rule R093) and eHandel needs a VOEC number and is excluded by rule R104.
        //Re-add them only together with category specific line mapping.
        public static Dictionary<string, string> DeclarationTypeList => new Dictionary<string, string>()
        {
            { Models.Validation.Meldingskategori.alminnelig.ToString(), Uniconta.ClientTools.Localization.lookup("NorwayVATCategoryAlminnelig") },
            { Models.Validation.Meldingskategori.primaernaering.ToString(), Uniconta.ClientTools.Localization.lookup("NorwayVATCategoryPrimaernaering") },
            { Models.Validation.Meldingskategori.omvendtAvgiftsplikt.ToString(), Uniconta.ClientTools.Localization.lookup("NorwayVATCategoryOmvendtAvgiftsplikt") }
        };

        //SAF-T codes that must never be reported in the VAT return (Skatteetaten information model)
        public static readonly HashSet<string> NotReportedStdCodes = new HashSet<string>() { "0", "7", "20", "21", "22" };

        public static Dictionary<string, string> SubmissionTypeList => new Dictionary<string, string>()
        {
            { Models.Submission.Innsendingstype.komplett.ToString(), Uniconta.ClientTools.Localization.lookup("NorwayVATSubmissionComplete") },
            { Models.Submission.Innsendingstype.ikkekomplett.ToString(), Uniconta.ClientTools.Localization.lookup("NorwayVATSubmissionIncomplete") }
        };

        public static Dictionary<string, string> SpecificationsList => new Dictionary<string, string>()
        {
            { "", "" },
            { "justering", Uniconta.ClientTools.Localization.lookup("NorwayVATSpecJustering") },
            { "tapPåKrav", Uniconta.ClientTools.Localization.lookup("NorwayVATSpecTapPaaKrav") },
            { "tilbakeføringAvInngåendeMerverdiavgift", Uniconta.ClientTools.Localization.lookup("NorwayVATSpecTilbakefoering") },
            { "uttak", Uniconta.ClientTools.Localization.lookup("NorwayVATSpecUttak") },
            { "justeringMerverdiavgiftskompensasjonFastEiendom", Uniconta.ClientTools.Localization.lookup("NorwayVATSpecJusteringKompensasjon") },
            { "kompensasjonsrett", Uniconta.ClientTools.Localization.lookup("NorwayVATSpecKompensasjonsrett") }
        };

        public static Dictionary<string, string> CommentsOptionsList => new Dictionary<string, string>()
        {
            { "", "" },
            { Models.Validation.ItemChoiceType.utvalgtMerknad.ToString(), Uniconta.ClientTools.Localization.lookup("NorwayVATPredefinedComment") },
            { Models.Validation.ItemChoiceType.beskrivelse.ToString(), Uniconta.ClientTools.Localization.lookup("Description") }
        };

        //Not labels: these are the predefined remark values in Skatteetaten's code list and are sent as they are
        public static List<string> PredefinedVATReportCommentsList = new List<string>()
        {
            "BFU",
            "virksomhet under avvikling",
            "virksomhet har endret selskapsform",
            "omsetningssvikt pga ekstraordinære forhold",
            "forlis",
            "endring av bruksområde på bygg, anlegg eller annen fast eiendom",
            "kompensasjonskravet gjelder flere terminer",
            "korrigering av tidligere kompensasjonskrav",
            "virksomheten er fusjonert eller fisjonert",
            "forholdsmessig kompensasjon",
            "virksomhet under avvikling kompensasjon",
        };

        //Line remarks are not a fixed list: NorwayVATRemarkCatalog offers the remarks Skatteetaten accepts for each line's code, side and sign
    }
}
