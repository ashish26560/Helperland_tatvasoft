using Helperland.Data;
using Helperland.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cloudscribe.Pagination.Models;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace Helperland.Controllers
{
    public class UserController : Controller
    {
        public readonly HelperlandContext _context;

        public UserController(HelperlandContext context)
        {
            _context = context;
        }

        public IActionResult Customer()
        {

            return View();
        }
        public IActionResult CustomerId(int id)
        {
            TempData["pageid"] = id;
            return View("Customer");
        }

        public async Task<IActionResult> Export()
        {
            int pagenumber = (int)HttpContext.Session.GetInt32("pagenumber");
            int pagesize = (int)HttpContext.Session.GetInt32("pagesize");

            int excluderecords = (pagenumber * pagesize) - pagesize;

            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var servicelist = await _context.ServiceRequests.Where(c => c.UserId == userid &&
            (c.Status == 3 || c.Status == 4 || c.Status == 6 || c.Status == 8)).Skip(excluderecords).Take(pagesize).ToListAsync();

            foreach (ServiceRequest service in servicelist)
            {
                if (service.ServiceProviderId != null)
                {
                    var serviceproviderdetails = await _context.Users.Where(c => c.UserId == service.ServiceProviderId).FirstOrDefaultAsync();

                    service.ServiceProviderName = serviceproviderdetails.FirstName + " " + serviceproviderdetails.LastName;

                    var rate = await _context.Ratings.Where(c => c.RatingTo == service.ServiceProviderId).ToListAsync();
                    decimal temp = 0;
                    foreach (Rating rating in rate)
                    {
                        if (rating.Ratings != 0)
                        {
                            temp += rating.Ratings;
                        }
                    }
                    if (rate.Count() != 0)
                    {
                        temp /= rate.Count();

                    }
                    service.ratings = temp;
                }

            }
            var builder = new StringBuilder();
            builder.AppendLine("Service ID,Service date,Service Provider,Payment,status");
            foreach (var item in servicelist)
            {
                builder.AppendLine($"{item.ServiceRequestId},{item.ServiceStartDate},{item.ServiceProviderName},{item.TotalCost},{item.Status}");
            }
            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Servicehistory.csv");
        }

        public async Task<IActionResult> RateSpModal(int id)
        {
            var p = await _context.ServiceRequests.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();
            var q = await _context.Users.Where(c => c.UserId == p.ServiceProviderId).FirstOrDefaultAsync();

            var rate = await _context.Ratings.Where(c => c.RatingTo == p.ServiceProviderId).ToListAsync();
            var currentrate = await _context.Ratings.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();

            decimal temp = 0;

            foreach (Rating rating in rate)
            {
                if (rating.Ratings != 0)
                {
                    temp += rating.Ratings;
                }
            }
            if (rate.Count() != 0)
            {
                temp /= rate.Count();

            }

            ViewBag.servicerequestid = id;
            ViewBag.serviceproviderid = p.ServiceProviderId;
            ViewBag.rating = temp;
            ViewBag.serviceprovidername = q.FirstName + " " + q.LastName;

            return View("_RateSpModal", currentrate);

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
                curuser.ModifiedDate = DateTime.Now;
                curuser.ModifiedBy = userid;
                await _context.SaveChangesAsync();
                //ModelState.Clear();


                return View("customer");
            }
            else
            {
                //please check your current password
                return StatusCode(500);
            }



        }
        [HttpPost]
        public async Task<IActionResult> AddRatings(Rating rating)
        {

            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var p = _context.Ratings.Where(c => c.ServiceRequestId == rating.ServiceRequestId).FirstOrDefault();

            rating.Ratings = (rating.OnTimeArrival + rating.QualityOfService + rating.Friendly) / 3;
            rating.RatingFrom = userid;
            rating.RatingDate = DateTime.Now;

            if (p != null)
            {
                p.OnTimeArrival = rating.OnTimeArrival;
                p.QualityOfService = rating.QualityOfService;
                p.Friendly = rating.Friendly;
                p.Ratings = rating.Ratings;
                p.RatingDate = rating.RatingDate;
                p.RatingFrom = rating.RatingFrom;
                p.RatingTo = rating.RatingTo;
                p.Comments = rating.Comments;
                await _context.SaveChangesAsync();

            }
            else
            {
                await _context.Ratings.AddAsync(rating);

                await _context.SaveChangesAsync();

            }
            return View("Customer");
        }

        [HttpPost]
        public async Task<IActionResult> CustUpdateMyDetails(User user)
        {

            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var p = await _context.Users.Where(c => c.UserId == userid).FirstOrDefaultAsync();

            p.FirstName = user.FirstName;
            p.LastName = user.LastName;
            p.LanguageId = user.LanguageId;
            p.Mobile = user.Mobile;
            p.ModifiedBy = userid;
            p.ModifiedDate = DateTime.Now;

            if (user.Day != null && user.Month != null && user.Year != null)
            {
                var DateTime = user.Day + "-" + user.Month + "-" + user.Year;
                p.DateOfBirth = Convert.ToDateTime(DateTime);
            }
            await _context.SaveChangesAsync();

            return View("Customer");
        }
        public async Task<IActionResult> CustGetMydetails()
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var p = await _context.Users.Where(c => c.UserId == userid).FirstOrDefaultAsync();

            //var date = p.DateOfBirth.Day.ToString();
            if (p.DateOfBirth != null)
            {
                DateTime datevalue = Convert.ToDateTime(p.DateOfBirth.ToString());
                p.Day = datevalue.Day.ToString();
                p.Month = datevalue.Month.ToString();
                p.Year = datevalue.Year.ToString();
            }

            return PartialView("_CustomerMysetMydetails", p);
        }
        public async Task<IActionResult> CustGetMyaddress()
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var p = await _context.UserAddresses.Where(c => c.UserId == userid).ToListAsync();

            return PartialView("_CustomerMysetMyaddress", p);
        }
        public async Task<IActionResult> CustomerMySettings()
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var p = await _context.ServiceRequests.Where(c => c.UserId == userid && (c.Status == 1 || c.Status == 2 || c.Status == 5 || c.Status == 7)).ToListAsync();

            return PartialView("_CustomerMysettings");
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
        public async Task<IActionResult> Customerpage(int pagenumber = 1, int pagesize = 5)
        {

            int excluderecords = (pagenumber * pagesize) - pagesize;

            var userid = (int)HttpContext.Session.GetInt32("UserId");

            List<string> profilepicturelist = new List<string>();

            var servicelist = await _context.ServiceRequests.
                Where(c => c.UserId == userid && c.ServiceStartDate >= DateTime.Now &&
                (c.Status == 1 || c.Status == 2 || c.Status == 5 || c.Status == 7)).ToListAsync();
            var slist = await _context.ServiceRequests.
                Where(c => c.UserId == userid && c.ServiceStartDate >= DateTime.Now &&
                (c.Status == 1 || c.Status == 2 || c.Status == 5 || c.Status == 7)).ToListAsync();

            //filtering the data where customer blocked serviceprovider and service provider blocked customer
            var block = await _context.FavoriteAndBlockeds.Where(c => c.UserId == userid && c.IsBlocked == true).ToListAsync();
            var blockby = await _context.FavoriteAndBlockeds.Where(c => c.TargetUserId == userid && c.IsBlocked == true).ToListAsync();

            foreach (FavoriteAndBlocked b in block)
            {
                servicelist = servicelist.Where(c => c.ServiceProviderId != b.TargetUserId).ToList();
                slist = slist.Where(c => c.ServiceProviderId != b.TargetUserId).ToList();
            }
            foreach (FavoriteAndBlocked b in blockby)
            {
                servicelist = servicelist.Where(c => c.ServiceProviderId != b.UserId).ToList();
                slist = slist.Where(c => c.ServiceProviderId != b.UserId).ToList();
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
                if (service.ServiceProviderId != null)
                {
                    var userpic = _context.Users.Where(c => c.UserId == service.ServiceProviderId).FirstOrDefault();
                    if (userpic != null)
                    {
                        profilepicturelist.Add(userpic.UserProfilePicture);

                    }
                    var serviceproviderdetails = await _context.Users.Where(c => c.UserId == service.ServiceProviderId).FirstOrDefaultAsync();

                    service.ServiceProviderName = serviceproviderdetails.FirstName + " " + serviceproviderdetails.LastName;

                    var rate = await _context.Ratings.Where(c => c.RatingTo == service.ServiceProviderId).ToListAsync();
                    decimal temp = 0;
                    foreach (Rating rating in rate)
                    {
                        if (rating.Ratings != 0)
                        {
                            temp += rating.Ratings;
                        }
                    }
                    if (rate.Count() != 0)
                    {
                        temp /= rate.Count();

                    }
                    service.ratings = temp;
                }
                else
                {

                    profilepicturelist.Add(null);
                }

            }

            ViewBag.profilepicture = profilepicturelist;
            return PartialView("_CustomerDashboard", result);
        }

        public async Task<IActionResult> AddressModal(int id)
        {

            var p = await _context.UserAddresses.Where(c => c.AddressId == id).FirstOrDefaultAsync();

            return PartialView("_AddressModals", p);
        }
        public async Task<IActionResult> AddAddress(UserAddress userAddress)
        {
            await _context.UserAddresses.AddAsync(userAddress);
            await _context.SaveChangesAsync();

            return PartialView("customer");
        }
        [HttpPost]
        public async Task<IActionResult> EditAddress(UserAddress userAddress)
        {

            var p = await _context.UserAddresses.Where(c => c.AddressId == userAddress.AddressId).FirstOrDefaultAsync();
            if (p == null)
            {
                var userid = (int)HttpContext.Session.GetInt32("UserId");

                var temp = _context.Users.Where(c => c.UserId == userid).FirstOrDefault();

                userAddress.UserId = userid;
                userAddress.IsDefault = false;
                userAddress.IsDeleted = false;
                userAddress.Email = temp.Email;

                await _context.UserAddresses.AddAsync(userAddress);

                await _context.SaveChangesAsync();

                return View("customer");
            }
            else
            {

                p.AddressLine1 = userAddress.AddressLine1;
                p.AddressLine2 = userAddress.AddressLine2;
                p.City = userAddress.City;
                p.PostalCode = userAddress.PostalCode;
                p.Mobile = userAddress.Mobile;

                await _context.SaveChangesAsync();
                return View("customer");
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteAddress(UserAddress userAddress)
        {

            var p = await _context.UserAddresses.Where(c => c.AddressId == userAddress.AddressId).FirstOrDefaultAsync();

            _context.UserAddresses.Remove(p);
            await _context.SaveChangesAsync();

            return View("customer");
        }
        [HttpPost]
        public async Task<IActionResult> GetServicedetails(int id)
        {

            var p = await _context.ServiceRequests.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();
            var extraservice = await _context.ServiceRequestExtras.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();
            var CurrentServiceAddress = await _context.ServiceRequestAddresses.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();
            p.Extra = extraservice.ServiceExtraId;
            string serviceaddress = "";
            serviceaddress += CurrentServiceAddress.AddressLine1 + ",";
            serviceaddress += CurrentServiceAddress.AddressLine2 + " ";
            serviceaddress += CurrentServiceAddress.City;
            p.Mobile += CurrentServiceAddress.Mobile;
            p.Email += CurrentServiceAddress.Email;
            p.ServiceAddress = serviceaddress;

            //var serviceDateTime = p.ServiceStartDate;
            //var starttime = serviceDateTime.ToString("t");
            //p.StartTime = Convert.ToDateTime(starttime);

            //ViewBag.p = p;
            //ViewBag.show = "true";
            return PartialView("_ServiceDetailsPopup", p);

        }

        [HttpPost]
        public async Task<IActionResult> RescheduleService(ServiceRequest serviceRequest)
        {
            var p = await _context.ServiceRequests.Where(c => c.ServiceRequestId == serviceRequest.ServiceRequestId).FirstOrDefaultAsync();

            string date = serviceRequest.ServiceStartDate.ToString("d");

            string time = serviceRequest.StartTime.ToString("T");
            DateTime startDateTime = Convert.ToDateTime(date).Add(TimeSpan.Parse(time));

            string Stime = startDateTime.ToString("T");
            DateTime RescStartTime = Convert.ToDateTime(Stime);
            string Etime = startDateTime.AddHours(p.ServiceHours).ToString("T");
            DateTime RescEndTime = Convert.ToDateTime(Etime);

            if (p.ServiceProviderId != null)
            {
                var acceptedservice = await _context.ServiceRequests.Where(c => c.ServiceProviderId == p.ServiceProviderId && (c.Status == 2 || c.Status == 5)).ToListAsync();
                foreach (ServiceRequest service in acceptedservice)
                {
                    string sdate = service.ServiceStartDate.ToString("d");
                    string stime = service.ServiceStartDate.ToString("T");
                    DateTime AccStartTime = Convert.ToDateTime(stime);
                    string etime = service.ServiceStartDate.AddHours(service.ServiceHours).ToString("T");
                    DateTime AccEndTime = Convert.ToDateTime(etime);
                    if (sdate == date)
                    {
                        if ((RescStartTime >= AccStartTime && RescStartTime <= AccEndTime)
                        || (RescEndTime >= AccStartTime && RescEndTime <= AccEndTime))
                        {

                            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            return Json(new
                            {
                                message = "Another service request has been assigned to the service provider on <b>" + sdate + " </b>from<b> " + stime + " </b>to<b> " + etime +
                                ". </b>Either choose another date or pick up a different time slot."
                            });
                        }
                    }
                }
                p.Status = 5;
                p.ServiceStartDate = startDateTime;
                await _context.SaveChangesAsync();
                var user = await _context.Users.Where(c => c.UserId == p.ServiceProviderId).FirstOrDefaultAsync();
                string cbody = "Hi " + user.FirstName + ", <br/><br/> Service request : " + p.ServiceRequestId +
                    " is Rescheduled by a Customer.<br/> Rescheduled Date :" + date + " <br/>Rescheduled Start time :" + Stime + "<br/><br/> Thank you";
                string csubject = "ServiceRequest in your area.";
                SendEmail(user.Email, cbody, csubject);


                return View("customer");
            }
            else
            {

                //status=5 for reschedule service
                p.Status = 5;
                p.ServiceStartDate = startDateTime;

                await _context.SaveChangesAsync();



                return View("customer");
            }

            //returning the updated customerdashboard

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
        public async Task<IActionResult> CancelService(ServiceRequest serviceRequest)
        {
            var p = await _context.ServiceRequests.Where(c => c.ServiceRequestId == serviceRequest.ServiceRequestId).FirstOrDefaultAsync();

            p.Comments = serviceRequest.Comments;

            //status=4 for canceled service
            p.Status = 4;

            await _context.SaveChangesAsync();
            var user = await _context.Users.Where(c => c.UserId == p.ServiceProviderId).FirstOrDefaultAsync();
            string cbody = "Hi " + user.FirstName + ", <br/><br/> Service request " + p.ServiceRequestId +
                ": is cancelled by a service provider.<br/> the reason stated by service provider is as below,<br/> '" + p.Comments + "'<br/><br/> Thank you";
            string csubject = "ServiceRequest in your area.";
            SendEmail(user.Email, cbody, csubject);
            //returning the updated customerdashboard
            return View("customer");
        }

        public async Task<IActionResult> ServiceHistory(int pagenumber = 1, int pagesize = 5)
        {
            HttpContext.Session.SetInt32("pagenumber", pagenumber);
            HttpContext.Session.SetInt32("pagesize", pagesize);

            int excluderecords = (pagenumber * pagesize) - pagesize;

            var userid = (int)HttpContext.Session.GetInt32("UserId");

            List<string> profilepicturelist = new List<string>();

            var servicelist = await _context.ServiceRequests.Where(c => c.UserId == userid
            && (c.Status == 3 || c.Status == 4 || c.Status == 6 || c.Status == 8)).Skip(excluderecords).Take(pagesize).ToListAsync();
            var slist = await _context.ServiceRequests.Where(c => c.UserId == userid &&
            (c.Status == 3 || c.Status == 4 || c.Status == 6 || c.Status == 8)).ToListAsync();


            var result = new PagedResult<ServiceRequest>
            {
                Data = servicelist,
                TotalItems = slist.Count(),
                PageNumber = pagenumber,
                PageSize = pagesize

            };

            foreach (ServiceRequest service in servicelist)
            {
                if (service.ServiceProviderId != null)
                {
                    var userpic = _context.Users.Where(c => c.UserId == service.ServiceProviderId).FirstOrDefault();
                    if (userpic != null)
                    {
                        profilepicturelist.Add(userpic.UserProfilePicture);

                    }

                    var serviceproviderdetails = await _context.Users.Where(c => c.UserId == service.ServiceProviderId).FirstOrDefaultAsync();

                    service.ServiceProviderName = serviceproviderdetails.FirstName + " " + serviceproviderdetails.LastName;

                    var rate = await _context.Ratings.Where(c => c.RatingTo == service.ServiceProviderId).ToListAsync();
                    decimal temp = 0;
                    foreach (Rating rating in rate)
                    {
                        if (rating.Ratings != 0)
                        {
                            temp += rating.Ratings;
                        }
                    }
                    if (rate.Count() != 0)
                    {
                        temp /= rate.Count();

                    }
                    service.ratings = temp;
                }
                else
                {

                    profilepicturelist.Add(null);
                }

            }

            ViewBag.profilepicture = profilepicturelist;
            return PartialView("_CustomerServiceHistory", result);
        }
        public IActionResult FavouritePros()
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");

            var p = _context.ServiceRequests.Where(c => c.UserId == userid &&
            (c.Status == 3 || c.Status == 4)).AsEnumerable().GroupBy(x => x.ServiceProviderId).ToList();

            List<User> users = new List<User>();
            List<string> blocklist = new List<string>();
            List<string> favouritelist = new List<string>();
            List<string> profilepicturelist = new List<string>();
            List<int> ratinglist = new List<int>();
            List<int> cleaninglist = new List<int>();
            foreach (var i in p)
            {
                User temp = _context.Users.Find(i.Key);
                users.Add(temp);

                var userpic = _context.Users.Where(c => c.UserId == i.Key).FirstOrDefault();
                if (userpic != null)
                {
                    profilepicturelist.Add(userpic.UserProfilePicture);

                }

                var clean = _context.ServiceRequests.Where(c => c.UserId == userid &&
                c.ServiceProviderId == i.Key && c.Status == 3).Count();
                cleaninglist.Add(clean);

                var blockornot = _context.FavoriteAndBlockeds.Where(c => c.UserId == userid &&
                                               c.TargetUserId == i.Key).FirstOrDefault();
                var totalrating = _context.Ratings.Where(c => c.RatingTo == i.Key).ToList();

                if (totalrating != null)
                {
                    decimal data = 0;
                    foreach (Rating rate in totalrating)
                    {
                        data += rate.Ratings;
                    }
                    if (totalrating.Count() != 0)
                    {
                        data /= totalrating.Count();

                    }
                    ratinglist.Add((int)data);
                }
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
                    if (blockornot.IsFavorite == true)
                    {
                        string value = "yes";
                        favouritelist.Add(value);
                    }
                    else
                    {
                        string value = "no";
                        favouritelist.Add(value);
                    }
                }
                else
                {

                    string value = "no";
                    blocklist.Add(value);
                    favouritelist.Add(value);
                }
            }

            ViewBag.blockedornot = blocklist;
            ViewBag.favouriteornot = favouritelist;
            ViewBag.ratings = ratinglist;
            ViewBag.cleaning = cleaninglist;
            ViewBag.profilepicture = profilepicturelist;

            return PartialView("_CustomerFavouritePros", users);
        }

        public async Task<IActionResult> UnfavouriteProvider(int id)
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var p = await _context.FavoriteAndBlockeds.Where(c => c.UserId == userid && c.TargetUserId == id).FirstOrDefaultAsync();
            if (p == null)
            {
                FavoriteAndBlocked favoriteAndBlocked = new FavoriteAndBlocked
                {
                    UserId = userid,
                    TargetUserId = id,
                    IsBlocked = false,
                    IsFavorite = false

                };

                await _context.FavoriteAndBlockeds.AddAsync(favoriteAndBlocked);
                await _context.SaveChangesAsync();
            }
            else
            {
                p.IsFavorite = false;
                await _context.SaveChangesAsync();
            }
            return FavouritePros();
        }
        public async Task<IActionResult> BlockProvider(int id)
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
            return FavouritePros();
        }
        public async Task<IActionResult> UnBlockProvider(int id)
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var p = await _context.FavoriteAndBlockeds.Where(c => c.UserId == userid && c.TargetUserId == id).FirstOrDefaultAsync();
            if (p == null)
            {
                FavoriteAndBlocked favoriteAndBlocked = new FavoriteAndBlocked
                {
                    UserId = userid,
                    TargetUserId = id,
                    IsBlocked = false,
                    IsFavorite = false

                };

                await _context.FavoriteAndBlockeds.AddAsync(favoriteAndBlocked);
                await _context.SaveChangesAsync();
            }
            else
            {
                p.IsBlocked = false;
                await _context.SaveChangesAsync();
            }
            return FavouritePros();
        }
        public async Task<IActionResult> FavouriteProvider(int id)
        {
            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var p = await _context.FavoriteAndBlockeds.Where(c => c.UserId == userid && c.TargetUserId == id).FirstOrDefaultAsync();
            if (p == null)
            {
                FavoriteAndBlocked favoriteAndBlocked = new FavoriteAndBlocked
                {
                    UserId = userid,
                    TargetUserId = id,
                    IsBlocked = false,
                    IsFavorite = true

                };

                await _context.FavoriteAndBlockeds.AddAsync(favoriteAndBlocked);
                await _context.SaveChangesAsync();
            }
            else
            {
                p.IsFavorite = true;
                await _context.SaveChangesAsync();
            }
            return FavouritePros();
        }
    }
}
