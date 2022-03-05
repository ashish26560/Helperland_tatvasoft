using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Helperland.Models
{
    public partial class User
    {
        public User()
        {
            FavoriteAndBlockedTargetUsers = new HashSet<FavoriteAndBlocked>();
            FavoriteAndBlockedUsers = new HashSet<FavoriteAndBlocked>();
            RatingRatingFromNavigations = new HashSet<Rating>();
            RatingRatingToNavigations = new HashSet<Rating>();
            ServiceRequestServiceProviders = new HashSet<ServiceRequest>();
            ServiceRequestUsers = new HashSet<ServiceRequest>();
            UserAddresses = new HashSet<UserAddress>();
        }

        public int UserId { get; set; }

        [Required(ErrorMessage = "Please enter First name")]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Please enter Last name")]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Please enter email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "New password required", AllowEmptyStrings = false)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [NotMapped]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirm password does not match")]
        public string ConfirmPassword { get; set; }
        //[Required(ErrorMessage = "Please enter confirm password")]
        //[Display(Name = "Confirm Password")]
        //[Compare("Password", ErrorMessage = "Password and confirm password does not match")]
        //public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Please enter Mobile number")]
        public string Mobile { get; set; }
        public int UserTypeId { get; set; }
        public int? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [NotMapped]
        public string Day { get; set; }
        [NotMapped]
        public string Month { get; set; }
        [NotMapped]
        public string Year { get; set; }
        public string UserProfilePicture { get; set; }
        public bool IsRegisteredUser { get; set; }
        public string PaymentGatewayUserRef { get; set; }
        public string ZipCode { get; set; }
        public bool WorksWithPets { get; set; }
        public int? LanguageId { get; set; }
        public int? NationalityId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int? Status { get; set; }
        public string BankTokenId { get; set; }
        public string TaxNo { get; set; }

        public string ResetPasswordCode { get; set; }

        public virtual ICollection<FavoriteAndBlocked> FavoriteAndBlockedTargetUsers { get; set; }
        public virtual ICollection<FavoriteAndBlocked> FavoriteAndBlockedUsers { get; set; }
        public virtual ICollection<Rating> RatingRatingFromNavigations { get; set; }
        public virtual ICollection<Rating> RatingRatingToNavigations { get; set; }
        public virtual ICollection<ServiceRequest> ServiceRequestServiceProviders { get; set; }
        public virtual ICollection<ServiceRequest> ServiceRequestUsers { get; set; }
        public virtual ICollection<UserAddress> UserAddresses { get; set; }
        
    }
}
