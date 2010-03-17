using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Net;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Server;

namespace OpenRA.Widgets.Delegates
{
	public class WidgetDelegate
	{
		// For checkboxes
		public virtual bool GetState(Widget w) { return false; }
		
		// For any widget
		public virtual bool OnMouseDown(Widget w, MouseInput mi) { return false; }
		public virtual bool OnMouseUp(Widget w, MouseInput mi) { return false; }
		public virtual bool OnMouseMove(Widget w, MouseInput mi) { return false; }
	}
	
	public class MainMenuButtonsDelegate : WidgetDelegate
	{	
		public override bool OnMouseUp(Widget w, MouseInput mi)
		{
			// Main Menu root
			if (w.Id == "MAINMENU_BUTTON_QUIT")
			{
				Game.Exit();	
				return true;
			}
			return false;
		}
	}
	
	public class CreateServerMenuDelegate : WidgetDelegate
	{
		static bool AdvertiseServerOnline = Game.Settings.InternetServer;
		
		public override bool GetState(Widget w)
		{
			if (w.Id == "CREATESERVER_CHECKBOX_ONLINE")
				return AdvertiseServerOnline;
			
			return false;
		}
		
		public override bool OnMouseDown(Widget w, MouseInput mi)
		{
			if (w.Id == "CREATESERVER_CHECKBOX_ONLINE")
			{
				AdvertiseServerOnline = !AdvertiseServerOnline;
				return true;
			}
			
			return false;
		}
		
		public override bool OnMouseUp(Widget w, MouseInput mi)
		{
			if (w.Id == "MAINMENU_BUTTON_CREATE")
			{
				Game.chrome.rootWidget.GetWidget("MAINMENU_BG").Visible = false;
				Game.chrome.rootWidget.GetWidget("CREATESERVER_BG").Visible = true;
				return true;
			}
			
			if (w.Id == "CREATESERVER_BUTTON_CANCEL")
			{
				Game.chrome.rootWidget.GetWidget("MAINMENU_BG").Visible = true;
				Game.chrome.rootWidget.GetWidget("CREATESERVER_BG").Visible = false;
				return true;
			}
			
			if (w.Id == "CREATESERVER_BUTTON_START")
			{
				Game.chrome.rootWidget.GetWidget("CREATESERVER_BG").Visible = false;
				Log.Write("Creating server");
				
				Server.Server.ServerMain(AdvertiseServerOnline, Game.Settings.MasterServer, 
				                        Game.Settings.GameName, Game.Settings.ListenPort, 
										Game.Settings.ExternalPort, Game.Settings.InitialMods);
				
				Log.Write("Joining server");
				Game.JoinServer(IPAddress.Loopback.ToString(), Game.Settings.ListenPort);
				return true;
			}

			return false;
		}
	}
	
	public class ServerBrowserDelegate : WidgetDelegate
	{
		static GameServer[] GameList;
		static List<Widget> GameButtons = new List<Widget>();
		
		public override bool OnMouseUp(Widget w, MouseInput mi)
		{
			// Main Menu root
			if (w.Id == "MAINMENU_BUTTON_JOIN")
			{
				Game.chrome.rootWidget.GetWidget("MAINMENU_BG").Visible = false;
				Widget bg = Game.chrome.rootWidget.GetWidget("JOINSERVER_BG");
				bg.Visible = true;
				
				int height = 50;
				int width = 300;
				int i = 0;
				GameList = MasterServerQuery.GetGameList(Game.Settings.MasterServer).ToArray();

				bg.Children.RemoveAll(a => GameButtons.Contains(a));
				GameButtons.Clear();

				foreach (var game in GameList)
				{
					ButtonWidget b = new ButtonWidget();
					b.Bounds = new Rectangle(bg.Bounds.X + 20, bg.Bounds.Y + height, width, 25);	
					b.GetType().GetField("Id").SetValue( b, "JOIN_GAME_{0}".F(i));
					b.GetType().GetField("Text").SetValue( b, "{0} ({1})".F(game.Name, game.Address));
					b.GetType().GetField("Delegate").SetValue( b, "ServerBrowserDelegate");
				
					bg.AddChild(b);
					GameButtons.Add(b);
					
					height += 35;
				}
				
				return true;
			}
			
			if (w.Id == "JOINSERVER_BUTTON_DIRECTCONNECT")
			{
				Game.chrome.rootWidget.GetWidget("JOINSERVER_BG").Visible = false;
				Game.JoinServer(Game.Settings.NetworkHost, Game.Settings.NetworkPort);
				return true;
			}
			
			if (w.Id.Substring(0,10) == "JOIN_GAME_")
			{
				Game.chrome.rootWidget.GetWidget("JOINSERVER_BG").Visible = false;
				int index = int.Parse(w.Id.Substring(10));
				var game = GameList[index];
				Game.JoinServer(game.Address.Split(':')[0], int.Parse(game.Address.Split(':')[1]));
				return true;
			}			
				
			if (w.Id == "JOINSERVER_BUTTON_CANCEL")
			{
				Game.chrome.rootWidget.GetWidget("JOINSERVER_BG").Visible = false;
				Game.chrome.rootWidget.GetWidget("MAINMENU_BG").Visible = true;
				return true;
			}
			
			return false;
		}
	}
}