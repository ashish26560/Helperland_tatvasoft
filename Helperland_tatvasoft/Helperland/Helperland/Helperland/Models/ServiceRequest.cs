using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

#nullable disable

namespace Helperland.Models
{
    public partial class ServiceRequest
    {
        public ServiceRequest()
        {
            Ratings = new HashSet<Rating>();
            ServiceRequestAddresses = new HashSet<ServiceRequestAddress>();
            ServiceRequestExtras = new HashSet<ServiceRequestExtra>();
        }

        public int ServiceRequestId { get; set; }
        public int UserId { get; set; }
        public int ServiceId { get; set; }


        [NotMapped]
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime StartDate { get; set; }

        [NotMapped]
        [DataType(DataType.Time)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:HH:mm}")]
        public DateTime StartTime { get; set; }

        [NotMapped]
        public string ServiceAddress { get; set; }

        [NotMapped]
        public string Mobile { get; set; }

        [NotMapped]
        public string Email { get; set; }
        [NotMapped]
        public string ServiceProviderName { get; set; }
        [NotMapped]
        public string CustomerName { get; set; }
        [NotMapped]
        public decimal? ratings { get; set; }
        [NotMapped]
        public string UserProfilePicture { get; set; }
        public DateTime ServiceStartDate { get; set; }
        public string ZipCode { get; set; }
        public decimal? ServiceHourlyRate { get; set; }

        public double ServiceHours { get; set; }

        public double? ExtraHours { get; set; }
        public decimal SubTotal { get; set; }
        public decimal? Discount { get; set; }
        public decimal TotalCost { get; set; }

        public string Comments { get; set; }
        public string PaymentTransactionRefNo { get; set; }
        public bool PaymentDue { get; set; }
        public int? ServiceProviderId { get; set; }
        public DateTime? SpacceptedDate { get; set; }
        public bool HasPets { get; set; }
        public int? Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public decimal? RefundedAmount { get; set; }
        public decimal Distance { get; set; }
        public bool? HasIssue { get; set; }
        public bool? PaymentDone { get; set; }
        public Guid? RecordVersion { get; set; }

        [NotMapped]
        public bool ExtraService1 { get; set; }
        [NotMapped]
        public bool ExtraService2 { get; set; }
        [NotMapped]
        public bool ExtraService3 { get; set; }
        [NotMapped]
        public bool ExtraService4 { get; set; }
        [NotMapped]
        public bool ExtraService5 { get; set; }
        [NotMapped]
        public int Extra { get; set; }

        public virtual User ServiceProvider { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
        public virtual ICollection<ServiceRequestAddress> ServiceRequestAddresses { get; set; }
        public virtual ICollection<ServiceRequestExtra> ServiceRequestExtras { get; set; }
    }
}
