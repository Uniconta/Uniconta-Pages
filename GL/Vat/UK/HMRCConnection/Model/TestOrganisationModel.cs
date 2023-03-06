using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection.Model
{
    class TestOrganisationModel
    {
        public string UserId { get; set; }
        public string Password { get; set; }
        public string UserFullName { get; set; }
        public string EmailAddress { get; set; }
        public TestOrganisationDetailsModel OrganisationDetails { get; set; }
        public string MtdVrn { get; set; }
        public string Vrn { get; set; }
    }

    class TestOrganisationDetailsModel
    {
        public string Name { get; set; }
        public TestOrganisationDetailsAddressModel Address { get; set; }
    }

    class TestOrganisationDetailsAddressModel
    {
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Postcode { get; set; }
    }
}
