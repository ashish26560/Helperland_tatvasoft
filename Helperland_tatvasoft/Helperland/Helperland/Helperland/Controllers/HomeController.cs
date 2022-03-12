using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Helperland.Models;
using Helperland.Data;
using System.Net.Mail;
using System.Net;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Helperland.Controllers
{
    public class HomeController : Controller
    {
        //private readonly ILogger<HomeController> _logger;
        private readonly HelperlandContext _context;

        public HomeController(HelperlandContext context)
        {
            _context = context;
        }
        //public Homecontroller(ilogger<homecontroller> logger)
        //{
        //    _logger = logger;
        //}

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgetPassword(User user)
        {
            string resetCode = Guid.NewGuid().ToString();

            var uriBuilder = new UriBuilder
            {
                Scheme = Request.Scheme,
                Host = Request.Host.Host,
                Port = Request.Host.Port ?? -1,
                Path = $"/Home/ResetPassword/{resetCode}"
            };

            var link = uriBuilder.Uri.AbsoluteUri;

            var getUser = (from s in _context.Users where s.Email == user.Email select s).FirstOrDefault();

            if (getUser != null)
            {
                getUser.ResetPasswordCode = resetCode;

                //This line I have added here to avoid confirm password not match issue , as we had added a confirm password property 

                //ObjHelperlandContext.Configuration.ValidateOnSaveEnabled = false;
                _context.SaveChanges();

                var subject = "Password Reset Request";
                var body = "Hi " + getUser.FirstName + ", <br/> You recently requested to reset your password for your account. Click the link below to reset it. " +

                     " <br/><br/><a href='" + link + "'>" + link + "</a> <br/><br/>" +
                     "If you did not request a password reset, please ignore this email or reply to let us know.<br/><br/> Thank you";

                SendEmail(getUser.Email, body, subject);

                TempData["ForgetPositive"] = "Reset password link has been sent to your email id.";
                TempData["Modal"] ="#forget-popup";

                return View("Index");
            }
            else
            {

                TempData["ForgetNegative"] = "User doesn't exists.";
                TempData["Modal"] = "#forget-popup";

                return View("Index");
            }

        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(User user)
        {
          
            var p = _context.Users.Where(c => c.Email == user.Email && c.Password == user.Password).FirstOrDefault();
            if (p != null)
            {
                var name = p.FirstName + " " + p.LastName;
                ViewBag.Name = name;
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("Name", name);
                HttpContext.Session.SetInt32("UserId", p.UserId);
                if (p.UserTypeId == 1)
                {

                    HttpContext.Session.SetString("UserTypeId", p.UserTypeId.ToString());
                    return RedirectToAction("Customer", "User");
                }
                else if (p.UserTypeId == 2)
                {

                    HttpContext.Session.SetString("UserTypeId", p.UserTypeId.ToString());
                    return RedirectToAction("ServiceProvider", "ServiceProvider");
                }
                else
                {
                    return View("Index");
                }
            }
            else
            {
                //ViewBag.loginMessage = "Email or password entered is invalid";

                TempData["LoginMessage"]= "Email or password entered is invalid";
                TempData["Modal"] = "#login-popup";

                return View("Index");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(User user)
        {
           
            return View(user);
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

        public IActionResult ResetPassword(string id)
        {
            //Verify the reset password link
            //Find account associated with this link
            //redirect to reset password page
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            using (HelperlandContext ObjHelperlandContext = new HelperlandContext())
            {
                var user = ObjHelperlandContext.Users.Where(a => a.ResetPasswordCode == id).FirstOrDefault();
                if (user != null)
                {
                    ResetPasswordModel model = new ResetPasswordModel
                    {
                        ResetCode = id
                    };
                    return View(model);
                }
                else
                {
                    return NotFound();
                }
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            var message = "";
            if (ModelState.IsValid)
            {
                using (HelperlandContext ObjHelperlandContext = new HelperlandContext())
                {
                    var user = ObjHelperlandContext.Users.Where(a => a.ResetPasswordCode == model.ResetCode).FirstOrDefault();
                    if (user != null)
                    {
                        //you can encrypt password here, we are not doing it
                        user.Password = model.NewPassword;
                        //make resetpasswordcode empty string now
                        user.ResetPasswordCode = "";
                        //to avoid validation issues, disable it
                      //  ObjHelperlandContext.Configuration.ValidateOnSaveEnabled = false;
                        ObjHelperlandContext.SaveChanges();
                        message = "New password updated successfully";
                    }
                }
            }
            else
            {
                message = "Something invalid";
            }
            ViewBag.Message = message;
            return View(model);
        }
        public IActionResult Becomeprovider()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Becomeprovider(User user)
        {
            if (ModelState.IsValid)
            {
                using (HelperlandContext objHelperlandContext = new HelperlandContext())
                {
                    //ContactU contact = new ContactU
                    //{
                    //    CreatedOn = DateTime.Now
                    //};
                    //contactU.CreatedOn= contact.CreatedOn;    
                    //IQueryable<bool> = select exists(select * from User where Email='" + Email + "' and password='" + Password + "');
                    //IEnumerable<User> query= from emp in objHelperlandContext.Users
                    //                         where emp.Email == email
                    //                         And emp.Password == password;


                    user.UserTypeId = 2;
                    user.CreatedDate = DateTime.Now; 
                    user.ModifiedDate = DateTime.Now;

                    objHelperlandContext.Users.Add(user);
                    objHelperlandContext.SaveChanges();
                    // Int64 id = objEmployee.EmployeeID;
                    ModelState.Clear();
                }
                // return View(objEmployee);

            }
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
