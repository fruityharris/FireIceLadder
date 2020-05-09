using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.IO;
using Newtonsoft.Json;

namespace Website.MiddleTier
{

    public class UpdateLadder
    {
        public static void Update(IMongoDatabase db)
        {
            //            db.DropCollection("Games");
            //            db.DropCollection("W1Games");
            //            db.DropCollection("RunningGames");
            db.DropCollection("GameWeeks");
            Ladder.Clear();
            CurrentPlayers.Clear();
            OldPlayers.Clear();
     

            List<Game> AllGames = GetGamesFromSnellman("FireIceLadderW%");


            // First create GameWeek 1

            GameWeek GW1 = new GameWeek();
            GW1.Name = "Week 1";
            GW1.ProcessingOrder = 1;
            GW1.Games = new List<Game>();

            List<Game> FinishedW1Games = AllGames.Where(x => (x.WeekNumber == 1 && x.finished == 1 && x.aborted == 0))
                                                                    .OrderByDescending(x => x.seconds_since_update).ToList();


            foreach (Game GameData in FinishedW1Games)
            {
                ProcessGame(GameData);
                GameData.index = GW1.Games.Count();
                GW1.Games.Add(GameData);
            }

            for (int rank = 1; rank < 5; rank++)
            {
                foreach (Game GameData in FinishedW1Games)
                {
                    foreach (GamePlayer gameplayer in GameData.GamePlayers.Where(x => x.rank == rank && x.dropped != 1).OrderBy(x => x.playername))
                    {
                        Ladder.Add(new LadderPlayer(gameplayer.playername, Ladder.Count() + 1));
                    }
                }
            }


            GW1.Ladder = Ladder;
            SaveGameWeek(GW1,  db);
            foreach (LadderPlayer LadderPlayer in Ladder)
            {
                LadderPlayer.OldPosition = LadderPlayer.Position;
            }


            // Now gameweeks 2 - 7

            int i = 2;
            GameWeek CurrentGW = new GameWeek();
            CurrentGW.ProcessingOrder = i;
            CurrentGW.Name = "Week ~" + i.ToString();
            CurrentGW.Games = new List<Game>();
            foreach (Game GameData in AllGames.Where(x => (x.aborted == 0 && x.WeekNumber > 1 && x.WeekNumber < 8))
                                                                    .OrderByDescending(x => x.seconds_since_update))
            {
                ProcessGame(GameData);

                if (CurrentGW.Games.Count() > 15)
                {
                    CurrentGW.Ladder = Ladder;
                    SaveGameWeek(CurrentGW, db);
                    foreach (LadderPlayer LadderPlayer in Ladder)
                    {
                        LadderPlayer.OldPosition = LadderPlayer.Position;
                    }
                    i = Math.Min(7, i + 1);
                    CurrentGW.Name = "Week ~" + i.ToString();
                    CurrentGW.Games.Clear();
                    CurrentGW.ProcessingOrder = i;
                }
                GameData.index = CurrentGW.Games.Count();
                CurrentGW.Games.Add(GameData);


                if (GameData.finished == 1)
                {
                    AddGameToLadder(GameData);
                }
            }
            CurrentGW.Ladder = Ladder;
            SaveGameWeek(CurrentGW, db);
            foreach (LadderPlayer LadderPlayer in Ladder)
            {
                LadderPlayer.OldPosition = LadderPlayer.Position;
            }


            // Now gameweeks 8+
            int CurrentWeekNumber = 8;
            
            CurrentPlayers = AllGames.Where(x => (x.aborted == 0 && x.WeekNumber == CurrentWeekNumber)).SelectMany(x => x.usernames).ToList();
            CurrentGW.ProcessingOrder = CurrentWeekNumber;
            CurrentGW.Name = "Week " + CurrentWeekNumber.ToString();
            CurrentGW.Games.Clear();
            foreach (Game GameData in AllGames.Where(x => (x.aborted == 0 && x.WeekNumber >= 8 && x.WeekNumber < 38))
                                                                                .OrderBy(x => x.WeekNumber).ThenByDescending(x => x.GameNumber))
            {
                ProcessGame(GameData);

                if (GameData.WeekNumber != CurrentWeekNumber)
                {

                    CurrentGW.Ladder = PurgeNonPlayers(Ladder, AllGames, CurrentWeekNumber);
                    SaveGameWeek(CurrentGW, db);
                    foreach (LadderPlayer LadderPlayer in Ladder)
                    {
                        LadderPlayer.OldPosition = LadderPlayer.Position;
                    }
                    CurrentWeekNumber = GameData.WeekNumber;
                    CurrentPlayers = AllGames.Where(x => (x.aborted == 0 && x.WeekNumber == CurrentWeekNumber)).SelectMany(x => x.usernames).ToList();
                    CurrentGW.Name = "Week " + CurrentWeekNumber.ToString();
                    CurrentGW.Games.Clear();
                    CurrentGW.ProcessingOrder = CurrentWeekNumber;
                }

                GameData.index = CurrentGW.Games.Count();
                CurrentGW.Games.Add(GameData);


                if (GameData.finished == 1)
                {
                    AddGameToLadder(GameData);
                }
            }

            CurrentGW.Ladder = PurgeNonPlayers(Ladder, AllGames, CurrentWeekNumber);
            SaveGameWeek(CurrentGW, db);
            foreach (LadderPlayer LadderPlayer in Ladder)
            {
                LadderPlayer.OldPosition = LadderPlayer.Position;
            }


            // Now gameweeks 38 onwards
            // Process all games in a gameweek at once and then order everyone properly at the end
            // No longer purge or drop down non players - just hide them somehow (probably in the front end)
            while (true)
            {
                CurrentWeekNumber +=1;
                CurrentGW.Games = AllGames.Where(x => (x.aborted == 0 && x.WeekNumber == CurrentWeekNumber)).OrderBy(x=>x.GameNumber).ToList();
                if(CurrentGW.Games.Count == 0)
                {
                    break;
                }
                CurrentGW.Name = "Week " + CurrentWeekNumber.ToString();
                CurrentGW.ProcessingOrder = CurrentWeekNumber;
                int InitialLadderSize = Ladder.Max(x => x.Playing ? x.Position : 0);
                if (InitialLadderSize == 0)
                {
                    InitialLadderSize = Ladder.Max(x => x.Position);
                }
                foreach (LadderPlayer LP in Ladder)
                {
                    LP.Playing = false;
                    LP.OldPosition = LP.Position;
                    // see note below
                    if (CurrentWeekNumber == 38)
                    {
                        LP.TemporaryPositionDouble = LP.Position;
                    }

                }

                foreach (Game GameData in CurrentGW.Games.OrderByDescending(x => x.GameNumber))
                {
                    ProcessGame(GameData);
                    GameData.index = 0 - GameData.GameNumber;
                    UpdateLadderBasedOnGame(GameData, InitialLadderSize + 1);

                }

                // then put everyone in the right place on the ladder
                // NOTE that for GW 38 we have to account for the fact everyone's moving a long way up the ladder. This means moving non-players up too, hence the "|| CurrentWeekNumber = 38". This works because we've set their temporary position above too.
                int newpos = 1;
                foreach (LadderPlayer LadderPlayer in Ladder.Where(x => x.Playing || CurrentWeekNumber == 38).OrderBy(x => x.TemporaryPositionDouble).ThenBy(x => x.OldPosition).ThenBy(x => x.Games.Last().Value.GameNumber).ThenBy(x => x.Games.Count()))
                {
                    LadderPlayer.Position = newpos;
                    if (LadderPlayer.Playing)
                    {
                        newpos++;
                    }
                }

                CurrentGW.Ladder = Ladder;
                SaveGameWeek(CurrentGW, db);

            }

        }

