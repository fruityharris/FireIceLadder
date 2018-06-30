using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Bson.Serialization;


namespace Website.Controllers
{

        public class HomeController : Controller
    {
        public IMongoDatabase MongoDatabase
        {
            get {
                if (mMongoDatabase == null)
                    {

                    string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["MongoDB"].ToString(); 
                    var client = new MongoClient(connectionString);
                    mMongoDatabase = client.GetDatabase(System.Configuration.ConfigurationManager.AppSettings["DBName"].ToString());

                }

                return mMongoDatabase;
            }
        }

        private IMongoDatabase mMongoDatabase;


        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Ladder()
        {
            List<BsonDocument> GameWeeks = MongoDatabase.GetCollection<BsonDocument>("GameWeeks").Find(x => true).ToList();
            return View(new Models.Ladder(GameWeeks));

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

        public ActionResult UpdateLadder()
        {
            MiddleTier.UpdateLadder.Update(MongoDatabase);

            return RedirectToRoute(new { controller = "Home", action = "Ladder" });
        }
    }
}