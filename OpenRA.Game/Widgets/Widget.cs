#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Widgets.Delegates;

namespace OpenRA.Widgets
{
	public abstract class Widget
	{
		// Info defined in YAML
		public string Id = null;
		public string X = "0";
		public string Y = "0";
		public string Width = "0";
		public string Height = "0";
		public string Delegate = null;
		public bool ClickThrough = true;
		public bool Visible = true;
		public readonly List<Widget> Children = new List<Widget>();

		// Calculated internally
		public Rectangle Bounds;
		public Widget Parent = null;

		static List<string> Delegates = new List<string>();
		public static Stack<string> WindowList = new Stack<string>();
		
		// Common Funcs that most widgets will want
		public Action<object> SpecialOneArg = (arg) => {};
		public Func<MouseInput,bool> OnMouseDown = mi => {return false;};
		public Func<MouseInput,bool> OnMouseUp = mi => {return false;};
		public Func<MouseInput,bool> OnMouseMove = mi => {return false;};
		public Func<System.Windows.Forms.KeyPressEventArgs, Modifiers,bool> OnKeyPress = (e,modifiers) => {return false;};

		public Func<bool> IsVisible;

		public Widget() { IsVisible = () => Visible; }
		
		public Widget(Widget widget)
		{	
			Id = widget.Id;
			X = widget.X;
		 	Y = widget.Y;
		 	Width = widget.Width;
			Height = widget.Height;
		 	Delegate = widget.Delegate;
		 	ClickThrough = widget.ClickThrough;
		 	Visible = widget.Visible;
			
			Bounds = widget.Bounds;
			Parent = widget.Parent;
			
			OnMouseDown = widget.OnMouseDown;
			OnMouseUp = widget.OnMouseUp;
			OnMouseMove = widget.OnMouseMove;
			OnKeyPress = widget.OnKeyPress;

			IsVisible = widget.IsVisible;
			
			foreach(var child in widget.Children)
				AddChild(child.Clone());
		}
		
		public abstract Widget Clone();
		
		public virtual int2 RenderOrigin
		{
			get {
				var offset = (Parent == null) ? int2.Zero : Parent.ChildOrigin;
				return new int2(Bounds.X, Bounds.Y) + offset;
			}
		}
		public virtual int2 ChildOrigin	{ get { return RenderOrigin; } }
		public virtual Rectangle RenderBounds { get { return new Rectangle(RenderOrigin.X, RenderOrigin.Y, Bounds.Width, Bounds.Height); } }
		
		public virtual void Initialize()
		{
			// Parse the YAML equations to find the widget bounds
			Rectangle parentBounds = (Parent == null) 
				? new Rectangle(0,0,Game.viewport.Width,Game.viewport.Height) 
				: Parent.Bounds;
			
			Dictionary<string, int> substitutions = new Dictionary<string, int>();
				substitutions.Add("WINDOW_RIGHT", Game.viewport.Width);
				substitutions.Add("WINDOW_BOTTOM", Game.viewport.Height);
				substitutions.Add("PARENT_RIGHT", parentBounds.Width);
				substitutions.Add("PARENT_LEFT", parentBounds.Left);
				substitutions.Add("PARENT_TOP", parentBounds.Top);
				substitutions.Add("PARENT_BOTTOM", parentBounds.Height);
			int width = Evaluator.Evaluate(Width, substitutions);
			int height = Evaluator.Evaluate(Height, substitutions);
					
			substitutions.Add("WIDTH", width);
			substitutions.Add("HEIGHT", height);
			
			Bounds = new Rectangle(Evaluator.Evaluate(X, substitutions),
			                       Evaluator.Evaluate(Y, substitutions),
			                       width,
			                       height);
			
			// Non-static func definitions
			
			if (Delegate != null && !Delegates.Contains(Delegate))
					Delegates.Add(Delegate);
			
			foreach (var child in Children)
				child.Initialize();
		}
		
		public void InitDelegates()
		{
			foreach(var d in Delegates)
				Game.CreateObject<IWidgetDelegate>(d);
		}
		
		public bool HitTest(int2 xy)
		{
			if (!IsVisible()) return false;
			if (RenderBounds.Contains(xy.ToPoint()) && !ClickThrough) return true;
			
			return Children.Any(c => c.HitTest(xy));
		}
		
