using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using KE2014.Models;
using System.Diagnostics;

namespace KE2014.Controllers
{
    public class TFIDFController : Controller
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
                case Models.TFIDFContext.ENTERTAINMENT:
                    ViewBag.Category = "影劇娛樂";
                    data = Models.TFIDFContext.LoadData(Models.TFIDFContext.ENTERTAINMENT, 50);
                    break;
                case Models.TFIDFContext.SPORTS:
                    ViewBag.Category = "運動";
                    data = Models.TFIDFContext.LoadData(Models.TFIDFContext.SPORTS, 50);
                    break;
                case Models.TFIDFContext.MAINLAND:
                    ViewBag.Category = "兩岸";
                    data = Models.TFIDFContext.LoadData(Models.TFIDFContext.MAINLAND, 50);
                    break;
                case Models.TFIDFContext.FINANCE:
                    ViewBag.Category = "財經";
                    data = Models.TFIDFContext.LoadData(Models.TFIDFContext.FINANCE, 50);
                    break;
                case Models.TFIDFContext.HEALTH:
                    ViewBag.Category = "保健";
                    data = Models.TFIDFContext.LoadData(Models.TFIDFContext.HEALTH, 50);
                    break;
                case Models.TFIDFContext.POLITICS:
                    ViewBag.Category = "政治";
                    data = Models.TFIDFContext.LoadData(Models.TFIDFContext.POLITICS, 50);
                    break;
                case Models.TFIDFContext.SOCIETY:
                    ViewBag.Category = "社會";
                    data = Models.TFIDFContext.LoadData(Models.TFIDFContext.SOCIETY, 50);
                    break;
            }

            ViewBag.Count = Models.TFIDFContext.DocumentCount;

            return View(data);
        }
    }
}
