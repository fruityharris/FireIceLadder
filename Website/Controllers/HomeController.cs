using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JSONHelpers;
using static JSONHelpers.Methods;


namespace Website.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Ladder()
        {
            Models.Ladder ladder = new Models.Ladder();
            ladder.Games = GetGamesFromFilestore("Games");
            ladder.W1Games = GetGamesFromFilestore("W1Games");
            ladder.RunningGames = GetGamesFromFilestore("RunningGames");
            return View(ladder);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}