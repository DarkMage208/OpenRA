
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRA.FileFormats;

namespace OpenRA.Widgets.Delegates
{
	public class LobbyDelegate : IWidgetDelegate
	{
		Widget Players, LocalPlayerTemplate, RemotePlayerTemplate;
		
		public LobbyDelegate ()
		{
			var r = Chrome.rootWidget;
			var lobby = r.GetWidget("SERVER_LOBBY");
			Players = Chrome.rootWidget.GetWidget("SERVER_LOBBY").GetWidget("PLAYERS");
			LocalPlayerTemplate = Players.GetWidget("TEMPLATE_LOCAL");
			RemotePlayerTemplate = Players.GetWidget("TEMPLATE_REMOTE");
			
			
			var mapButton = lobby.GetWidget("CHANGEMAP_BUTTON");
			mapButton.OnMouseUp = mi => {
				r.OpenWindow("MAP_CHOOSER");
				return true;
			};	
				
			mapButton.IsVisible = () => mapButton.Visible && Game.IsHost;
			
			Game.LobbyInfoChanged += UpdatePlayerList;
		}
		
		void UpdatePlayerList()
		{
			Players.Children.Clear();
			
			int offset = 0;
			foreach(var client in Game.LobbyInfo.Clients)
			{
				var c = client;
				var template = (client.Index == Game.LocalClient.Index)? LocalPlayerTemplate.Clone() : RemotePlayerTemplate.Clone();
				
				template.Id = "PLAYER_{0}".F(c.Index);
				template.Parent = Players;			
				template.GetWidget<LabelWidget>("NAME").GetText = () => c.Name;
				
				if(client.Index == Game.LocalClient.Index)
				{
					//TODO: Real Color Button
					var color = template.GetWidget<ColorButtonWidget>("COLOR");
					color.OnMouseUp = CyclePalette;
					color.GetPaletteIndex = () => c.PaletteIndex;
					
					var faction = template.GetWidget<ButtonWidget>("FACTION");
					faction.OnMouseUp = CycleRace;
					faction.GetText = () => c.Country;
					
					var spawn = template.GetWidget<ButtonWidget>("SPAWN");
					spawn.OnMouseUp = CycleSpawnPoint;
					spawn.GetText = () => (c.SpawnPoint == 0) ? "-" : c.SpawnPoint.ToString(); 
					
					var team = template.GetWidget<ButtonWidget>("TEAM");
					team.OnMouseUp = CycleTeam;
					team.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString(); 
	
					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.Checked = () => c.State == Session.ClientState.Ready;
					status.OnMouseDown = CycleReady;
				}
				else 
				{
					//TODO: Real Color Label
					var color = template.GetWidget<LabelWidget>("COLOR");
					color.GetText = () => c.PaletteIndex.ToString();
					
					var faction = template.GetWidget<LabelWidget>("FACTION");
					faction.GetText = () => c.Country;
					
					var spawn = template.GetWidget<LabelWidget>("SPAWN");
					spawn.GetText = () => (c.SpawnPoint == 0) ? "-" : c.SpawnPoint.ToString();
					
					var team = template.GetWidget<LabelWidget>("TEAM");
					team.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString(); 
	
					var status = template.GetWidget<CheckboxWidget>("STATUS");
					status.Checked = () => c.State == Session.ClientState.Ready;
				}
				
				template.Bounds = new Rectangle(0, offset, template.Bounds.Width, template.Bounds.Height);
				template.IsVisible = () => true;
				Players.AddChild(template);
				
				offset += template.Bounds.Height;
			}
		}
		
		bool PaletteAvailable(int index) { return Game.LobbyInfo.Clients.All(c => c.PaletteIndex != index); }
		bool SpawnPointAvailable(int index) { return (index == 0) || Game.LobbyInfo.Clients.All(c => c.SpawnPoint != index); }
		
		bool CyclePalette(MouseInput mi)
		{
			var d = (mi.Button == MouseButton.Left) ? +1 : Player.PlayerColors(Game.world).Count() - 1;

			var newIndex = ((int)Game.LocalClient.PaletteIndex + d) % Player.PlayerColors(Game.world).Count();
				
			while (!PaletteAvailable(newIndex) && newIndex != (int)Game.LocalClient.PaletteIndex)
				newIndex = (newIndex + d) % Player.PlayerColors(Game.world).Count();
			
			Game.IssueOrder(
				Order.Chat("/pal " + newIndex));
			
			return true;
		}

		bool CycleRace(MouseInput mi)
		{	
			var countries = new[] { "Random" }.Concat(Game.world.GetCountries().Select(c => c.Name));
			
			if (mi.Button == MouseButton.Right)
				countries = countries.Reverse();
			
			var nextCountry = countries
				.SkipWhile(c => c != Game.LocalClient.Country)
				.Skip(1)
				.FirstOrDefault();

			if (nextCountry == null)
				nextCountry = countries.First();

			Game.IssueOrder(Order.Chat("/race " + nextCountry));
			
			return true;
		}

		bool CycleReady(MouseInput mi)
		{
			Game.IssueOrder(Order.Chat("/ready"));
			return true;
		}

		bool CycleSpawnPoint(MouseInput mi)
		{
			var d = (mi.Button == MouseButton.Left) ? +1 : Game.world.Map.SpawnPoints.Count();

			var newIndex = (Game.LocalClient.SpawnPoint + d) % (Game.world.Map.SpawnPoints.Count()+1);

			while (!SpawnPointAvailable(newIndex) && newIndex != (int)Game.LocalClient.SpawnPoint)
				newIndex = (newIndex + d) % (Game.world.Map.SpawnPoints.Count()+1);

			Game.IssueOrder(
				Order.Chat("/spawn " + newIndex));
			return true;
		}
		
		bool CycleTeam(MouseInput mi)
		{
			var d = (mi.Button == MouseButton.Left) ? +1 : Game.world.Map.PlayerCount;

			var newIndex = (Game.LocalClient.Team + d) % (Game.world.Map.PlayerCount+1);

			Game.IssueOrder(
				Order.Chat("/team " + newIndex));
			return true;
		}
	}
}
