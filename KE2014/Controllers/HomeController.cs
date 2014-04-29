using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using KE2014.Models;
using System.Diagnostics;

namespace KE2014.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
           // ViewBag.Message = "Home Page";
            return View();
        }
    }
}
