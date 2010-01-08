﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class Mobile : IIssueOrder, IResolveOrder, IOccupySpace, IMovement
	{
		readonly Actor self;

		int2 __fromCell;
		public int2 fromCell
		{
			get { return __fromCell; }
			set { Game.UnitInfluence.Remove(self, this); __fromCell = value; Game.UnitInfluence.Add(self, this); }
		}
		public int2 toCell
		{
			get { return self.Location; }
			set
			{
				if (self.Location != value)
				{
					Game.UnitInfluence.Remove(self, this);
					self.Location = value;
					self.Owner.Shroud.Explore(self);
				}
				Game.UnitInfluence.Add(self, this);
			}
		}

		public Mobile(Actor self)
		{
			this.self = self;
			__fromCell = toCell;
			Game.UnitInfluence.Add(self, this);
		}

		public void TeleportTo(Actor self, int2 xy)
		{
			fromCell = toCell = xy;
			self.CenterLocation = Util.CenterOfCell(fromCell);
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;
		
			if (underCursor != null)
			{
				// force-move
				if (!mi.Modifiers.HasModifier(Modifiers.Alt)) return null;
				if (!Game.IsActorCrushableByActor(underCursor, self)) return null;
			}

			if (Util.GetEffectiveSpeed(self) == 0) return null;		/* allow disabling move orders from modifiers */
			if (xy == toCell) return null;
			return new Order("Move", self, null, xy, null);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				self.CancelActivity();
				self.QueueActivity(new Activities.Move(order.TargetLocation, 8));
			}
		}

		public IEnumerable<int2> OccupiedCells()
		{
			return (fromCell == toCell)
				? new[] { fromCell } 
				: new[] { fromCell, toCell };
		}

		public UnitMovementType GetMovementType()
		{
			switch (Rules.UnitCategory[self.Info.Name])
			{
				case "Infantry":
					return UnitMovementType.Foot;
				case "Vehicle":
					return (self.Info as VehicleInfo).Tracked ? UnitMovementType.Track : UnitMovementType.Wheel;
				case "Ship":
					return UnitMovementType.Float;
				case "Plane":
					return UnitMovementType.Fly;
				default:
					throw new InvalidOperationException("GetMovementType on unit that shouldn't be able to move.");
			}
		}
		
		public bool CanEnterCell(int2 a)
		{
			if (!Game.BuildingInfluence.CanMoveHere(a)) return false;

			var crushable = true;
			foreach (Actor actor in Game.UnitInfluence.GetUnitsAt(a))
			{
				if (actor == self) continue;
				
				if (!Game.IsActorCrushableByActor(actor, self))
				{
					crushable = false;
					break;
				}
			}
			
			if (!crushable) return false;
			
			return Rules.Map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(GetMovementType(),
					Rules.TileSet.GetWalkability(Rules.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}

		public IEnumerable<int2> GetCurrentPath()
		{
			var move = self.GetCurrentActivity() as Activities.Move;
			if (move == null || move.path == null) return new int2[] { };
			return Enumerable.Reverse(move.path);
		}
	}
}
