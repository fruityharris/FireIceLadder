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
        public Ladder(List<BsonDocument> AllGamesDoc, List<BsonDocument> W1GamesDoc, List<BsonDocument> RunningGamesDoc)
        {
            Games = new List<Game>();
            W1Games = new List<Game>();
            RunningGames = new List<Game>();
            foreach (var document in AllGamesDoc.OrderByDescending(x => x))
            {
                Game game = BsonSerializer.Deserialize<Game>(document);
                Games.Add(game);
            }

            foreach (var document in W1GamesDoc)
            {
                Game game = BsonSerializer.Deserialize<Game>(document);
                W1Games.Add(game);
            }

            foreach (var document in RunningGamesDoc)
            {
                Game game = BsonSerializer.Deserialize<Game>(document);
                RunningGames.Add(game);
            }



        }
    }



}