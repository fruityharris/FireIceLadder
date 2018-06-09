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
        public IMongoDatabase MongoDatabase;
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Ladder()
        {
            var connectionString = "mongodb+srv://Ladder:1_Yatsura@cluster0-j66ax.mongodb.net/admin?retryWrites=true";
            var client = new MongoClient(connectionString);
            MongoDatabase = client.GetDatabase("FireIceLadder");
            IMongoCollection<BsonDocument> collection = MongoDatabase.GetCollection<BsonDocument>("games");
            var document = collection.Find(x => true).First();
            return View(new Models.Ladder(document));

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