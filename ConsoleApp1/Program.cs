using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using JSONHelpers;
using static JSONHelpers.Methods;


namespace ConsoleApp1
{
    class Program
    {
        static List<LadderPlayer> Ladder = new List<LadderPlayer>();

        static void Main(string[] args)
        {

            CreateNewDirectory();
            List<Game> W1Games = StoreAndReturnGamesFromSnellman("FireIceLadderW1G%", "W1games");
            List<Game> AllGames = StoreAndReturnGamesFromSnellman("FireIceLadderW%", "Allgames");


            List<Game> FinishedW1Games = W1Games.Where(x => (x.finished == 1 && x.aborted == 0))
                                                                    .OrderByDescending(x => x.seconds_since_update).ToList();

            for (int rank = 1; rank < 5; rank++)
            {
                foreach (Game GameData in FinishedW1Games)
                {
                    ProcessGame(GameData);
                    WriteGameToConsole(GameData);
                    foreach (GamePlayer gameplayer in GameData.GamePlayers.Where(x => x.rank == rank && x.dropped != 1).OrderBy(x => x.playername))
                    {
                        Ladder.Add(new LadderPlayer(gameplayer.playername, Ladder.Count() + 1));
                    }
                }
            }


            int W1ProcessingOrder = 0;
            foreach (Game GameData in FinishedW1Games)
            {
                W1ProcessingOrder++;
                GameData.Ladder = Ladder;
                SaveGame(GameData, W1ProcessingOrder, "W1Games");
            }

            WriteLadderToConsole();


            int GameProcessingOrder = 0;
            foreach (Game GameData in AllGames.Where(x => (x.finished == 1 && x.aborted == 0 && x.id.Substring(0, 16) != "FireIceLadderW1G"))
                                                                    .OrderByDescending(x => x.seconds_since_update))
            {
                GameProcessingOrder += 1;
                ProcessGame(GameData);
                WriteGameToConsole(GameData);
                AddGameToLadder(GameData);

                // Sort ladder by marathon position and add to the game
                Ladder.Sort(new MarathonComparer());
                GameData.Ladder = Ladder;

                WriteLadderToConsole();
                SaveGame(GameData, GameProcessingOrder, "Games");

            }

            GameProcessingOrder = 0;
            foreach (Game GameData in AllGames.Where(x => (x.finished == 0 && x.aborted == 0)))
            {
                GameProcessingOrder++;
                ProcessGame(GameData);
                WriteGameToConsole(GameData);
                SaveGame(GameData, GameProcessingOrder, "RunningGames");

            }

            Console.ReadKey();
        }

        

        static void WriteGameToConsole(Game game)
        {
            Console.WriteLine(game.id + ": " + game.seconds_since_update.ToString());
            foreach (GamePlayer gameplayer in game.GamePlayers)
            {
                Console.WriteLine(String.Format("{0} - pos: {1}{2}", gameplayer.playername, gameplayer.rank, gameplayer.dropped == 1 ? " dropped" : ""));
            }
        }

        static void WriteLadderToConsole()
        {
            foreach (LadderPlayer LadderPlayer in Ladder.OrderBy(x => x.Position))
            {
                Console.WriteLine(LadderPlayer.Position + ": " + LadderPlayer.PlayerName);

            }

        }

        static Dictionary<int, int> GetScoresList(int playercount)
        {
            if(playercount == 3)
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
        }

        static void AddGameToLadder(Game game)
        { 
            foreach(LadderPlayer LadderPlayer in Ladder)
            {
                LadderPlayer.OldPosition = LadderPlayer.Position;
            }

            // add everyone to the ladder in order
            int ProcessingOrder = 1;

            // if the player dropped in their first game, don't add them to the ladder
            foreach (GamePlayer gameplayer in game.GamePlayers.Where(x => x.dropped != 1).OrderBy(x => x.rank).ThenBy(x => x.playername))
            {
                if (!Ladder.Exists(x => x.PlayerName == gameplayer.playername))
                {
                    Ladder.Add(new LadderPlayer(gameplayer.playername, Ladder.Count() + 1));
                }
                gameplayer.processingorder = ProcessingOrder;
                ProcessingOrder++;

                if (gameplayer.rank == 1)
                {
                    Ladder.Find(x => x.PlayerName == gameplayer.playername).AddWinToMarathonScore(game.id);
                }
            }

            // give everyone a score 

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
                    Console.WriteLine(gameplayer.playername + ": " + score);
                }


            }

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
                    double NewPosAbsolute = Convert.ToDouble(gameplayer.currentposition) - gameplayer.score;
                    double NewPosRelative = Convert.ToDouble(gameplayer.currentposition) * (1 - gameplayer.score / 20);
                    if (gameplayer.score < 0)
                    {
                        gameplayer.newposition = Math.Min(TotalPlayers, Convert.ToInt32(Math.Ceiling(Math.Max(NewPosAbsolute, NewPosRelative))));

                    }
                    else
                    {
                        gameplayer.newposition = Math.Max(1, Convert.ToInt32(Math.Floor(Math.Min(NewPosAbsolute, NewPosRelative))));
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

                    Console.WriteLine(String.Format("{0} moves from {1} to {2}", gameplayer.playername, gameplayer.newposition, gameplayer.newposition));
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
                    Console.WriteLine(String.Format("{0} moves from {1} to {2}", LadderPlayer.PlayerName, LadderPlayer.Position, newpos));
                    LadderPlayer.Position = newpos;
                }
                newpos++;
            }

        }
        
    }
}