        public static void UpdateLadderBasedOnGame(Game game, int DefaultLadderPosition)
        {
            foreach (GamePlayer gameplayer in game.GamePlayers)
            {
                if (game.finished == 1)
                {
                    // add new people to ladder. No longer worrying about people who drop in their first game - just add them to the ladder anyway
                    if (!Ladder.Exists(x => x.PlayerName == gameplayer.playername))
                    {
                        Ladder.Add(new LadderPlayer(gameplayer.playername, DefaultLadderPosition));
                        gameplayer.currentposition = DefaultLadderPosition;

                    }
                    else
                    {
                        gameplayer.currentposition = Ladder.Find(x => x.PlayerName == gameplayer.playername).Position;

                    }

                    AddPositionalScores(game, false);


                    // Add the game info to the ladder player and set their temporary "position" ready for ordering
                    PlayerGameInfo Info = new PlayerGameInfo();
                    Info.GameNumber = game.GameNumber;
                    Info.WeekNumber = game.WeekNumber;
                    Info.Finished = true;
                    Info.Dropped = false;
                    Info.Faction = gameplayer.faction;
                    Info.Rank = gameplayer.rank;
                    LadderPlayer LP = Ladder.Find(x => x.PlayerName == gameplayer.playername);
                    LP.AddGameInfo(Info);
                    LP.Playing = true;
                    LP.TemporaryPositionDouble = LP.Position - gameplayer.score;
                    LP.AddTogmgMarathonScore(game.GameNumber, (int)gameplayer.rank);

                }
                else
                {
                    LadderPlayer LP = Ladder.Find(x => x.PlayerName == gameplayer.playername);
                    if (LP != null)
                    {
                        LP.Playing = true;
                        LP.TemporaryPositionDouble = (double)(LP.Position);
                    }
                }
            }

        }

