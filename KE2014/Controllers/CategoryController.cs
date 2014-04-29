using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using KE2014.Models;
using System.Diagnostics;

namespace KE2014.Controllers
{
    public class CategoryController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controller = filterContext.Controller;
            if (controller != null)
            {
                var timer = new Stopwatch();
                controller.ViewData["_ActionTimer"] = timer;
                timer.Start();
            }
            base.OnActionExecuting(filterContext);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var controller = filterContext.Controller;
            if (controller != null)
            {
                var timer = (Stopwatch)controller.ViewData["_ActionTimer"];
                if (timer != null)
                {
                    timer.Stop();
                    controller.ViewData["_ElapsedTime"] = timer.Elapsed.TotalSeconds;
                }
            }
        }

        public ActionResult Index(int id)
        {
            Dictionary<string, Frequency> data = new Dictionary<string, Frequency>();

            switch (id)
            {
                case Models.DataContext.ENTERTAINMENT:
                    ViewBag.Category = "影劇娛樂";
                    data = Models.DataContext.LoadData(Models.DataContext.ENTERTAINMENT, 50);
                    break;
                case Models.DataContext.SPORTS:
                    ViewBag.Category = "運動";
                    data = Models.DataContext.LoadData(Models.DataContext.SPORTS, 50);
                    break;
                case Models.DataContext.MAINLAND:
                    ViewBag.Category = "兩岸";
                    data = Models.DataContext.LoadData(Models.DataContext.MAINLAND, 50);
                    break;
                case Models.DataContext.FINANCE:
                    ViewBag.Category = "財經";
                    data = Models.DataContext.LoadData(Models.DataContext.FINANCE, 50);
                    break;
                case Models.DataContext.HEALTH:
                    ViewBag.Category = "保健";
                    data = Models.DataContext.LoadData(Models.DataContext.HEALTH, 50);
                    break;
                case Models.DataContext.POLITICS:
                    ViewBag.Category = "政治";
                    data = Models.DataContext.LoadData(Models.DataContext.POLITICS, 50);
                    break;
                case Models.DataContext.SOCIETY:
                    ViewBag.Category = "社會";
                    data = Models.DataContext.LoadData(Models.DataContext.SOCIETY, 50);
                    break;
            }

            ViewBag.Count = Models.DataContext.DocumentCount;

            return View(data);
        }
    }
}
