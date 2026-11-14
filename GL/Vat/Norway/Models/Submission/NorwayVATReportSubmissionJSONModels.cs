using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.Norway.Models.Submission
{
    public class InstanceCreationRequest
    {
        public InstanceCreationRequestInstanceOwner InstanceOwner { get; set; }
    }

    public class InstanceCreationRequestInstanceOwner
    {
        public string OrganisationNumber { get; set; }
    }

    public class InstanceCreationResponse
    {
        public string Id { get; set; }
        public InstanceCreationResponseInstanceOwner InstanceOwner { get; set; }
        public string AppId { get; set; }
        public string Org { get; set; }
        public InstanceCreationResponseSelfLinks SelfLinks { get; set; }
        public DateTime? DueBefore { get; set; }
        public DateTime? VisibleAfter { get; set; }
        public InstanceCreationResponseProcess Process { get; set; }
        public InstanceCreationResponseStatus Status { get; set; }
        public InstanceCreationResponseCompleteConfirmations[] CompleteConfirmations { get; set; }
        public InstanceCreationResponseData[] Data { get; set; }
        public string PresentationTexts { get; set; }
        public InstanceCreationResponseDataValues DataValues { get; set; }
        public DateTime? Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastChanged { get; set; }
        public string LastChangedBy { get; set; }
    }

    public class InstanceCreationResponseInstanceOwner
    {
        public string PartyId { get; set; }
        public string PersonNumber { get; set; }
        public string OrganisationNumber { get; set; }
        public string Username { get; set; }
    }

    public class InstanceCreationResponseSelfLinks
    {
        public string Apps { get; set; }
        public string Platform { get; set; }
    }

    public class InstanceCreationResponseProcess
    {
        public DateTime? Started { get; set; }
        public string StartEvent { get; set; }
        public InstanceCreationResponseCurrentTask CurrentTask { get; set; }
        public DateTime? Ended { get; set; }
        public string EndEvent { get; set; }
    }

    public class InstanceCreationResponseCurrentTask
    {
        public int Flow { get; set; }
        public DateTime? Started { get; set; }
        public string ElementId { get; set; }
        public string Name { get; set; }
        public string AltinnTaskType { get; set; }
        public DateTime? Ended { get; set; }
        public DateTime? Validated { get; set; }
        public string FlowType { get; set; }
    }

    public class InstanceCreationResponseStatus
    {
        public bool IsArchived { get; set; }
        public DateTime? Archived { get; set; }
        public bool IsSoftDeleted { get; set; }
        public DateTime? SoftDeleted { get; set; }
        public bool IsHardDeleted { get; set; }
        public DateTime? HardDeleted { get; set; }
        public int ReadStatus { get; set; }
        public InstanceCreationResponseSubstatus Substatus { get; set; }
    }

    public class InstanceCreationResponseSubstatus
    {
        public string Label { get; set; }
        public string Description { get; set; }
    }

    public class InstanceCreationResponseCompleteConfirmations
    {
        public string StakeholderId { get; set; }
        public DateTime? ConfirmedOn { get; set; }
    }

    public class InstanceCreationResponseData
    {
        public string Id { get; set; }
        public string InstanceGuid { get; set; }
        public string DataType { get; set; }
        public string Filename { get; set; }
        public string ContentType { get; set; }
        public string BlobStoragePath { get; set; }
        public InstanceCreationResponseSelfLinks SelfLinks { get; set; }
        public int Size { get; set; }
        public bool Locked { get; set; }
        public string Refs { get; set; }
        public bool IsRead { get; set; }
        public string DeleteStatus { get; set; }
        public DateTime? Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastChanged { get; set; }
        public string LastChangedBy { get; set; }
    }

    public class InstanceCreationResponseDataValues
    {
        [JsonProperty("dialog.id")]
        public string DialogId { get; set; }
    }
}