        // You probably want UseIntegerDivision = false
        public static void AddPositionalScores(Game game, bool UseIntegerDivision)
        {

            Dictionary<int, int> ScoresList = GetScoresList(game.usernames.Count());

            for (int i = 1; i <= 4; i++)
            {
                double score = 0;
                int TiedPlayers = game.GamePlayers.Where(x => x.rank == i).Count();

                if (TiedPlayers == 0)
                    continue;

                for (int j = i; j < i + TiedPlayers; j++)
                {
                    score += ScoresList[j];
                }
                score = score / TiedPlayers;

                foreach (GamePlayer gameplayer in game.GamePlayers.Where(x => x.rank == i))
                {
                    gameplayer.score = gameplayer.dropped == 1 ? -4 : score;
                    if (UseIntegerDivision)
                    {
                        gameplayer.score = gameplayer.score * Math.Max(1, gameplayer.currentposition / 20);
                    }
                    else
                    {
                        if(gameplayer.currentposition > 20)
                        {
                            gameplayer.score = gameplayer.score * gameplayer.currentposition;
                            gameplayer.score = gameplayer.score / 20;

                        }
                    }
                }
            }

        }

        // Legacy method for GW < 38
        public static List<LadderPlayer> PurgeNonPlayers(List<LadderPlayer> Ladder, List<Game> Games, int CurrentWeekNumber)
        {
            if (CurrentWeekNumber > 19)
            {
                List<Game> RecentGames = Games.Where(x => x.aborted == 0 && x.WeekNumber >= CurrentWeekNumber - 4 && x.WeekNumber <= CurrentWeekNumber).ToList();
                List<GamePlayer> RecentPlayers = RecentGames.SelectMany(x => x.GamePlayers).ToList();
                List<string> RecentPlayerNames = RecentPlayers.Select(x => x.playername).ToList();
                OldPlayers.AddRange(Ladder.Where(x => !RecentPlayerNames.Contains(x.PlayerName) && x.Position > Ladder.Count * .75));
                Ladder.RemoveAll(x => !RecentPlayerNames.Contains(x.PlayerName) && x.Position > Ladder.Count * .75);

                int position = 1;
                foreach (LadderPlayer LadderPlayer in Ladder.OrderBy(x => x.Position))
                {
                    LadderPlayer.Position = position;
                    position++;
                }
            }


            return Ladder;
        }

