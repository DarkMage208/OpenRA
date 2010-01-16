﻿using System.Collections.Generic;

namespace OpenRa.Orders
{
	interface IOrderSource
	{
		void SendLocalOrders(int localFrame, List<Order> localOrders);
		List<Order> OrdersForFrame(int currentFrame);
		bool IsReadyForFrame(int frameNumber);
	}
}
