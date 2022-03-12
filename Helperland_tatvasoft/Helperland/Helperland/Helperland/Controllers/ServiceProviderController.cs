using cloudscribe.Pagination.Models;
using Helperland.Data;
using Helperland.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var userid = (int)HttpContext.Session.GetInt32("UserId");
            var servicelist = await _context.ServiceRequests.Where(c => c.SpacceptedDate != null && c.Status == 3 && c.ServiceProviderId == userid).ToListAsync();
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
            foreach(var item in servicelist)
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
            var servicelist = await _context.ServiceRequests.Where(c => c.SpacceptedDate == null && c.ServiceStartDate >= DateTime.Now && (c.Status == 1 || c.Status == 5 || c.Status == 7) && c.ZipCode == user.ZipCode).Skip(excluderecords).Take(pagesize).ToListAsync();
            var slist = await _context.ServiceRequests.Where(c => c.SpacceptedDate == null && c.ServiceStartDate >= DateTime.Now && (c.Status == 1 || c.Status == 5 || c.Status == 7) && c.ZipCode == user.ZipCode).ToListAsync();

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

            var p = await _context.ServiceRequests.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();
            if (p.SpacceptedDate == null)
            {
                TempData["responseid"] = "1";
                p.SpacceptedDate = DateTime.Now;
                p.Status = 2;
                p.ModifiedDate = DateTime.Now;
                p.ServiceProviderId = userid;
                p.ModifiedBy = userid;
                await _context.SaveChangesAsync();
                return View("_AcceptReqResponseModal");
            }
            else
            {
                TempData["responseid"] = "2";
                return View("_AcceptReqResponseModal");
            }
            //for conflict of time remaining
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

            var servicelist = await _context.ServiceRequests.Where(c => c.SpacceptedDate != null && (c.Status == 2 || c.Status == 5) && c.ServiceStartDate >= DateTime.Now && c.ServiceProviderId == userid).Skip(excluderecords).Take(pagesize).ToListAsync();
            var slist = await _context.ServiceRequests.Where(c => c.SpacceptedDate != null && (c.Status == 2 || c.Status == 5) && c.ServiceStartDate >= DateTime.Now && c.ServiceProviderId == userid).ToListAsync();

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
