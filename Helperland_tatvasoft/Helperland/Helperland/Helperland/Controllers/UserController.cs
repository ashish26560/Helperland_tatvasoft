using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Helperland.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Customer()
        {
            return View();
        }

        public IActionResult Serviceprovider()
        {
            return View();
        }
    }
}
