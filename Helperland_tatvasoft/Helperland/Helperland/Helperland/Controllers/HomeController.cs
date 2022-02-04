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

namespace Helperland.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(User user)
        {
            
                using (HelperlandContext ObjHelperlandContext = new HelperlandContext())
                {
               
                    string email = user.Email;

                    var p = ObjHelperlandContext.Users.Where(c => c.Email == email && c.Password == user.Password).ToList();

                    //ModelState.Clear();
                    if (p.Count == 1)
                    {
                        if (p.FirstOrDefault().UserTypeId == 1)
                        {
                            return RedirectToAction("Customer", "User");
                        } 
                        if (p.FirstOrDefault().UserTypeId == 2)
                        {
                            return RedirectToAction("Serviceprovider", "User");
                        }
                    }
                    else 
                    {
                    ViewBag.Message = "Email or password entered is invalid";

                    }

                //forget password
                string resetCode = Guid.NewGuid().ToString();
                // var verifyUrl = "/Account/ResetPassword/" + resetCode;

                var uriBuilder = new UriBuilder
                {
                    Scheme = Request.Scheme,
                    Host = Request.Host.Host,
                    Port = Request.Host.Port ?? -1,
                    Path = $"/Home/ResetPassword/{resetCode}"
                };

                var link = uriBuilder.Uri.AbsoluteUri;

                //var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

                var getUser = (from s in ObjHelperlandContext.Users where s.Email == user.Email select s).FirstOrDefault();
                if (getUser != null)
                {
                    getUser.ResetPasswordCode = resetCode;

                    //This line I have added here to avoid confirm password not match issue , as we had added a confirm password property 

                   //ObjHelperlandContext.Configuration.ValidateOnSaveEnabled = false;
                    ObjHelperlandContext.SaveChanges();

                    var subject = "Password Reset Request";
                    var body = "Hi " + getUser.FirstName + ", <br/> You recently requested to reset your password for your account. Click the link below to reset it. " +

                         " <br/><br/><a href='" + link + "'>" + link + "</a> <br/><br/>" +
                         "If you did not request a password reset, please ignore this email or reply to let us know.<br/><br/> Thank you";

                    SendEmail(getUser.Email, body, subject);

                    ViewBag.Message = "Reset password link has been sent to your email id.";
                }
                else
                {
                    ViewBag.Message = "User doesn't exists.";
                    return View();
                }

            }
           
            return View();
        }

        private void SendEmail(string emailAddress, string body, string subject)
        {
            using (MailMessage mm = new MailMessage("ashish.chauhan93133@gmail.com", emailAddress))
            {
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
