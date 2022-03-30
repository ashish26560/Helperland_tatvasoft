using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Helperland.Models
{
    public partial class UserAddress
    {
        public int AddressId { get; set; }
        public int UserId { get; set; }

        [Required(ErrorMessage = "Please enter Street Name ")]
        public string AddressLine1 { get; set; }

        [Required(ErrorMessage = "Please enter House number")]
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }

        [Required(ErrorMessage = "Please enter postalcode")]
        public string PostalCode { get; set; }
        public bool IsDefault { get; set; }
        public bool IsDeleted { get; set; }

        [Required(ErrorMessage = "Please enter Mobile number")]
        [RegularExpression(@"^([0-9]{10})$", ErrorMessage = "Invalid Mobile Number.")]
        public string Mobile { get; set; }
        public string Email { get; set; }

        public virtual User User { get; set; }
    }
}
