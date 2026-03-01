using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Torn
{
	/// <summary>
	/// This represents a P&C Micro's O-Zone lasergame database server. 
	/// It connects to O-Zone as if we were an O-Zone print server.
	/// </summary>
	public class OZone : LaserGameServer
	{
		protected string server;
		protected string port;

		private List<ServerGame> serverGames = new List<ServerGame>();
		private List<LaserGamePlayer> laserPlayers = new List<LaserGamePlayer>();

		private TcpClient client;
		private NetworkStream nwStream;

		protected OZone() { }

		public OZone(string _server, string _port)
		{
			server = _server;
			port = _port;
			Connect();
		}

		private bool Connect()
		{
			if (connected) {
				client.Close();
			};
			client = new TcpClient(server, Int32.Parse(port));
			nwStream = client.GetStream();
			connected = true;

			ReadFromOzone(client, nwStream);
			ReadFromOzone(client, nwStream);

			return connected;
		}

		public override List<ServerGame> GetGames()
		{
			string textToSend = "{\"command\": \"list\"}";
			string result = QueryServer(textToSend);

			List<ServerGame> games = new List<ServerGame>();

			try
			{
				if (result?.Length < 6)
					return games;

				string cleanedResult = result?.Remove(0, 5);

				Console.WriteLine("GetGames() returned " + result?.Length + " characters, beginning with " + result?.Substring(0, 30) + "...");

				JObject root = JObject.Parse(cleanedResult);

				JToken gameList = root.SelectToken("$.gamelist");

				foreach (JObject jgame in gameList.Children())
				{
					var game = new ServerGame();
					if (jgame["gamenum"] != null)   game.GameId = Int32.Parse(jgame["gamenum"].ToString());
					if (jgame["gamename"] != null)   game.Description = jgame["gamename"].ToString();
					if (jgame["starttime"] != null)
					{
						string dateTimeStr = jgame["starttime"].ToString();
						game.Time = DateTime.Parse(dateTimeStr,
							System.Globalization.CultureInfo.InvariantCulture);
					}
					if (jgame["endtime"] != null)
					{
						try
						{
							string dateTimeStr = jgame["endtime"].ToString();
							game.EndTime = DateTime.Parse(dateTimeStr,
								System.Globalization.CultureInfo.InvariantCulture);
						} catch
						{
							string dateTimeStr = jgame["starttime"].ToString();
							game.EndTime = DateTime.Parse(dateTimeStr,
								System.Globalization.CultureInfo.InvariantCulture);
						}
					}
					if (jgame["valid"] != null)
					{
						int isValid = Int16.Parse(jgame["valid"].ToString());
						if (isValid > 0)
						{
							game.OnServer = true;
							games.Add(game);
						}
						else game.OnServer = false;

						serverGames.Add(game);
					}
				}
			}
			catch (Exception e)
			{
				// Squish: something threw in the Newtonsoft.Json parsing, probably because sometimes O-Zone sends
				// incomplete data and we get "Unexpected end of content". Let's just return the games list we had so far.
				Console.WriteLine("GetGames() threw with message:\n" + e.Message + "\n. Result:\n" + result + "\n. Stack trace:\n" + e.StackTrace + "\nReconnecting...");
				Connect();
			}
			
			return games;
		}

		string ReadFromOzone(TcpClient client, NetworkStream nwStream)
		{
			string str = "";
			try
			{
				bool reading = true;
				int BYTE_LIMIT = 128;


				while (reading)
				{
					byte[] bytesToRead = new byte[BYTE_LIMIT];
					int bytesRead = nwStream.Read(bytesToRead, 0, BYTE_LIMIT);
					string current = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);

					str += current;
					if(current.EndsWith("}"))
					{
						Thread.Sleep(1);
						var result = nwStream.DataAvailable;

						if (!result) break;
					}
					
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("ReadFromOzone() threw with message:\n" + e.Message + "\n. Stack trace:\n" + e.StackTrace);
			}

			return str;
		}

		private string QueryServer(string query)
		{
			string result = null;
			try
			{
				//---create a TCPClient object at the IP and port no.---
				byte[] messageBytes = ASCIIEncoding.ASCII.GetBytes("(" + query);

				int[] header = new int[] { query.Length, 0, 0, 0 };

				byte[] bytesToSend = new byte[header.Length + messageBytes.Length];
				System.Buffer.BlockCopy(header, 0, bytesToSend, 0, header.Length);
				System.Buffer.BlockCopy(messageBytes, 0, bytesToSend, header.Length, messageBytes.Length);

				//---send the text---
				nwStream.Write(bytesToSend, 0, bytesToSend.Length);

				//---read back the text---
				result = ReadFromOzone(client, nwStream);
			}
			catch (Exception e)
			{
				// Squish. O-Zone has randomly forcibly closed connection.
				Console.WriteLine("QueryServer() threw with message:\n" + e.Message + "\n. Stack trace:\n" + e.StackTrace + "\nReconnecting...");
				Connect();
			}

			return result;
		}

		public override void PopulateGame(ServerGame game)
		{
			if (!game.GameId.HasValue)
				return;

			if (game.Events.Count != 0)
				return;

			try
			{
				string textToSend = "{\"gamenumber\": " + game.GameId + ", \"command\": \"all\"}";
				string result = QueryServer(textToSend);

				if (result?.Length < 6)
					return;

				string cleanedResult = result?.Remove(0, 5);

				JObject root = JObject.Parse(cleanedResult);

				IDictionary<int, string> eventNames = new Dictionary<int, string>
				{
					{ 0, "Tag Foe - Phasor" },
					{ 1, "Tag Foe - Chest" },
					{ 2, "Tag Foe - Left Front Shoulder" },
					{ 3, "Tag Foe - Right Front Shoulder" },
					{ 4, "Tag Foe - Left Back Shoulder" },
					{ 5, "Tag Foe - Right Back Shoulder" },
					{ 6, "Tag Foe - Back" },

					{ 7, "Tag Ally - Phasor" },
					{ 8, "Tag Ally - Chest" },
					{ 9, "Tag Ally - Left Front Shoulder" },
					{ 10, "Tag Ally - Right Front Shoulder" },
					{ 11, "Tag Ally - Left Back Shoulder" },
					{ 12, "Tag Ally - Right Back Shoulder" },
					{ 13, "Tag Ally - Back" },

					{ 14, "Tagged by Foe - Phasor" },
					{ 15, "Tagged by Foe - Chest" },
					{ 16, "Tagged by Foe - Left Front Shoulder" },
					{ 17, "Tagged by Foe - Right Front Shoulder" },
					{ 18, "Tagged by Foe - Left Back Shoulder" },
					{ 19, "Tagged by Foe - Right Back Shoulder" },
					{ 20, "Tagged by Foe - Back" },

					{ 21, "Tagged by Ally - Phasor" },
					{ 22, "Tagged by Ally - Chest" },
					{ 23, "Tagged by Ally - Left Front Shoulder" },
					{ 24, "Tagged by Ally - Right Front Shoulder" },
					{ 25, "Tagged by Ally - Left Back Shoulder" },
					{ 26, "Tagged by Ally - Right Back Shoulder" },
					{ 27, "Tagged by Ally - Back" },

					{ 28, "Level 1 Termination" },
					{ 29, "Level 2 Termination" },

					{ 30, "Tag Base" },
					{ 31, "Destroy Base" },

					{ 32, "Eliminated" },

					{ 33, "Tagged by Base" },
					{ 34, "Tagged by Mine" },

					{ 55, "Locked on" },
					{ 56, "Launch Missile" },
					{ 57, "Missile Tag" },

					{ 60, "Denied Ally" },
					{ 61, "Denied Foe" },
					{ 62, "Denied by Ally" },
					{ 63, "Denied by Foe" },
					{ 64, "Denied by Timeout" },
					{ 65, "Assist Denied Foe" }
				};

				var events = new List<Event>();
				var players = new List<ServerPlayer>();

				if (root["events"] != null)
				{
					string eventsStr = root["events"].ToString();
					var eventsDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventsStr);

					foreach (var evnt in eventsDictionary)
					{
						string eventContent = evnt.Value.ToString();
						JObject eventRoot = JObject.Parse(eventContent);

						int eventTime = 0;
						string eventPlayerId = "";
						int eventPlayerTeamId = -1;
						int eventType = -1;
						int score = 0;
						string eventOtherPlayerId = "";
						int eventOtherPlayerTeamId = -1;
						if (eventRoot["time"] != null) eventTime = Int32.Parse(eventRoot["time"].ToString());
						if (eventRoot["idf"] != null) eventPlayerId = eventRoot["idf"].ToString();
						if (eventRoot["tidf"] != null) eventPlayerTeamId = Int32.Parse(eventRoot["tidf"].ToString());
						if (eventRoot["evtyp"] != null) eventType = Int32.Parse(eventRoot["evtyp"].ToString());
						if (eventRoot["score"] != null) score = Int32.Parse(eventRoot["score"].ToString());
						if (eventRoot["ida"] != null) eventOtherPlayerId = eventRoot["ida"].ToString();
						if (eventRoot["tida"] != null) eventOtherPlayerTeamId = Int32.Parse(eventRoot["tida"].ToString());

						var gameEvent = new Event
						{
							Time = game.Time.AddSeconds(eventTime),
							ServerPlayerId = eventPlayerId,
							ServerTeamId = eventPlayerTeamId,
							Event_Type = eventType,
							Score = score,
							OtherPlayer = eventOtherPlayerId,
							OtherTeam = eventOtherPlayerTeamId,
							Event_Name = eventNames.ContainsKey(eventType) ? eventNames[eventType] : "Unknown",
						};
						events.Add(gameEvent);
					}
				}

				if (root["players"] != null)
				{
					string playersStr = root["players"].ToString();
					var playersDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(playersStr);

					foreach (var player in playersDictionary)
					{
						string id = player.Key.ToString();
						string playerContent = player.Value.ToString();
						JObject playerRoot = JObject.Parse(playerContent);

						ServerPlayer serverPlayer = new ServerPlayer();
						if (playerRoot["alias"] != null) serverPlayer.Alias = playerRoot["alias"].ToString();
						if (playerRoot["score"] != null) serverPlayer.Score = Int32.Parse(playerRoot["score"].ToString());
						if (playerRoot["wterm"] != null) serverPlayer.YellowCards = Int32.Parse(playerRoot["wterm"].ToString());
						if (playerRoot["term"] != null) serverPlayer.RedCards = Int32.Parse(playerRoot["term"].ToString());
						if (playerRoot["rank"] != null) serverPlayer.Rank = UInt32.Parse(playerRoot["rank"].ToString());
						if (playerRoot["elim"] != null) serverPlayer.SetIsEliminated(Int32.Parse(playerRoot["elim"].ToString()) > 0);
						// This is correct Ozone has the names backwards for tagson and tagsby
						if (playerRoot["tagsby"] != null) serverPlayer.HitsOn = Int32.Parse(playerRoot["tagsby"].ToString());
						if (playerRoot["tagson"] != null) serverPlayer.HitsBy = Int32.Parse(playerRoot["tagson"].ToString());
						if (playerRoot["bhits"] != null) serverPlayer.BaseHits = Int32.Parse(playerRoot["bhits"].ToString());
						if (playerRoot["bdest"] != null) serverPlayer.BaseDestroys = Int32.Parse(playerRoot["bdest"].ToString());
						if (playerRoot["bdenialsh"] != null) serverPlayer.BaseDenies = Int32.Parse(playerRoot["bdenialsh"].ToString());
						if (playerRoot["bdeniedsh"] != null) serverPlayer.BaseDenied = Int32.Parse(playerRoot["bdeniedsh"].ToString());
						if (playerRoot["omid"] != null)
						{
							string omid = playerRoot["omid"].ToString();
							// If pack was not logged in use alias as identifier
							if (omid == "-1")
							{
								omid = playerRoot["alias"].ToString();

							}
							serverPlayer.PlayerId = omid;
							serverPlayer.ServerPlayerId = id;
						}

						if (playerRoot["tid"] != null)
						{
							serverPlayer.TeamId = Int32.Parse(playerRoot["tid"].ToString());
							serverPlayer.ServerTeamId = Int32.Parse(playerRoot["tid"].ToString());
							if (0 <= serverPlayer.ServerTeamId && serverPlayer.ServerTeamId < 8)
								serverPlayer.Colour = (Colour)(serverPlayer.ServerTeamId + 1);
							else
								serverPlayer.Colour = Colour.None;
						}

						if (playerRoot["qrcode"] != null) serverPlayer.QRCode = playerRoot["qrcode"].ToString();

						serverPlayer.PopulateTerms(events);

						serverPlayer.Pack = "Pack " + id;

						players.Add(serverPlayer);
					}
				}

				game.Players.AddRange(players);
				game.Events.AddRange(events);
			}
			catch (Exception e)
			{
				// Squish: something threw in the Newtonsoft.Json parsing, probably because sometimes O-Zone sends incomplete data and we get "Unexpected end of content".
				// Let's not populate game.Events, because doing so would mean that we think this game is fully populated, and would not retry it.
				// Let's not populate game.Players, because doing so would mean that after we retry, some players might appear twice.
				Console.WriteLine("PopulateGame() threw with message:\n" + e.Message + "\n. Stack trace:\n" + e.StackTrace);
			}
		}

		public override List<LaserGamePlayer> GetPlayers(string mask)
		{
			return new List<LaserGamePlayer>();
		}

		public override List<LaserGamePlayer> GetPlayers(string mask, List<LeaguePlayer> players)
		{
			foreach (LeaguePlayer player in players)
			{

				LaserGamePlayer laserPlayer = new LaserGamePlayer();
				laserPlayer.Alias = player.Name;
				laserPlayer.Id = player.Id;
				if(laserPlayers.Find((p) => p.Id == laserPlayer.Id) == null) laserPlayers.Add(laserPlayer);

			}
			
			return laserPlayers;
		}
	}
}
