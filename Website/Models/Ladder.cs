using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Website.MiddleTier;

namespace Website.Models
{
    public class Ladder
    {
        public List<GameWeek> GameWeeks;
        public int NumRowsDivFive;
        public Ladder(List<BsonDocument> GameWeeksDoc)
        {
            NumRowsDivFive = 0;
            var lGameWeeks = new List<GameWeek>(); 
            foreach (var document in GameWeeksDoc.OrderByDescending(x => x))
            {
                GameWeek gameweek = BsonSerializer.Deserialize<GameWeek>(document);
                foreach(LadderPlayer lp in gameweek.Ladder)
                {
                    Game PlayerGame = gameweek.Games.Where(x => x.GamePlayers.Exists(y => y.playername == lp.PlayerName)).FirstOrDefault();
                    lp.GameNumber = PlayerGame == null ? (int?)null : PlayerGame.GameNumber;
                }
                lGameWeeks.Add(gameweek);
                NumRowsDivFive = Math.Max(NumRowsDivFive, gameweek.Ladder.Where(x => x.Playing).Count()/5 + 1);
                NumRowsDivFive = Math.Max(NumRowsDivFive, gameweek.Games.Count());

            }

            GameWeeks = lGameWeeks.OrderByDescending(x => x.ProcessingOrder).ToList();
        }
    }



}