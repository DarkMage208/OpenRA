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
	public class Widget
	{
		// Info defined in YAML
		public readonly string Id = null;
		public readonly string X = "0";
		public readonly string Y = "0";
		public readonly string Width = "0";
		public readonly string Height = "0";
		public readonly string Delegate = null;

		public Lazy<WidgetDelegate> InputHandler;

		public bool Visible = true;
		public readonly List<Widget> Children = new List<Widget>();

		// Calculated internally
		public Rectangle Bounds;
		public Widget Parent = null;

		public Widget() { InputHandler = Lazy.New(() => BindHandler(Delegate)); }
		
		public virtual void Initialize()
		{
			// Parse the YAML equations to find the widget bounds
			Rectangle parentBounds = (Parent == null) ? new Rectangle(0,0,Game.viewport.Width,Game.viewport.Height) : Parent.Bounds;
			
			Dictionary<string, int> substitutions = new Dictionary<string, int>();
				substitutions.Add("WINDOW_RIGHT", Game.viewport.Width);
				substitutions.Add("WINDOW_BOTTOM", Game.viewport.Height);
				substitutions.Add("PARENT_RIGHT", parentBounds.Width);
				substitutions.Add("PARENT_BOTTOM", parentBounds.Height);
			int width = Evaluator.Evaluate(Width, substitutions);
			int height = Evaluator.Evaluate(Height, substitutions);
					
			substitutions.Add("WIDTH", width);
			substitutions.Add("HEIGHT", height);
			
			Bounds = new Rectangle(parentBounds.X + Evaluator.Evaluate(X, substitutions),
			                       parentBounds.Y + Evaluator.Evaluate(Y, substitutions),
			                       width,
			                       height);
			
			foreach (var child in Children)
				child.Initialize();
		}

		public Rectangle GetEventBounds()
		{
			return Children
				.Where(c => c.Visible)
				.Select(c => c.GetEventBounds())
				.Aggregate(Bounds, Rectangle.Union);
		}

		static WidgetDelegate BindHandler(string name)
		{
			if (name == null) return null;

			foreach (var mod in Game.ModAssemblies)
			{
				var act = (WidgetDelegate)mod.First.CreateInstance(mod.Second + "." + name);
				if (act != null) return act;
			}

			throw new InvalidOperationException("Cannot locate widget delegate: {0}".F(name));
		}
		
		public virtual bool HandleInput(MouseInput mi)
		{
			// Are we able to handle this event?
			if (!Visible || !GetEventBounds().Contains(mi.Location.X,mi.Location.Y))
				return false;
			
			// Can any of our children handle this?
			foreach (var child in Children)
				if (child.HandleInput(mi))
					return true;

			// Mousedown
			if (InputHandler.Value != null && mi.Event == MouseInputEvent.Down)
				return InputHandler.Value.OnMouseDown(this, mi);
			
			// Mouseup
			if (InputHandler.Value != null && mi.Event == MouseInputEvent.Up)
				return InputHandler.Value.OnMouseUp(this, mi);
			
			// Mousemove
			if (InputHandler.Value != null && mi.Event == MouseInputEvent.Move)
				return InputHandler.Value.OnMouseMove(this, mi);
			return false;
		}
		
		public virtual void Draw()
		{
			if (Visible)
				foreach (var child in Children)
					child.Draw();
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
				if (child.GetWidget(id) != null)
					return child;
			
			return null;
		}
		
	}

	class ContainerWidget : Widget { }
}