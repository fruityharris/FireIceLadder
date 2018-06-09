﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Website.Models
{
    public class Ladder
    {
        public List<Game> Games;
        public List<Game> W1Games;
        public List<Game> RunningGames;
        public Ladder(BsonDocument document)
        {
            Game game = BsonSerializer.Deserialize<Game>(document);
            Games = new List<Game>();
            Games.Add(game);
            W1Games = Games;
            RunningGames = Games;

            //            ladder.Games = GetGamesFromFilestore("Games");
            //ladder.W1Games = GetGamesFromFilestore("W1Games");
            //ladder.RunningGames = GetGamesFromFilestore("RunningGames");


        }
    }

    [BsonIgnoreExtraElements]
    public class Game
    {
        public int index { get; set; }
        public ObjectId id { get; set; }
        public int? finished { get; set; }
        public int? aborted { get; set; }
        public float seconds_since_update { get; set; }
        public List<string> usernames { get; set; }
        public List<int?> ranks { get; set; }
        public string name { get; set; }
        public List<GamePlayer> GamePlayers { get; set; }
        public List<LadderPlayer> Ladder { get; set; }
        public int? round { get; set; }
        public List<int?> vps { get; set; }
        public List<int?> dropped { get; set; }
        public List<string> factions { get; set; }
        public int LargestGameNumberInLadder;
        public int GameNumber
        {
            get
            {
                return int.Parse(name.Substring(name.IndexOf("G") + 1, name.Length - name.IndexOf("G") - 1)); ;
            }

        }
        public int WeekNumber
        {
            get
            {
                return int.Parse(name.Substring(name.IndexOf("W") + 1, name.IndexOf("G") - name.IndexOf("W") - 1));
            }
        }
    }

    public class GamePlayer
    {
        public int? rank { get; set; }
        public int? dropped { get; set; }
        public string playername { get; set; }
        public double score { get; set; }
        public int currentposition { get; set; }
        public int newposition { get; set; }
        public int processingorder { get; set; }
        public string faction { get; set; }
        public int? vp { get; set; }
    }

    

    public class LadderPlayer
    {
        public int Position;
        public int? OldPosition;
        public string PlayerName;
        public Dictionary<int, int> MarathonScore;

        public LadderPlayer(string lPlayerName, int lPosition)
        {
            MarathonScore = new Dictionary<int, int>();
            PlayerName = lPlayerName;
            Position = lPosition;
        }

        public void AddWinToMarathonScore(int GameNumber)
        {
            if (MarathonScore.ContainsKey(GameNumber))
            {
                MarathonScore[GameNumber] += 1;
            }
            else
            {
                MarathonScore.Add(GameNumber, 1);
            }
        }
    }

}