        public static List<Game> GetGamesFromSnellman(string Pattern)
        {

            string url = "http://terra.snellman.net/app/list-games/by-pattern/" + Pattern;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse response = request.GetResponse();

            SnellmanData Data;

            using (var sr = new StreamReader(response.GetResponseStream()))
            {
            JsonSerializer serializer = new JsonSerializer();
            Data = (SnellmanData)serializer.Deserialize(sr, typeof(SnellmanData));
            }

            return Data.games.Select(X => X.ConvertToGame()).ToList();
        }

        public static void SaveGame(Game game, int ProcessingOrder, string CollectionName, IMongoDatabase db)
        {
            game.index = ProcessingOrder;
            db.GetCollection<BsonDocument>(CollectionName).InsertOne(game.ToBsonDocument());
        }

        public static void SaveGameWeek(GameWeek GW, IMongoDatabase db)
        {
            db.GetCollection<BsonDocument>("GameWeeks").InsertOne(GW.ToBsonDocument());
        }

        static List<LadderPlayer> Ladder = new List<LadderPlayer>();
        static List<LadderPlayer> OldPlayers = new List<LadderPlayer>();
        static List<string> CurrentPlayers = new List<string>();
        static int LargestGameNumber = 0;




        static Dictionary<int, int> GetScoresList(int playercount)
        {
            if (playercount == 3)
            {
                return new Dictionary<int, int>() { { 1, 4 }, { 2, 1 }, { 3, -1 } };
            }
            else return new Dictionary<int, int>() { { 1, 5 }, { 2, 2 }, { 3, 0 }, { 4, -1 } };
        }

        static void ProcessGame(Game game)
        {
            //get everyone's info
            game.GamePlayers = new List<GamePlayer>();
            for (int i = 0; i < game.usernames.Count(); i++)
            {

                GamePlayer gp = new GamePlayer();
                gp.playername = game.usernames[i];
                gp.rank = game.ranks[i];
                gp.faction = game.factions[i];
                gp.vp = game.vps[i];
                gp.dropped = game.dropped[i];
                game.GamePlayers.Add(gp);

            }
            LargestGameNumber = Math.Max(game.GameNumber, LargestGameNumber);
            game.LargestGameNumberInLadder = LargestGameNumber;
        }

