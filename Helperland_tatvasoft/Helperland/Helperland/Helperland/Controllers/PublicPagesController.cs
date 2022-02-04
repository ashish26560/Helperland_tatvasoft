using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helperland.Models;
using Helperland.Data;
using System.IO;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting;

namespace Helperland.Controllers
{
    public class PublicPagesController : Controller
    {
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(ContactU contactU,String Lastname  )
        {
            if (ModelState.IsValid)
            {
                using HelperlandContext objHelperlandContext = new HelperlandContext();
          
                string lastname = Convert.ToString(Lastname);
                contactU.Name = (contactU.Name + " " + lastname);
                contactU.Message = ("This mail is sent by " + contactU.Name + ", <br/><br/> Email:-  "+ contactU.Email+ "<br/> Phone number:-  " + contactU.PhoneNumber + "<br/><br/> Query:-  " +contactU.Message );

                contactU.CreatedOn = DateTime.Now;
               // contactU.FileName = Path.GetFileName(contactU.AttachmentFile.FileName);
                //if (contactU.AttachmentFile != null)
                //{
                //    string folder = "~/media/";
                //    folder += Guid.NewGuid().ToString() + "_" + contactU.AttachmentFile.FileName;
                //    //string serverFolder = Path.Combine(IWebHostEnvironment.WebRootPath, folder);
                //   // contactU.AttachmentFile.CopyToAsync(new FileStream(serverFolder, FileMode.Create));
                //    contactU.FileName = folder;

                //}

                objHelperlandContext.ContactUs.Add(contactU);
                objHelperlandContext.SaveChanges();
              
                //sending mail logic starts here
                using MailMessage mm = new MailMessage("ashish.chauhan93133@gmail.com", "ashishchauhan26560@gmail.com");

                
                //MailAddress by = new MailAddress("ashishchauhan26560@gmail.com");
                //String to = new string("ashish26560@gmail.com");
                

                mm.Subject = contactU.Subject;
                mm.Body = contactU.Message;
                //mm.From = by;
                //mm.To.Add(to);
                //mm.To = to;

                // mm.To = recepiant;

                //if (contactU.AttachmentFile.Length > 0)
                //{
                //    string fileName = Path.GetFileName(contactU.AttachmentFile.FileName);
                //    mm.Attachments.Add(new Attachment(contactU.AttachmentFile.OpenReadStream(), fileName));
                //}

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
