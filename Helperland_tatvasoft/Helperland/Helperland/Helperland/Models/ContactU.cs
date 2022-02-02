using System;
using System.ComponentModel.DataAnnotations;
#nullable disable

namespace Helperland.Models
{
    public partial class ContactU
    {
        //[Key]
        public int ContactUsId { get; set; }

        [Required(ErrorMessage = "Please enter name")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter email address")]
        [Display(Name = "Email Address")]
        //[EmailAddress]
        public string Email { get; set; }


        public string Subject { get; set; }

        [Required(ErrorMessage = "Please enter phone number")]
        [Display(Name = "Phone Number")]
        [Phone]
        public string PhoneNumber { get; set; }


        public string Message { get; set; }
        public string UploadFileName { get; set; }

        [DataType(DataType.Time)]
        public DateTime? CreatedOn { get; set;
           
        }
        public int? CreatedBy { get; set; }
        public string FileName { get; set; }
    }
}
