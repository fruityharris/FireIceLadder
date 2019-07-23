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
    public class AllPlayers
    {
        public List<LadderPlayer> LadderPlayers;
        public AllPlayers(BsonDocument GameWeekDoc)
        {
                GameWeek gameweek = BsonSerializer.Deserialize<GameWeek>(GameWeekDoc);
            LadderPlayers = gameweek.Ladder;
        }
    }



}