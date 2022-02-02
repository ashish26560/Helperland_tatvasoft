using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Helperland.Models;
using Helperland.Data;

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

                    ModelState.Clear();
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
                }
           
            return View();
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
