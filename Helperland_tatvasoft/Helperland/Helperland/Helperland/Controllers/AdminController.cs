using cloudscribe.Pagination.Models;
using Helperland.Data;
using Helperland.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Helperland.Controllers
{
    public class AdminController : Controller
    {
        public readonly HelperlandContext _context;

        public AdminController(HelperlandContext context)
        {
            _context = context;
        }
        public IActionResult Admin()
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
        public async Task<IActionResult> CancelRequest(int id)
        {
            var servicerequest = await _context.ServiceRequests.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();
            servicerequest.Status = 4;

            var serviceprovider = await _context.Users.Where(c => c.UserId == servicerequest.ServiceProviderId).FirstOrDefaultAsync();
            var user = await _context.Users.Where(c => c.UserId == servicerequest.UserId).FirstOrDefaultAsync();
            if (servicerequest.ServiceProviderId != null)
            {
                string bodycust = "Hi " + user.FirstName + ", <br/><br/> Service request " + servicerequest.ServiceRequestId +
                    ": is cancelled by a Admin.<br/><br/> Thank you";
                string csubject = "ServiceRequest is cancelled.";
                string bodyprov = "Hi " + serviceprovider.FirstName + ", <br/><br/> Service request " + servicerequest.ServiceRequestId +
                    ": is cancelled by a Admin.<br/><br/> Thank you";
                SendEmail(user.Email, bodycust, csubject);
                SendEmail(serviceprovider.Email, bodyprov, csubject);
            }

            await _context.SaveChangesAsync();
            return ServiceRequestPage();
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
        public async Task<IActionResult> ProviderApprove(int id)
        {
            var user = await _context.Users.Where(c => c.UserId == id).FirstOrDefaultAsync();
            if (user.IsApproved)
            {

                user.IsApproved = false;
            }
            else
            {
                user.IsApproved = true;

            }
            await _context.SaveChangesAsync();
            return UserManagementPage();
        }
        public async Task<IActionResult> UserStatus(int id)
        {
            var user = await _context.Users.Where(c => c.UserId == id).FirstOrDefaultAsync();
            if (user.IsActive)
            {
                user.IsActive = false;
            }
            else
            {
                user.IsActive = true;
            }
            await _context.SaveChangesAsync();
            return UserManagementPage();
        }
        public async Task<IActionResult> UpdateServiceDetails(ServiceRequestDetails serviceRequestDetails)
        {
            var id = serviceRequestDetails.Service.ServiceRequestId;
            var servicerequest = await _context.ServiceRequests.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();
            var serviceaddress = await _context.ServiceRequestAddresses.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();

            var userid = (int)HttpContext.Session.GetInt32("UserId");
            string date = serviceRequestDetails.Service.ServiceStartDate.ToString("yyyy-MM-dd");
            string time = serviceRequestDetails.Service.StartTime.ToString("HH:mm:ss");
            DateTime startDateTime = Convert.ToDateTime(date).Add(TimeSpan.Parse(time));

            serviceRequestDetails.Service.ServiceStartDate = startDateTime;
            servicerequest.ServiceStartDate = serviceRequestDetails.Service.ServiceStartDate;
            servicerequest.Comments = serviceRequestDetails.Service.Comments;
            servicerequest.Status = 5;
            servicerequest.ModifiedDate = DateTime.Now;
            servicerequest.ModifiedBy = userid;

            serviceaddress.AddressLine1 = serviceRequestDetails.ServiceAddress.AddressLine1;
            serviceaddress.AddressLine2 = serviceRequestDetails.ServiceAddress.AddressLine2;
            serviceaddress.City = serviceRequestDetails.ServiceAddress.City;
            serviceaddress.PostalCode = serviceRequestDetails.ServiceAddress.PostalCode;

            await _context.SaveChangesAsync();


            var serviceprovider = await _context.Users.Where(c => c.UserId == servicerequest.ServiceProviderId).FirstOrDefaultAsync();
            var user = await _context.Users.Where(c => c.UserId == servicerequest.UserId).FirstOrDefaultAsync();

            string bodycust = "Hi " + user.FirstName + ", <br/><br/> Service request " + servicerequest.ServiceRequestId +
                ": is rescheduled by a Admin.<br/><br/>the details modified are as below:<br/>Rescheduled Date :" + date +
                "<br/> Rescheduled Start time :" + time +
                "<br/>Comment :" + servicerequest.Comments +
                "<br/><br/>AddressLine1 :" + serviceaddress.AddressLine1 +
                "<br/>AddressLine2 :" + serviceaddress.AddressLine2 +
                "<br/>City :" + serviceaddress.City +
                "<br/>PostalCode :" + serviceaddress.PostalCode +
                "<br/><br/> Thank you";
            string csubject = "ServiceRequest is rescheduled.";

            string bodyprov = "Hi " + serviceprovider.FirstName + ", <br/><br/> Service request " + servicerequest.ServiceRequestId +
                     ": is rescheduled by a Admin.<br/><br/>the details modified are as below:<br/>Rescheduled Date :" + date +
                "<br/> Rescheduled Start time :" + time +
                "<br/>Comment :" + servicerequest.Comments +
                "<br/><br/>AddressLine1 :" + serviceaddress.AddressLine1 +
                "<br/>AddressLine2 :" + serviceaddress.AddressLine2 +
                "<br/>City :" + serviceaddress.City +
                "<br/>PostalCode :" + serviceaddress.PostalCode +
                "<br/><br/> Thank you";

            SendEmail(user.Email, bodycust, csubject);
            SendEmail(serviceprovider.Email, bodyprov, csubject);

            return ServiceRequestPage();
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
        public async Task<IActionResult> EditAndRescheduleModal(int id)
        {
            var servicerequest = await _context.ServiceRequests.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();
            var serviceaddress = await _context.ServiceRequestAddresses.Where(c => c.ServiceRequestId == id).FirstOrDefaultAsync();

            ServiceRequestDetails servicedetails = new ServiceRequestDetails();

            servicedetails.Service = servicerequest;
            servicedetails.ServiceAddress = serviceaddress;


            return PartialView("_AdminEditAndReschedule", servicedetails);
        }
        public IActionResult ServiceRequestSearch()
        {
            List<SelectListItem> customers = new List<SelectListItem>
                {
                    new SelectListItem{Text="Select Customer", Value="0"}
                };

            List<SelectListItem> serviceProvider = new List<SelectListItem>
                {
                    new SelectListItem{Text="Select Service Provider", Value="0"}
                };

            List<User> users = _context.Users.ToList();

            foreach (User user in users)
            {
                if (user.UserTypeId == 1)
                {
                    customers.Add(new SelectListItem { Text = user.FirstName + " " + user.LastName, Value = user.UserId.ToString() });
                }
                if (user.UserTypeId == 2)
                {
                    serviceProvider.Add(new SelectListItem { Text = user.FirstName + " " + user.LastName, Value = user.UserId.ToString() });
                }
            }

            ViewBag.customers = customers;
            ViewBag.serviceproviders = serviceProvider;
            return PartialView("_AdminServReqSearch");
        }
        public IActionResult SearchRequest(int serviceid,
            int customerid,
            int serviceproviderid,
            int statusid,
            DateTime fromdate = default, DateTime todate = default)
        {

            HttpContext.Session.SetInt32("serviceid", serviceid);
            HttpContext.Session.SetInt32("customerid", customerid);
            HttpContext.Session.SetInt32("statusid", statusid);
            HttpContext.Session.SetInt32("serviceproviderid", serviceproviderid);
            HttpContext.Session.SetString("fromdate", JsonSerializer.Serialize(fromdate));
            HttpContext.Session.SetString("todate", JsonSerializer.Serialize(todate));
            return ServiceRequestPage();
        }
        public IActionResult SearchUserRequest(int username,
            int userrole,
            int phonenumber,
            int zipcode,
            DateTime fromdate = default, DateTime todate = default)
        {

            HttpContext.Session.SetInt32("username", username);
            HttpContext.Session.SetInt32("userrole", userrole);
            HttpContext.Session.SetString("phonenumber", phonenumber.ToString());
            HttpContext.Session.SetString("zipcode", zipcode.ToString());
            HttpContext.Session.SetString("fromdateusr", JsonSerializer.Serialize(fromdate));
            HttpContext.Session.SetString("todateusr", JsonSerializer.Serialize(todate));
            return UserManagementPage();
        }

        public IActionResult ServiceRequestPage(int pagenumber = 1, int pagesize = 5)
        {
            List<ServiceRequest> slist = new List<ServiceRequest>();
            var result = new PagedResult<ServiceRequest>();
            HttpContext.Session.SetInt32("pagenumber", pagenumber);
            HttpContext.Session.SetInt32("pagesize", pagesize);
            int excluderecords = (pagenumber * pagesize) - pagesize;
            var services = _context.ServiceRequests.ToList();

            services = _context.ServiceRequests.
                ToList();
            slist = _context.ServiceRequests.ToList();

            //for new pending and rescheduled request
            foreach (ServiceRequest service in services)
            {
                if (service.ServiceStartDate.AddHours(24) <= DateTime.Now && (service.Status == 1 || service.Status == 2 || service.Status == 5))
                {
                    service.Status = 8;
                    _context.SaveChanges();
                }
            }
            if (HttpContext.Session.GetInt32("serviceid") != 0)
            {
                var serviceid = (int)HttpContext.Session.GetInt32("serviceid");
                slist = services.Where(c => c.ServiceRequestId == serviceid).ToList();
                services = services.Where(c => c.ServiceRequestId == serviceid).
                    ToList();

                if (services.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }
            if (HttpContext.Session.GetInt32("customerid") != 0)
            {
                var customerid = (int)HttpContext.Session.GetInt32("customerid");
                slist = services.Where(c => c.UserId == customerid).ToList();
                services = services.Where(c => c.UserId == customerid).
                    ToList();
                if (services.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }
            if (HttpContext.Session.GetInt32("serviceproviderid") != 0)
            {
                var serviceproviderid = (int)HttpContext.Session.GetInt32("serviceproviderid");
                slist = services.Where(c => c.ServiceProviderId == serviceproviderid).ToList();
                services = services.Where(c => c.ServiceProviderId == serviceproviderid).
                    ToList();
                if (services.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }
            if (HttpContext.Session.GetInt32("statusid") != 0)
            {
                var statusid = (int)HttpContext.Session.GetInt32("statusid");
                slist = services.Where(c => c.Status == statusid).ToList();
                services = services.Where(c => c.Status == statusid).
                    ToList();
                if (services.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }
            if (JsonSerializer.Deserialize<DateTime>(HttpContext.Session.GetString("fromdate")) != default)
            {

                var datefrom = HttpContext.Session.GetString("fromdate");

                var fromdate = JsonSerializer.Deserialize<DateTime>(datefrom);
                //var value = session.GetString(key);

                //return value == null ? default : JsonSerializer.Deserialize<T>(value);
                slist = services.Where(c => c.ServiceStartDate >= fromdate).ToList();

                services = services.Where(c => c.ServiceStartDate >= fromdate).
                    ToList();
                if (services.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }
            if (JsonSerializer.Deserialize<DateTime>(HttpContext.Session.GetString("todate")) != default)
            {
                var dateto = HttpContext.Session.GetString("todate");

                var todate = JsonSerializer.Deserialize<DateTime>(dateto);
                //var value = session.GetString(key);

                //return value == null ? default : JsonSerializer.Deserialize<T>(value);
                slist = services.Where(c => c.ServiceStartDate <= todate).ToList();

                services = services.Where(c => c.ServiceStartDate <= todate).
                    ToList();
                if (services.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }

            services = services.Skip(excluderecords).
                    Take(pagesize).ToList();
            result = new PagedResult<ServiceRequest>
            {
                Data = services,
                TotalItems = slist.Count(),
                PageNumber = pagenumber,
                PageSize = pagesize

            };

            foreach (ServiceRequest service in services)
            {
                var customer = _context.Users.Where(c => c.UserId == service.UserId).FirstOrDefault();
                service.CustomerName = customer.FirstName + " " + customer.LastName;
                var address = _context.ServiceRequestAddresses.Where(c => c.ServiceRequestId == service.ServiceRequestId).FirstOrDefault();
                service.ServiceAddress = address.AddressLine1 + "," + address.AddressLine2 + "," + address.City;

                if (service.ServiceProviderId != null)
                {
                    var serviceproviderdetails = _context.Users.Where(c => c.UserId == service.ServiceProviderId).FirstOrDefault();

                    service.ServiceProviderName = serviceproviderdetails.FirstName + " " + serviceproviderdetails.LastName;
                    service.UserProfilePicture = serviceproviderdetails.UserProfilePicture;
                    var rate = _context.Ratings.Where(c => c.RatingTo == service.ServiceProviderId).ToList();
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
            return PartialView("_AdminServReq", result);
        }
        public IActionResult UserManagementSearch()
        {
            List<SelectListItem> usernames = new List<SelectListItem>
                {
                    new SelectListItem{Text="Select user", Value="0"}
                };
            List<User> users = _context.Users.ToList();

            foreach (User user in users)
            {

                usernames.Add(new SelectListItem { Text = user.FirstName + " " + user.LastName, Value = user.UserId.ToString() });

            }
            ViewBag.username = usernames;
            return PartialView("_AdminUsrManagSearch");
        }
        public IActionResult UserManagementPage(int pagenumber = 1, int pagesize = 5)
        {

            HttpContext.Session.SetInt32("pagenumber", pagenumber);
            HttpContext.Session.SetInt32("pagesize", pagesize);

            int excluderecords = (pagenumber * pagesize) - pagesize;
            var users = _context.Users.
                   ToList();
            var ulist = _context.Users.ToList();


            if (HttpContext.Session.GetInt32("username") != 0)
            {
                var username = (int)HttpContext.Session.GetInt32("username");
                ulist = users.Where(c => c.UserId == username).ToList();
                users = users.Where(c => c.UserId == username).
                    ToList();

                if (users.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }
            if (HttpContext.Session.GetInt32("userrole") != 0)
            {
                var userrole = (int)HttpContext.Session.GetInt32("userrole");
                ulist = users.Where(c => c.UserTypeId == userrole).ToList();
                users = users.Where(c => c.UserTypeId == userrole).
                    ToList();

                if (users.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }
            if (HttpContext.Session.GetString("phonenumber") != "0")
            {
                var phonenumber = HttpContext.Session.GetString("phonenumber");

                users = users.Where(c => c.Mobile != null).ToList();
                ulist = users.Where(c => c.Mobile.Contains(phonenumber)).ToList();
                users = users.Where(c => c.Mobile.Contains(phonenumber)).
                    ToList();

                if (users.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }
            if (HttpContext.Session.GetString("zipcode") != "0")
            {
                var zipcode = HttpContext.Session.GetString("zipcode");
                users = users.Where(c => c.ZipCode != null).ToList();

                ulist = users.Where(c => c.ZipCode.Contains(zipcode)).ToList();
                users = users.Where(c => c.ZipCode.Contains(zipcode)).ToList();

                if (users.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }

            if (JsonSerializer.Deserialize<DateTime>(HttpContext.Session.GetString("fromdateusr")) != default)
            {

                var usrdatefrom = HttpContext.Session.GetString("fromdateusr");

                var fromdateusr = JsonSerializer.Deserialize<DateTime>(usrdatefrom);
                //var value = session.GetString(key);

                //return value == null ? default : JsonSerializer.Deserialize<T>(value);
                ulist = users.Where(c => c.CreatedDate >= fromdateusr).ToList();

                users = users.Where(c => c.CreatedDate >= fromdateusr).
                    ToList();
                if (users.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }
            if (JsonSerializer.Deserialize<DateTime>(HttpContext.Session.GetString("todateusr")) != default)
            {
                var usrdateto = HttpContext.Session.GetString("todateusr");

                var todateusr = JsonSerializer.Deserialize<DateTime>(usrdateto);
                //var value = session.GetString(key);

                //return value == null ? default : JsonSerializer.Deserialize<T>(value);
                ulist = users.Where(c => c.CreatedDate <= todateusr).ToList();

                users = users.Where(c => c.CreatedDate <= todateusr).
                    ToList();
                if (users.Count == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { message = "Data Not Found." });
                }
            }
            users = users.Skip(excluderecords).Take(pagesize).
                   ToList();
            foreach (User user in users)
            {
                var zipcode = _context.Zipcodes.Where(c => c.ZipcodeValue == user.ZipCode).FirstOrDefault();
                if (zipcode != null)
                {
                    var city = _context.Cities.Where(c => c.Id == zipcode.CityId).FirstOrDefault();

                    if (city != null)
                    {
                        user.City = city.CityName;
                    }
                }
            }
            var result = new PagedResult<User>
            {
                Data = users,
                TotalItems = ulist.Count(),
                PageNumber = pagenumber,
                PageSize = pagesize

            };
            return PartialView("_AdminUsrManag", result);
        }
    }
}