        // Legacy method for GW < 38
        static void AddGameToLadder(Game game)
        {
            // add everyone to the ladder in order
            int ProcessingOrder = 1;


            // if the player dropped in their first game, don't add them to the ladder
            foreach (GamePlayer gameplayer in game.GamePlayers.Where(x => x.dropped != 1).OrderBy(x => x.rank).ThenBy(x => x.playername))
            {
                if (!Ladder.Exists(x => x.PlayerName == gameplayer.playername))
                {
                    if (OldPlayers.Exists(x => x.PlayerName == gameplayer.playername))
                    {
                        LadderPlayer Player = OldPlayers.Where(x => x.PlayerName == gameplayer.playername).First();
                        Player.Position = Ladder.Count() + 1;
                        Ladder.Add(Player);
                    }
                    else
                    {
                        Ladder.Add(new LadderPlayer(gameplayer.playername,Ladder.Count() + 1));
                    }
                }
                gameplayer.processingorder = ProcessingOrder;
                ProcessingOrder++;

                PlayerGameInfo Info = new PlayerGameInfo();
                Info.GameNumber = game.GameNumber;
                Info.WeekNumber = game.WeekNumber;
                Info.Finished = true;
                Info.Dropped = false;
                Info.Faction = gameplayer.faction;
                Info.Rank = gameplayer.rank;
                Ladder.Find(x => x.PlayerName == gameplayer.playername).AddGameInfo(Info);

                if (game.WeekNumber >= 5)
                {
                    Ladder.Find(x => x.PlayerName == gameplayer.playername).AddTogmgMarathonScore(game.GameNumber, (int)gameplayer.rank);
                }

            }


            // give everyone a score 
            AddPositionalScores(game, true);

            // set people's new positions
            int TotalPlayers = Ladder.Count();

            // get their new position 
            foreach (GamePlayer gameplayer in game.GamePlayers.OrderBy(x => x.processingorder))
            {
                /* this is the description of how to find your new position
                 * 
                 * To determine ladder positions, the following algorithm is applied to each game in order of completion:
                 * Anyone in the game who's not yet on the ladder, gets added at the bottom of the ladder, in order of finishing position (alphabetically in the case of a tie)
                 * Everyone in the game gets their new ladder position calculated as follows (L is current ladder position, S is the score as determined by the table to the right)
                 * For positive S: New L = Floor(Min(L *(1 - S/20), L - S))
                 * For negative S: New L = Ceiling(Max(L *(1 - S/20), L - S))
                 * If N players from the game receive the same new position, they are allocated positions L through L+ N - 1 ordered by position in the game and then alphabetically
                 * Ls are adjusted such that they are between 1 and (# of players on ladder)
                */

                // if the player dropped in their first game, they may not be on the ladder at all
                if (Ladder.Exists(x => x.PlayerName == gameplayer.playername))
                {
                    gameplayer.currentposition = Ladder.Find(x => x.PlayerName == gameplayer.playername).Position;
                    double ScoreRelative = Convert.ToDouble(gameplayer.currentposition) * gameplayer.score / 20;
                    int PlacesToMove = 0;

                    if (gameplayer.score < 0)
                    {
                        PlacesToMove = Convert.ToInt32(Math.Floor(Math.Min(gameplayer.score, ScoreRelative)));
                    }
                    else
                    {
                        PlacesToMove = Convert.ToInt32(Math.Ceiling(Math.Max(gameplayer.score, ScoreRelative)));
                    }

                    if (CurrentPlayers.Count() == 0 || PlacesToMove <= 0 || game.WeekNumber > 8)
                    {
                        gameplayer.newposition = Math.Max(gameplayer.currentposition - PlacesToMove, 1);
                    }
                    else
                    {
                        gameplayer.newposition = gameplayer.currentposition;
                        int j = 0;
                        while (j < PlacesToMove && gameplayer.newposition > 1)
                        {
                            gameplayer.newposition--;
                            if (gameplayer.newposition < 1)
                            {
                                j++;
                            }
                            else
                            {
                                j = j + Ladder.Where(x => x.Position == gameplayer.newposition && CurrentPlayers.Contains(x.PlayerName)).Count();
                            }
                        }
                    }

                    // resolve duplicates - shunt them down 1 if someone has the same position
                    while (game.GamePlayers.Where(x => x.processingorder < gameplayer.processingorder && x.newposition == gameplayer.newposition).Count() > 0)
                    {
                        gameplayer.newposition += 1;
                    }
                }
            }

            // The resolve duplicates code above may have resulted in a gap at the bottom of the ladder - fix this.
            // finally, we can update them on the ladder
            foreach (GamePlayer gameplayer in game.GamePlayers.OrderByDescending(x => x.processingorder))
            {
                // if the player dropped in their first game, they may not be on the ladder at all
                if (Ladder.Exists(x => x.PlayerName == gameplayer.playername))
                {
                    if (gameplayer.newposition > TotalPlayers)
                    {
                        gameplayer.newposition = TotalPlayers;
                    }

                    while (game.GamePlayers.Where(x => x.processingorder > gameplayer.processingorder && x.newposition == gameplayer.newposition).Count() > 0)
                    {
                        gameplayer.newposition -= 1;
                    }

                    Ladder.Find(x => x.PlayerName == gameplayer.playername).Position = gameplayer.newposition;
                }
            }



            // update the rest of the ladder
            int newpos = 1;
            foreach (LadderPlayer LadderPlayer in Ladder.OrderBy(x => x.Position))
            {
                if (game.GamePlayers.Select(x => x.playername).ToList().Contains(LadderPlayer.PlayerName))
                    continue;

                while (game.GamePlayers.Where(x => x.newposition == newpos).Count() > 0)
                {
                    newpos++;
                }
                if (LadderPlayer.Position != newpos)
                {
                    LadderPlayer.Position = newpos;
                }
                newpos++;
            }

        }

    }
}