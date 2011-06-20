#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.FileFormats;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.RA.Server
{
	public class LobbyCommands : ServerTrait, IInterpretCommand, INotifyServerStart
	{
		public bool InterpretCommand(S server, Connection conn, Session.Client client, string cmd)
		{
			if (server.GameStarted)
			{
				server.SendChatTo(conn, "Cannot change state when game started. ({0})".F(cmd));
				return false;
			}
			else if (client.State == Session.ClientState.Ready && !(cmd == "ready" || cmd == "startgame"))
			{
				server.SendChatTo(conn, "Cannot change state when marked as ready.");
				return false;
			}
			
			var dict = new Dictionary<string, Func<string, bool>>
			{
				{ "ready",
					s =>
					{
						// if we're downloading, we can't ready up.
						if (client.State == Session.ClientState.NotReady)
							client.State = Session.ClientState.Ready;
						else if (client.State == Session.ClientState.Ready)
							client.State = Session.ClientState.NotReady;

						Log.Write("server", "Player @{0} is {1}",
							conn.socket.RemoteEndPoint, client.State);

						server.SyncLobbyInfo();
						
						if (server.conns.Count > 0 && server.conns.All(c => server.GetClient(c).State == Session.ClientState.Ready))
							InterpretCommand(server, conn, client, "startgame");
						
						return true;
					}},
				{ "startgame", 
					s => 
					{
						server.StartGame();
						return true;
					}},
				{ "lag",
					s =>
					{
						int lag;
						if (!int.TryParse(s, out lag)) { Log.Write("server", "Invalid order lag: {0}", s); return false; }

						Log.Write("server", "Order lag is now {0} frames.", lag);

						server.lobbyInfo.GlobalSettings.OrderLatency = lag;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot",
					s =>
					{
						if (!server.lobbyInfo.Slots.ContainsKey(s))
						{
							Log.Write("server", "Invalid slot: {0}", s );
							return false;
						}
						var slot = server.lobbyInfo.Slots[s];

						if (slot.Closed || slot.Bot != null ||
					    	server.lobbyInfo.ClientInSlot(s) != null)
							return false;

						client.Slot = s;
						S.SyncClientToPlayerReference(client, server.Map.Players[s]);

						server.SyncLobbyInfo();
						return true;
					}},
				{ "spectate",
					s =>
					{
						client.Slot = null;
						client.SpawnPoint = 0;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_close",
					s =>
					{
						if (!server.lobbyInfo.Slots.ContainsKey(s))
						{
							Log.Write("server", "Invalid slot: {0}", s );
							return false;
						}

						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						// kick any player that's in the slot
						var occupant = server.lobbyInfo.ClientInSlot(s);
						if (occupant != null)
						{
							var occupantConn = server.conns.FirstOrDefault( c => c.PlayerIndex == occupant.Index );
							if (occupantConn != null)
							{
								server.SendOrderTo(occupantConn, "ServerError", "Your slot was closed by the host");
								server.DropClient(occupantConn);
							}
						}
						var slot = server.lobbyInfo.Slots[s];
						slot.Closed = true;
						slot.Bot = null;

						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_open",
					s =>
					{
						if (!server.lobbyInfo.Slots.ContainsKey(s))
						{
							Log.Write("server", "Invalid slot: {0}", s );
							return false;
						}

						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						var slot = server.lobbyInfo.Slots[s];
						slot.Closed = false;
						slot.Bot = null;

						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_bot",
					s =>
					{
						var parts = s.Split(' ');

						if (parts.Length < 2)
						{
							server.SendChatTo( conn, "Malformed slot_bot command" );
							return true;
						}

						if (!server.lobbyInfo.Slots.ContainsKey(parts[0]))
						{
							Log.Write("server", "Invalid slot: {0}", parts[0] );
							return false;
						}

						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						var slot = server.lobbyInfo.Slots[parts[0]];
						slot.Bot = string.Join(" ", parts.Skip(1).ToArray() );
						slot.Closed = false;

						server.SyncLobbyInfo();
						return true;
					}},
				{ "map",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can change the map" );
							return true;
						}
						server.lobbyInfo.GlobalSettings.Map = s;			
						LoadMap(server);

						// Reassign players into slots
						int i = 0;
						foreach(var c in server.lobbyInfo.Clients)
						{
							c.SpawnPoint = 0;
							c.State = Session.ClientState.NotReady;
							c.Slot = c.Slot == null || i >= server.lobbyInfo.Slots.Count ?
								null : server.lobbyInfo.Slots.ElementAt(i++).Key;

							if (c.Slot != null)
								S.SyncClientToPlayerReference(c, server.Map.Players[c.Slot]);
						}
						
						server.SyncLobbyInfo();
						return true;
					}},
				{ "lockteams",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can set that option" );
							return true;
						}
						
						bool.TryParse(s, out server.lobbyInfo.GlobalSettings.LockTeams);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "allowcheats",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can set that option" );
							return true;
						}
						
						bool.TryParse(s, out server.lobbyInfo.GlobalSettings.AllowCheats);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "kick",
					s => 
					{

						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can kick players" );
							return true;
						}

						int clientID;
						int.TryParse( s, out clientID );

						var connToKick = server.conns.SingleOrDefault( c => server.GetClient(c) != null && server.GetClient(c).Index == clientID);
						if (connToKick == null) 
						{
							server.SendChatTo( conn, "Noone in that slot." );
							return true;
						}
						
						server.SendOrderTo(connToKick, "ServerError", "You have been kicked from the server");
						server.DropClient(connToKick);
						server.SyncLobbyInfo();
						return true;
					}},
			};
			
			var cmdName = cmd.Split(' ').First();
			var cmdValue = string.Join(" ", cmd.Split(' ').Skip(1).ToArray());

			Func<string,bool> a;
			if (!dict.TryGetValue(cmdName, out a))
				return false;
			
			return a(cmdValue);
		}
		
		public void ServerStarted(S server) { LoadMap(server); }
		static Session.Slot MakeSlotFromPlayerReference(PlayerReference pr)
		{
			if (!pr.Playable) return null;
			return new Session.Slot
			{
				PlayerReference = pr.Name,
				Bot = null,
				Closed = false,
				AllowBots = pr.AllowBots,
				LockRace = pr.LockRace,
				LockColor = pr.LockColor,
				LockTeam = false
			};
		}

		public static void LoadMap(S server)
		{
			server.Map = new Map(server.ModData.AvailableMaps[server.lobbyInfo.GlobalSettings.Map].Path);
			server.lobbyInfo.Slots = server.Map.Players
				.Select(p => MakeSlotFromPlayerReference(p.Value))
				.Where(s => s != null)
				.ToDictionary(s => s.PlayerReference, s => s);
		}
	}
}
