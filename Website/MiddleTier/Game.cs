﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace Website.MiddleTier
{
    public class SnellmanGame
    {
        public string id { get; set; }
        public int? finished { get; set; }
        public int? aborted { get; set; }
        public float seconds_since_update { get; set; }
        public List<string> usernames { get; set; }
        public List<int?> ranks { get; set; }
        public int? round { get; set; }
        public List<int?> vps { get; set; }
        public List<int?> dropped { get; set; }
        public List<string> factions { get; set; }



        public Game ConvertToGame()
        {
            var obj = this;
            Game output = new Game()
            {
                name = obj.id,
                finished = obj.finished,
                aborted = obj.aborted,
                seconds_since_update = obj.seconds_since_update,
                usernames = obj.usernames,
                ranks = obj.ranks,
                round = obj.round,
                vps = obj.vps,
                dropped = obj.dropped,
                factions = obj.factions
            };
            return output;
        }
    }


    [BsonIgnoreExtraElements]
    public class SnellmanData
    {
        public List<SnellmanGame> games { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class GameWeek
    {
        public List<Game> Games;
        public string Name;
        public List<LadderPlayer> Ladder;
        public int ProcessingOrder;
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


    public class PlayerGameInfo
    {
        public int GameNumber;
        public int WeekNumber;
        public bool Finished;
        public bool Dropped;
        public string Faction;
        public int? Rank;
    }

    public class LadderPlayer
    {
        public int Position;
        public int? OldPosition;
        public double TemporaryPositionDouble;
        public string PlayerName;
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<int, PlayerGameInfo> Games;

        public bool Playing;
        public int gmgMarathonScore;
        public int? GameNumber;

        public LadderPlayer(string lPlayerName, int lPosition)
        {
            Games = new Dictionary<int, PlayerGameInfo>();
            PlayerName = lPlayerName;
            Position = lPosition;
            gmgMarathonScore = 0;
        }

        public void AddTogmgMarathonScore(int GameNumber, int rank)
        {
            gmgMarathonScore += 1000 / (GameNumber * rank);
        }

        public void AddGameInfo(PlayerGameInfo Info)
        {
            Games.Add(Info.WeekNumber, Info);
        }
        
    }
    
}