		public Rectangle GetEventBounds()
		{
			return Children
				.Where(c => c.Visible)
				.Select(c => c.GetEventBounds())
				.Aggregate(RenderBounds, Rectangle.Union);
		}
		
		
		public bool Focused { get { return Chrome.selectedWidget == this; } }
		public virtual bool TakeFocus(MouseInput mi)
		{
			if (Focused)
				return true;
			
			if (Chrome.selectedWidget != null && !Chrome.selectedWidget.LoseFocus(mi))
				return false;
			Chrome.selectedWidget = this;
			return true;
		}
		
		// Remove focus from this widget; return false if you don't want to give it up
		public virtual bool LoseFocus(MouseInput mi)
		{
			if (Chrome.selectedWidget == this)
				Chrome.selectedWidget = null;
			
			return true;
		}
		
		public virtual bool HandleInput(MouseInput mi) { return !ClickThrough; }
		public bool HandleMouseInputOuter(MouseInput mi)
		{
			// Are we able to handle this event?
			if (!(Focused || (IsVisible() && GetEventBounds().Contains(mi.Location.X,mi.Location.Y))))
				return false;
			
			// Send the event to the deepest children first and bubble up if unhandled
			foreach (var child in Children)
				if (child.HandleMouseInputOuter(mi))
					return true;

			// Do any widgety behavior (button click etc)
			// Return false if it can't handle any user actions
			if (!HandleInput(mi))
				return false;
			
			// Apply any special logic added by delegates; they return true if they caught the input
			if (mi.Event == MouseInputEvent.Down && OnMouseDown(mi)) return true;
			if (mi.Event == MouseInputEvent.Up && OnMouseUp(mi)) return true;
			if (mi.Event == MouseInputEvent.Move && OnMouseMove(mi)) return true;
			
			return true;
		}
				
		
		public virtual bool HandleKeyPress(System.Windows.Forms.KeyPressEventArgs e, Modifiers modifiers) { return false; }
		public virtual bool HandleKeyPressOuter(System.Windows.Forms.KeyPressEventArgs e, Modifiers modifiers)
		{			
			if (!IsVisible())
				return false;
			
			// Can any of our children handle this?
			foreach (var child in Children)
				if (child.HandleKeyPressOuter(e, modifiers))
					return true;

			// Do any widgety behavior (enter text etc)
			var handled = HandleKeyPress(e,modifiers);
			
			// Apply any special logic added by delegates; they return true if they caught the input
			if (OnKeyPress(e,modifiers)) return true;
			
			return handled;
		}
		
		public abstract void DrawInner( World world );
		
		public virtual void Draw(World world)
		{
			if (IsVisible())
			{
				DrawInner( world );
				foreach (var child in Children)
					child.Draw(world);
			}
		}
		
		public virtual void Tick(World world)
		{
			if (IsVisible())
				foreach (var child in Children)
					child.Tick(world);
		}
		
		public void AddChild(Widget child)
		{
			child.Parent = this;
			Children.Add( child );
		}
		
		public Widget GetWidget(string id)
		{
			if (this.Id == id)
				return this;
			
			foreach (var child in Children)
			{
				var w = child.GetWidget(id);
				if (w != null)
					return w;
			}
			return null;
		}

		public T GetWidget<T>(string id) where T : Widget
		{
			return (T)GetWidget(id);
		}
		
		public void CloseWindow()
		{
			Chrome.rootWidget.GetWidget(WindowList.Pop()).Visible = false;
			if (WindowList.Count > 0)
				Chrome.rootWidget.GetWidget(WindowList.Peek()).Visible = true;
		}

		public Widget OpenWindow(string id)
		{
			if (WindowList.Count > 0)
				Chrome.rootWidget.GetWidget(WindowList.Peek()).Visible = false;
			WindowList.Push(id);
			var window = Chrome.rootWidget.GetWidget(id);
			window.Visible = true;
			return window;
		}
	}

	class ContainerWidget : Widget {
		public ContainerWidget() : base() { }

		public ContainerWidget(Widget other) : base(other) { }

		public override void DrawInner( World world ) { }

		public override Widget Clone() { return new ContainerWidget(this); }
	}
	public interface IWidgetDelegate { }
}