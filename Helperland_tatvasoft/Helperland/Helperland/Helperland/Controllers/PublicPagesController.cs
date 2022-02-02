using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helperland.Models;
using Helperland.Data;

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
        public IActionResult Contact(ContactU contactU,String Lastname )
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

                    string lastname = Convert.ToString(Lastname);
                    contactU.Name=(contactU.Name+" "+lastname);

                    contactU.CreatedOn = DateTime.Now;

                    objHelperlandContext.ContactUs.Add(contactU);
                    objHelperlandContext.SaveChanges();
                   // Int64 id = objEmployee.EmployeeID;
                    ModelState.Clear();
                }
               // return View(objEmployee);

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
                using (HelperlandContext objHelperlandContext = new HelperlandContext())
                {
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
                }
                // return View(objEmployee);

            }
            return View();
        }
    }
}
