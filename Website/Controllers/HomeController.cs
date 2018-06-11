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
                    var connectionString = "mongodb+srv://Ladder:1_Yatsura@cluster0-j66ax.mongodb.net/admin?retryWrites=true";
                    var client = new MongoClient(connectionString);
                    mMongoDatabase = client.GetDatabase("FireIceLadder");

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
            IMongoCollection<BsonDocument> collection = MongoDatabase.GetCollection<BsonDocument>("Games");
            var documents = collection.Find(x => true).ToList();
            return View(new Models.Ladder(documents));

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