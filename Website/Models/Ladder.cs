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
        public List<Game> Games;
        public List<Game> W1Games;
        public List<Game> RunningGames;
        public Ladder(List<BsonDocument> documents)
        {
            Games = new List<Game>();
            foreach (var document in documents)
            {
                Game game = BsonSerializer.Deserialize<Game>(document);
                Games.Add(game);
            }
            W1Games = Games;
            RunningGames = Games;

            //            ladder.Games = GetGamesFromFilestore("Games");
            //ladder.W1Games = GetGamesFromFilestore("W1Games");
            //ladder.RunningGames = GetGamesFromFilestore("RunningGames");


        }
    }



}