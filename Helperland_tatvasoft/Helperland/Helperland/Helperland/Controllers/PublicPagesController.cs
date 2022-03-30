using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Helperland.Models;
using Helperland.Data;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Helperland.Controllers
{
    public class PublicPagesController : Controller
    {
        public readonly HelperlandContext _context;

        public PublicPagesController(HelperlandContext context)
        {
            _context = context;
        }

        public IActionResult About()
        {
            return View();
        }
        public IActionResult Price()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
        public IActionResult Book_now()
        {
            return View();
        }


        public IActionResult CheckPostalCode(User user)
        {
            var p = _context.Users.Where(c => c.ZipCode == user.ZipCode && c.UserTypeId == 2).FirstOrDefault();
            if (p != null)
            {
                HttpContext.Session.SetString("ZipCode", p.ZipCode);

                return View("Book_now");
            }
            else
            {
                //TempData["PostalCodeMessage"] = "Postal code you have entered is not valid.";

                return StatusCode(404);

                //return new HttpStatusCodeResult(401, "Custom Error Message 1");

            }

        }
        public IActionResult LoadYourDetails()
        {
            var p = HttpContext.Session.GetString("ZipCode");
            var zipcode = _context.Zipcodes.Where(c => c.ZipcodeValue == p).FirstOrDefault();
            var city = _context.Cities.Where(c => c.Id == zipcode.CityId).FirstOrDefault();
            ViewBag.City = city.CityName;
            ViewBag.zipcode = p;
            return PartialView("_YourDetails");

        }
        [HttpPost]
        public async Task<IActionResult> YourDetails(UserAddress userAddress)
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var p = _context.Users.Where(c => c.UserId == userid).FirstOrDefault();

            userAddress.UserId = userid;
            userAddress.IsDefault = false;
            userAddress.IsDeleted = false;
            userAddress.Email = p.Email;
            userAddress.PostalCode = HttpContext.Session.GetString("ZipCode");

            await _context.UserAddresses.AddAsync(userAddress);
            await _context.SaveChangesAsync();
            return View("Book_now");
        }

        public IActionResult UserAddress()
        {
            int userid = (int)HttpContext.Session.GetInt32("UserId");
            var zipcode = HttpContext.Session.GetString("ZipCode");
            var p = _context.UserAddresses.Where(c => c.UserId == userid && c.PostalCode == zipcode).ToList();

            return PartialView("UserAddress", p);
        }
        public IActionResult Booksuccess()
        {
            ViewBag.servicereqid = HttpContext.Session.GetInt32("ServiceRequestId");
            return PartialView("_BookingSuccess");
        }
        public async Task<IActionResult> LoadFavouritePro()
        {

            int userid = (int)HttpContext.Session.GetInt32("UserId");

            var favouriteproviders = await _context.FavoriteAndBlockeds.Where
                (c => c.UserId == userid && c.IsFavorite == true).ToListAsync();
            List<User> users = new List<User>();

            foreach (FavoriteAndBlocked fav in favouriteproviders)
            {
                var user = await _context.Users.Where(c => c.UserId == fav.TargetUserId).FirstOrDefaultAsync();
                users.Add(user);
            }

            return PartialView("_SelectFavouritePro", users);
        }
        public async Task<IActionResult> CompleteBooking(ServiceRequest serviceRequest,
            ServiceRequestAddress serviceRequestAddress,
            ServiceRequestExtra serviceRequestExtra)
        {
            var zipcode = HttpContext.Session.GetString("ZipCode");
            var Name = HttpContext.Session.GetString("Name");

            var data = HttpContext.Session.GetString("schedule");
            serviceRequest = JsonConvert.DeserializeObject<ServiceRequest>(data);

            var extraid = serviceRequest.Extra;
            serviceRequest.ExtraHours = serviceRequest.ExtraHours;
            serviceRequest.ServiceHourlyRate = 25;
            var totalhours = (decimal)(serviceRequest.ExtraHours + serviceRequest.ServiceHours);
            serviceRequest.SubTotal = (decimal)(totalhours * serviceRequest.ServiceHourlyRate);
            serviceRequest.TotalCost = (decimal)(totalhours * serviceRequest.ServiceHourlyRate);
            serviceRequest.PaymentDone = true;
            serviceRequest.PaymentDue = false;
            serviceRequest.Status = 1;
            serviceRequest.ServiceProviderId = HttpContext.Session.GetInt32("favid");
            await _context.ServiceRequests.AddAsync(serviceRequest);
            await _context.SaveChangesAsync();

            var servicereqid = serviceRequest.ServiceRequestId;

            HttpContext.Session.SetInt32("ServiceRequestId", serviceRequest.ServiceRequestId);

            HttpContext.Session.Remove("schedule");

            data = HttpContext.Session.GetString("serviceaddress");
            serviceRequestAddress = JsonConvert.DeserializeObject<ServiceRequestAddress>(data);
            serviceRequestAddress.ServiceRequestId = servicereqid;

            //serviceRequestExtra.ServiceRequestId = (int)HttpContext.Session.GetInt32("ServiceRequestId");
            await _context.ServiceRequestAddresses.AddAsync(serviceRequestAddress);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("serviceaddress");

            serviceRequestExtra.ServiceRequestId = servicereqid;
            serviceRequestExtra.ServiceExtraId = extraid;

            await _context.ServiceRequestExtras.AddAsync(serviceRequestExtra);
            await _context.SaveChangesAsync();
            //Select(c => c.Email)
            if (serviceRequest.ServiceProviderId == null)
            {
                var serviceproviderlist = _context.Users.Where(c => c.ZipCode == zipcode && c.UserTypeId == 2).ToList();
                foreach (User mail in serviceproviderlist)
                {
                    string body = "Hi " + mail.FirstName + ", <br/> we have got new service request in your area." +
                        " <br/><br/>Customer Name :" + Name + "<br/>Service Request ID :" + servicereqid +
                        "<br/><br/>You can accept it by loging in.<br/><br/> Thank you";

                    string subject = "We have got new ServiceRequest in your area.";
                    SendEmail(mail.Email, body, subject);
                }
            }
            else
            {
                var spuser = await _context.Users.Where(c => c.UserId == serviceRequest.ServiceProviderId).FirstOrDefaultAsync();
                string body = "Hi " + spuser.FirstName + ", <br/> A service Request is directly assigned to you." +
    " <br/><br/>Customer Name :" + Name + "<br/>Service Request ID :" + servicereqid +
    ".<br/><br/> Thank you";

                string subject = "We have got new ServiceRequest in your area.";
                SendEmail(spuser.Email, body, subject);
            }
            return PartialView("_BookingSuccess", ViewBag.servicereqid);
        }
        private void SendEmail(string emailAddress, string body, string subject)
        {
            using MailMessage mm = new MailMessage("ashish.chauhan93133@gmail.com", emailAddress);
            mm.Subject = subject;
            mm.Body = body;

            mm.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                EnableSsl = true
            };
            NetworkCredential NetworkCred = new NetworkCredential("ashish.chauhan93133@gmail.com", "FeelFree@2389");
            smtp.UseDefaultCredentials = true;
            smtp.Credentials = NetworkCred;
            smtp.Port = 587;
            smtp.Send(mm);
        }
        [HttpPost]
        public IActionResult SendAddress(int radioValue, int favid, ServiceRequestAddress serviceRequestAddress)
        {
            //if (radioValue is null)
            //{
            //    throw new ArgumentNullException(nameof(radioValue));
            //}

            var p = _context.UserAddresses.Where(c => c.AddressId == radioValue).FirstOrDefault();

            serviceRequestAddress.AddressLine1 = p.AddressLine1;
            serviceRequestAddress.AddressLine2 = p.AddressLine2;
            serviceRequestAddress.City = p.City;
            serviceRequestAddress.Email = p.Email;
            serviceRequestAddress.Mobile = p.Mobile;
            serviceRequestAddress.PostalCode = p.PostalCode;


            HttpContext.Session.SetInt32("favid", favid);

            HttpContext.Session.SetString("serviceaddress", JsonConvert.SerializeObject(serviceRequestAddress));
            //await _context.ServiceRequestAddresses.AddAsync(serviceRequestAddress);
            //await _context.SaveChangesAsync();
            return View("Book_now");
        }

        [HttpPost]
        public IActionResult RequestService(ServiceRequest booking)
        {
            char[] array = { '0', '0', '0', '0', '0' };

            booking.ExtraHours = 0;
            if (booking.ExtraService1)
            {
                booking.ExtraHours += 0.5;
                array[0] = '1';
            }
            if (booking.ExtraService2)
            {

                booking.ExtraHours += 0.5;
                array[1] = '1';
            }
            if (booking.ExtraService3)
            {

                booking.ExtraHours += 0.5;
                array[2] = '1';
            }
            if (booking.ExtraService4)
            {

                booking.ExtraHours += 0.5;
                array[3] = '1';
            }
            if (booking.ExtraService5)
            {

                booking.ExtraHours += 0.5;
                array[4] = '1';
            }
            //var extra=array.ToString();
            string extra = new string(array);
            string date = booking.StartDate.ToString("yyyy-MM-dd");
            string time = booking.StartTime.ToString("HH:mm:ss");
            DateTime startDateTime = Convert.ToDateTime(date).Add(TimeSpan.Parse(time));



            booking.ServiceStartDate = startDateTime;
            booking.ZipCode = HttpContext.Session.GetString("ZipCode");
            booking.UserId = (int)HttpContext.Session.GetInt32("UserId");
            booking.CreatedDate = DateTime.Now;
            booking.ModifiedDate = DateTime.Now;
            booking.PaymentDone = true;
            booking.PaymentDue = false;
            booking.Extra = Convert.ToInt32(extra);


            HttpContext.Session.SetString("schedule", JsonConvert.SerializeObject(booking));
            return View("Book_now");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(ContactU contactU, String Lastname)
        {
            if (ModelState.IsValid)
            {
                using HelperlandContext objHelperlandContext = new HelperlandContext();

                string lastname = Convert.ToString(Lastname);
                contactU.Name = (contactU.Name + " " + lastname);
                contactU.Message = ("This mail is sent by " + contactU.Name + ", <br/><br/> Email:-  " + contactU.Email + "<br/> Phone number:-  " + contactU.PhoneNumber + "<br/><br/> Query:-  " + contactU.Message);

                contactU.CreatedOn = DateTime.Now;

                objHelperlandContext.ContactUs.Add(contactU);
                objHelperlandContext.SaveChanges();

                //sending mail logic starts here
                using MailMessage mm = new MailMessage("ashish.chauhan93133@gmail.com", "ashishchauhan26560@gmail.com");



                mm.Subject = contactU.Subject;
                mm.Body = contactU.Message;

                mm.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient())
                {
                    String password = new string("FeelFree@2389");
                    String from = new string("ashish.chauhan93133@gmail.com");
                    smtp.Host = "smtp.gmail.com";
                    smtp.EnableSsl = true;
                    NetworkCredential NetworkCred = new NetworkCredential(from, password);

                    smtp.UseDefaultCredentials = true;
                    smtp.Credentials = NetworkCred;
                    smtp.Port = 587;
                    smtp.Send(mm);
                    ViewBag.Message = "Email has been sent to the admin";
                }


                ModelState.Clear();

            }
            return View();
        }


        public IActionResult Faq()
        {
            return View();
        }
        public IActionResult Createaccount()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Createaccount(User user)
        {
            if (ModelState.IsValid)
            {
                using HelperlandContext objHelperlandContext = new HelperlandContext();
                //ContactU contact = new ContactU
                //{
                //    CreatedOn = DateTime.Now
                //};
                //contactU.CreatedOn= contact.CreatedOn;


                user.CreatedDate = DateTime.Now;
                user.ModifiedDate = DateTime.Now;
                user.UserTypeId = 1;

                objHelperlandContext.Users.Add(user);
                objHelperlandContext.SaveChanges();
                // Int64 id = objEmployee.EmployeeID;
                ModelState.Clear();
                // return View(objEmployee);

            }
            return View();
        }
    }
}
