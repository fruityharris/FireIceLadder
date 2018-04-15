using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace JSONHelpers
{
    public class Methods
    {
        public static string TopLevelDirectory = "C:/FireIceLadder/Data";

        public static string GetLatestDirectory()
        {
            return Directory.GetDirectories(TopLevelDirectory).OrderByDescending(x => x).First();
        }

        public static string GetLatestSubdirectory(string Sub)
        {
            string lRet = GetLatestDirectory() + "/" + Sub;
            if (!Directory.Exists(lRet))
            {
                Directory.CreateDirectory(lRet);
            }
            return lRet;
        }


        public static void CreateNewDirectory()
        {
            Directory.CreateDirectory(TopLevelDirectory + "/" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss"));

        }

        public static void SaveGame(Game game, int ProcessingOrder, string SubFolder)
        {

            string filepath = GetLatestSubdirectory(SubFolder) + "/" + ProcessingOrder.ToString("D6") + "_" + game.id;

            using (StreamWriter file = File.CreateText(filepath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, game);
            }

        }

        public static List<Game> StoreAndReturnGamesFromSnellman(string Pattern, string LogName)
        {

            string url = "http://terra.snellman.net/app/list-games/by-pattern/" + Pattern;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                string filepath = GetLatestSubdirectory("Logs") + "/" + LogName + ".json";
                WebResponse response = request.GetResponse();
                using (Stream output = File.Create(filepath))
                using (Stream responseStream = response.GetResponseStream())
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, bytesRead);
                    }
                }

                List<Game> Games = new List<Game>();

                using (StreamReader file = File.OpenText(filepath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    SnellmanData RawData = (SnellmanData)serializer.Deserialize(file, typeof(SnellmanData));
                    Games = RawData.games;
                }

                return Games;

            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.GetEncoding("utf-8"));
                    String errorText = reader.ReadToEnd();
                    Console.WriteLine(errorText);
                }
                throw;
            }

        }

        public static List<Game> GetGamesFromFilestore(string SubFolder)
        {
            List<Game> Games = new List<Game>();

            foreach (string filepath in Directory.GetFiles(GetLatestSubdirectory(SubFolder)).OrderByDescending(x => x))
            {
                using (StreamReader file = System.IO.File.OpenText(filepath))
                {
                    Games.Add(JsonConvert.DeserializeObject<Game>(file.ReadToEnd()));
                }
            }

            return Games;

        }
    }

    public class SnellmanData
    {
        public List<Game> games { get; set; }
    }

    public class Game
    {
        public int? finished { get; set; }
        public int? aborted { get; set; }
        public float seconds_since_update { get; set; }
        public List<string> usernames { get; set; }
        public List<int?> ranks { get; set; }
        public string id { get; set; }
        public List<GamePlayer> GamePlayers { get; set; }
        public List<LadderPlayer> Ladder { get; set; }
        public int? round { get; set; }
        public List<int?> vps { get; set; }
        public List<int?> dropped { get; set; }
        public List<string> factions { get; set; }
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

        public void AddWinToMarathonScore(string GameId)
        {
            int Week = int.Parse(GameId.Substring(GameId.IndexOf("W") + 1, GameId.IndexOf("G") - GameId.IndexOf("W") - 1));
            int Game = int.Parse(GameId.Substring(GameId.IndexOf("G") + 1, GameId.Length - GameId.IndexOf("G") - 1));
            if(Week >= 5)
            {
                if(MarathonScore.ContainsKey(Game))
                {
                    MarathonScore[Game] += 1;
                }
                else
                {
                    MarathonScore.Add(Game, 1);
                }
            }
        }
    }
}
