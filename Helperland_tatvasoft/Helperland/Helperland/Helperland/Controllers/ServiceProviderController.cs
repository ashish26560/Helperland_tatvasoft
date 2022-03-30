using cloudscribe.Pagination.Models;
using Helperland.Data;
using Helperland.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Helperland.Controllers
{
    public class ServiceProviderController : Controller
    {
        public readonly HelperlandContext _context;

        public ServiceProviderController(HelperlandContext context)
        {
            _context = context;
        }
        public IActionResult ServiceProvider()
        {
            return View();
        }
        public async Task<IActionResult> Export()
        {
            int pagenumber = (int)HttpContext.Session.GetInt32("pagenumber");
            int pagesize = (int)HttpContext.Session.GetInt32("pagesize");

            int excluderecords = (pagenumber * pagesize) - pagesize;
            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var servicelist = await _context.ServiceRequests.Where(c => c.SpacceptedDate != null && c.Status == 3 && c.ServiceProviderId == userid).Skip(excluderecords).Take(pagesize).ToListAsync();
            foreach (ServiceRequest service in servicelist)
            {
                var serviceproviderdetails = await _context.Users.Where(c => c.UserId == service.UserId).FirstOrDefaultAsync();

                service.ServiceProviderName = serviceproviderdetails.FirstName + " " + serviceproviderdetails.LastName;

                var temp = await _context.ServiceRequestAddresses.Where(c => c.ServiceRequestId == service.ServiceRequestId).FirstOrDefaultAsync();

                service.ServiceAddress += temp.AddressLine1 + ", ";
                service.ServiceAddress += temp.AddressLine2 + ", ";
                service.ServiceAddress += temp.City;
            }
            var builder = new StringBuilder();
            builder.AppendLine("Service ID,Service date,Customer Details");
            foreach (var item in servicelist)
            {
                builder.AppendLine($"{item.ServiceRequestId},{item.ServiceStartDate},{item.ServiceProviderName}");
            }
            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Servicehistory.csv");
        }
        public IActionResult ServiceproviderId(int id)
        {
            TempData["pageid"] = id;
            return View("ServiceProvider");
        }
        public async Task<IActionResult> ServiceProviderPage(int pagenumber = 1, int pagesize = 5)
        {

            int excluderecords = (pagenumber * pagesize) - pagesize;

            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var user = await _context.Users.Where(c => c.UserId == userid).FirstOrDefaultAsync();
            var servicelist = await _context.ServiceRequests.Where(c => c.SpacceptedDate == null && c.ServiceStartDate >= DateTime.Now && (c.Status == 1 || c.Status == 5 || c.Status == 7) && c.ZipCode == user.ZipCode).ToListAsync();
            var slist = await _context.ServiceRequests.Where(c => c.SpacceptedDate == null && c.ServiceStartDate >= DateTime.Now && (c.Status == 1 || c.Status == 5 || c.Status == 7) && c.ZipCode == user.ZipCode).ToListAsync();

            //filtering the data where customer blocked serviceprovider and service provider blocked customer
            var block = await _context.FavoriteAndBlockeds.Where(c => c.UserId == userid && c.IsBlocked == true).ToListAsync();
            var blockby = await _context.FavoriteAndBlockeds.Where(c => c.TargetUserId == userid && c.IsBlocked == true).ToListAsync();

            foreach (FavoriteAndBlocked b in block)
            {
                servicelist = servicelist.Where(c => c.UserId != b.TargetUserId).ToList();
                slist = slist.Where(c => c.UserId != b.TargetUserId).ToList();
            }
            foreach (FavoriteAndBlocked b in blockby)
            {
                servicelist = servicelist.Where(c => c.UserId != b.UserId).ToList();
                slist = slist.Where(c => c.UserId != b.UserId).ToList();
            }


            servicelist = servicelist.Skip(excluderecords).Take(pagesize).ToList();

            slist = slist.ToList();

            var result = new PagedResult<ServiceRequest>
            {
                Data = servicelist,
                TotalItems = slist.Count(),
                PageNumber = pagenumber,
                PageSize = pagesize

            };

            foreach (ServiceRequest service in servicelist)
            {
                var serviceproviderdetails = await _context.Users.Where(c => c.UserId == service.UserId).FirstOrDefaultAsync();

                service.ServiceProviderName = serviceproviderdetails.FirstName + " " + serviceproviderdetails.LastName;

                var temp = await _context.ServiceRequestAddresses.Where(c => c.ServiceRequestId == service.ServiceRequestId).FirstOrDefaultAsync();

                service.ServiceAddress += temp.AddressLine1 + ", ";
                service.ServiceAddress += temp.AddressLine2 + ", ";
                service.ServiceAddress += temp.City;
            }

            return View("_ServProvNewServRequest", result);
        }
        public async Task<IActionResult> AcceptServiceRequest(int id)
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var serviceRequest = await _context.ServiceRequests.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();


            //accepted by other provider or not
            if (serviceRequest.SpacceptedDate == null)
            {
                //date and time of request accept service
                string Sdate = serviceRequest.ServiceStartDate.ToString("d");

                string Stime = serviceRequest.ServiceStartDate.ToString("T");

                DateTime RescStartTime = Convert.ToDateTime(Stime);

                string Etime = serviceRequest.ServiceStartDate.AddHours(serviceRequest.ServiceHours).ToString("T");

                DateTime RescEndTime = Convert.ToDateTime(Etime);

                var acceptedservice = await _context.ServiceRequests.Where(c => c.ServiceProviderId == userid
                                                                   && c.SpacceptedDate != null &&
                                                                   (c.Status == 2 || c.Status == 5)).ToListAsync();

                //check each accepted request for conflict
                foreach (ServiceRequest service in acceptedservice)
                {
                    string sdate = service.ServiceStartDate.ToString("d");
                    string stime = service.ServiceStartDate.ToString("T");
                    DateTime AccStartTime = Convert.ToDateTime(stime);
                    string etime = service.ServiceStartDate.AddHours(service.ServiceHours).ToString("T");
                    DateTime AccEndTime = Convert.ToDateTime(etime);
                    if (sdate == Sdate)
                    {
                        if ((RescStartTime >= AccStartTime && RescStartTime <= AccEndTime)
                        || (RescEndTime >= AccStartTime && RescEndTime <= AccEndTime))
                        {

                            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            return Json(new
                            {
                                message = "Another service request having <b>ID " + service.ServiceRequestId +
                                " </b>has already been assigned which has time overlap with this service request.You can’t pick this one!"
                            });
                        }
                    }
                }


                TempData["responseid"] = "1";
                serviceRequest.SpacceptedDate = DateTime.Now;
                serviceRequest.Status = 2;
                serviceRequest.ModifiedDate = DateTime.Now;
                serviceRequest.ServiceProviderId = userid;
                serviceRequest.ModifiedBy = userid;
                await _context.SaveChangesAsync();

                var serviceproviderlist = _context.Users.Where(c => c.ZipCode == serviceRequest.ZipCode
                && c.UserTypeId == 2 && c.UserId != userid).ToList();
                foreach (User mail in serviceproviderlist)
                {
                    string body = "Hi " + mail.FirstName + ", <br/><br/> Service request " + serviceRequest.ServiceRequestId +
                        ": is no more available now.<br/><br/> Thank you";
                    string subject = "ServiceRequest in your area.";
                    SendEmail(mail.Email, body, subject);
                }

                var user = await _context.Users.Where(c => c.UserId == serviceRequest.UserId).FirstOrDefaultAsync();
                string cbody = "Hi " + user.FirstName + ", <br/><br/> Service request " + serviceRequest.ServiceRequestId +
                    ": is accepted by a service provider.<br/><br/> Thank you";
                string csubject = "ServiceRequest in accepted.";
                SendEmail(user.Email, cbody, csubject);
                return View("_AcceptReqResponseModal");

            }
            else
            {
                //show error that request already accepted
                TempData["responseid"] = "2";
                return View("_AcceptReqResponseModal");

            }
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
        public async Task<IActionResult> ServProvDetailsModal(int id)
        {
            var p = await _context.ServiceRequests.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();


            var serviceproviderdetails = await _context.Users.Where(c => c.UserId == p.UserId).FirstOrDefaultAsync();

            p.ServiceProviderName = serviceproviderdetails.FirstName + " " + serviceproviderdetails.LastName;

            var temp = await _context.ServiceRequestAddresses.Where(c => c.ServiceRequestId == p.ServiceRequestId).FirstOrDefaultAsync();

            p.ServiceAddress += temp.AddressLine1 + ", ";
            p.ServiceAddress += temp.AddressLine2 + ", ";
            p.ServiceAddress += temp.City;
            p.Mobile = serviceproviderdetails.Mobile;
            p.Email = serviceproviderdetails.Email;

            var extraservice = await _context.ServiceRequestExtras.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();
            p.Extra = extraservice.ServiceExtraId;


            return View("_ServProvDetailsModal", p);

        }
        public async Task<IActionResult> ServProvServHistory(int pagenumber = 1, int pagesize = 5)
        {
            HttpContext.Session.SetInt32("pagenumber", pagenumber);
            HttpContext.Session.SetInt32("pagesize", pagesize);
            int excluderecords = (pagenumber * pagesize) - pagesize;
            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var servicelist = await _context.ServiceRequests.Where(c => c.SpacceptedDate != null && c.Status == 3 && c.ServiceProviderId == userid).Skip(excluderecords).Take(pagesize).ToListAsync();
            var slist = await _context.ServiceRequests.Where(c => c.SpacceptedDate != null && c.Status == 3 && c.ServiceProviderId == userid).ToListAsync();

            var result = new PagedResult<ServiceRequest>
            {
                Data = servicelist,
                TotalItems = slist.Count(),
                PageNumber = pagenumber,
                PageSize = pagesize

            };

            foreach (ServiceRequest service in servicelist)
            {
                var serviceproviderdetails = await _context.Users.Where(c => c.UserId == service.UserId).FirstOrDefaultAsync();

                service.ServiceProviderName = serviceproviderdetails.FirstName + " " + serviceproviderdetails.LastName;

                var temp = await _context.ServiceRequestAddresses.Where(c => c.ServiceRequestId == service.ServiceRequestId).FirstOrDefaultAsync();

                service.ServiceAddress += temp.AddressLine1 + ", ";
                service.ServiceAddress += temp.AddressLine2 + ", ";
                service.ServiceAddress += temp.City;
            }

            return View("_ServProvServHistory", result);
        }
        public async Task<IActionResult> ServProvUpcomServ(int pagenumber = 1, int pagesize = 5)
        {

            int excluderecords = (pagenumber * pagesize) - pagesize;
            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var servicelist = await _context.ServiceRequests.Where(c => c.SpacceptedDate != null && (c.Status == 2 || c.Status == 5) && c.ServiceStartDate.AddHours(24) >= DateTime.Now && c.ServiceProviderId == userid).Skip(excluderecords).Take(pagesize).ToListAsync();
            var slist = await _context.ServiceRequests.Where(c => c.SpacceptedDate != null && (c.Status == 2 || c.Status == 5) && c.ServiceStartDate.AddHours(24) >= DateTime.Now && c.ServiceProviderId == userid).ToListAsync();

            var result = new PagedResult<ServiceRequest>
            {
                Data = servicelist,
                TotalItems = slist.Count(),
                PageNumber = pagenumber,
                PageSize = pagesize

            };

            foreach (ServiceRequest service in servicelist)
            {
                var serviceproviderdetails = await _context.Users.Where(c => c.UserId == service.UserId).FirstOrDefaultAsync();

                service.ServiceProviderName = serviceproviderdetails.FirstName + " " + serviceproviderdetails.LastName;

                var temp = await _context.ServiceRequestAddresses.Where(c => c.ServiceRequestId == service.ServiceRequestId).FirstOrDefaultAsync();

                service.ServiceAddress += temp.AddressLine1 + ", ";
                service.ServiceAddress += temp.AddressLine2 + ", ";
                service.ServiceAddress += temp.City;
            }

            return View("_ServProvUpcomServ", result);
        }
        public async Task<IActionResult> CancelRequestModal(int id)
        {
            var p = await _context.ServiceRequests.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();

            return View("_CancelModal", p);
        }
        public async Task<IActionResult> CompleteRequest(int id)
        {
            var p = await _context.ServiceRequests.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();

            p.Status = 3;
            await _context.SaveChangesAsync();
            return View("_CancelModal", p);
        }
        public async Task<IActionResult> CancelService(ServiceRequest serviceRequest)
        {
            var p = await _context.ServiceRequests.Where(c => c.ServiceRequestId == serviceRequest.ServiceRequestId).FirstOrDefaultAsync();

            p.Status = 4;
            p.Comments = serviceRequest.Comments;
            await _context.SaveChangesAsync();

            var user = await _context.Users.Where(c => c.UserId == p.UserId).FirstOrDefaultAsync();
            string cbody = "Hi " + user.FirstName + ", <br/><br/> Service request " + p.ServiceRequestId +
                ": is cancelled by a service provider.<br/> the reason stated by service provider is as below,<br/> '" + p.Comments + "'<br/><br/> Thank you";
            string csubject = "ServiceRequest in your area.";
            SendEmail(user.Email, cbody, csubject);
            return View("ServiceProvider");
        }
        public async Task<IActionResult> Myratings(int pagenumber = 1, int pagesize = 5)
        {

            int excluderecords = (pagenumber * pagesize) - pagesize;

            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var servicelist = await _context.ServiceRequests.Where(c => c.ServiceProviderId == userid && c.SpacceptedDate != null && c.Status == 3).Skip(excluderecords).Take(pagesize).ToListAsync();
            var slist = await _context.ServiceRequests.Where(c => c.ServiceProviderId == userid && c.SpacceptedDate != null && c.Status == 3).ToListAsync();

            foreach (ServiceRequest service in servicelist)
            {
                var serviceproviderdetails = await _context.Users.Where(c => c.UserId == service.UserId).FirstOrDefaultAsync();
                var rating = await _context.Ratings.Where(c => c.ServiceRequestId == service.ServiceRequestId).FirstOrDefaultAsync();
                if (rating != null)
                {
                    service.ratings = rating.Ratings;
                }
                service.ServiceProviderName = serviceproviderdetails.FirstName + " " + serviceproviderdetails.LastName;

            }

            var result = new PagedResult<ServiceRequest>
            {
                Data = servicelist,
                TotalItems = slist.Count(),
                PageNumber = pagenumber,
                PageSize = pagesize

            };

            return View("_ServProvMyRatings", result);
        }
        public async Task<IActionResult> ServProvMySetting()
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var userdetails = await _context.Users.Where(c => c.UserId == userid).FirstOrDefaultAsync();
            var useraddress = await _context.UserAddresses.Where(c => c.UserId == userid).FirstOrDefaultAsync();
            if (useraddress != null)
            {
                ViewBag.Addressline1 = useraddress.AddressLine1;
                ViewBag.Addressline2 = useraddress.AddressLine2;
                ViewBag.City = useraddress.City;
                ViewBag.PostalCode = useraddress.PostalCode;

            }
            if (userdetails.DateOfBirth != null)
            {
                DateTime datevalue = (Convert.ToDateTime(userdetails.DateOfBirth.ToString()));
                userdetails.Day = datevalue.Day.ToString();
                userdetails.Month = datevalue.Month.ToString();
                userdetails.Year = datevalue.Year.ToString();
            }
            return View("_ServProvMySettings", userdetails);
        }
        public async Task<IActionResult> ServUpdateMyDetails(User user, string Addressline2, string Addressline1, string Postalcode, string City)
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var userdetails = await _context.Users.Where(c => c.UserId == userid).FirstOrDefaultAsync();
            var useraddress = await _context.UserAddresses.Where(c => c.UserId == userid).FirstOrDefaultAsync();
            if (useraddress != null)
            {
                useraddress.PostalCode = Postalcode;
                userdetails.ZipCode = Postalcode;
                useraddress.AddressLine1 = Addressline1;
                useraddress.AddressLine2 = Addressline2;
                useraddress.City = City;
                await _context.SaveChangesAsync();
            }
            else
            {
                UserAddress userAddress = new UserAddress
                {
                    UserId = userid,
                    PostalCode = Postalcode,
                    AddressLine1 = Addressline1,
                    AddressLine2 = Addressline2,
                    City = City,
                    Mobile = user.Mobile,
                    Email = user.Email
                };

                userdetails.ZipCode = Postalcode;
                await _context.UserAddresses.AddAsync(userAddress);
                await _context.SaveChangesAsync();
            }
            userdetails.FirstName = user.FirstName;
            userdetails.LastName = user.LastName;
            userdetails.Mobile = user.Mobile;
            userdetails.NationalityId = user.NationalityId;
            userdetails.Gender = user.Gender;
            userdetails.UserProfilePicture = user.UserProfilePicture;

            if (user.Day != null && user.Month != null && user.Year != null)
            {
                var DateTime = user.Day + "-" + user.Month + "-" + user.Year;
                userdetails.DateOfBirth = Convert.ToDateTime(DateTime);
            }
            await _context.SaveChangesAsync();
            return View("ServiceProvider");
        }
        [HttpPost]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(User user)
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var curuser = await _context.Users.Where(c => c.UserId == userid).FirstOrDefaultAsync();

            if (user.Password == curuser.Password)
            {
                curuser.Password = user.NewPassword;

                await _context.SaveChangesAsync();
                //ModelState.Clear();

                return View("ServiceProvider");
            }
            else
            {
                //please check your current password
                return StatusCode(500);
            }
        }
        public async Task<string> GetCityName(string id)
        {
            var p = await _context.Zipcodes.Where(c => c.ZipcodeValue == id).FirstOrDefaultAsync();
            if (p == null)
            {
                return "NotFound";
            }
            var q = await _context.Cities.Where(c => c.Id == p.CityId).FirstOrDefaultAsync();
            string cityname = q.CityName;

            return cityname;
        }
        public async Task<IActionResult> LoadBlockCustomer()
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var p = _context.ServiceRequests.Where(c => c.ServiceProviderId == userid && (c.Status == 3 || c.Status == 4)).AsEnumerable().GroupBy(x => x.UserId).ToList();

            List<User> users = new List<User>();
            List<string> blocklist = new List<string>();
            foreach (var i in p)
            {
                User temp = await _context.Users.FindAsync(i.Key);
                users.Add(temp);

                var blockornot = await _context.FavoriteAndBlockeds.Where(c => c.UserId == userid && c.TargetUserId == i.Key).FirstOrDefaultAsync();
                if (blockornot != null)
                {
                    if (blockornot.IsBlocked == true)
                    {
                        string value = "yes";
                        blocklist.Add(value);
                    }
                    else
                    {
                        string value = "no";
                        blocklist.Add(value);
                    }
                }
                else
                {

                    string value = "no";
                    blocklist.Add(value);
                }
            }

            ViewBag.blockedornot = blocklist;
            return View("_ServProvBlockCustomer", users);
        }
        public async Task<IActionResult> BlockUser(int id)
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var p = await _context.FavoriteAndBlockeds.Where(c => c.UserId == userid && c.TargetUserId == id).FirstOrDefaultAsync();
            if (p == null)
            {
                FavoriteAndBlocked favoriteAndBlocked = new FavoriteAndBlocked
                {
                    UserId = userid,
                    TargetUserId = id,
                    IsBlocked = true,
                    IsFavorite = false
                };

                await _context.FavoriteAndBlockeds.AddAsync(favoriteAndBlocked);
                await _context.SaveChangesAsync();
            }
            else
            {
                p.IsBlocked = true;
                await _context.SaveChangesAsync();
            }
            return View("ServiceProvider");
        }
        public async Task<IActionResult> UnBlockUser(int id)
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var p = await _context.FavoriteAndBlockeds.Where(c => c.UserId == userid && c.TargetUserId == id).FirstOrDefaultAsync();
            if (p != null)
            {
                p.IsBlocked = false;
                await _context.SaveChangesAsync();
            }
            return View("ServiceProvider");
        }
    }
}
