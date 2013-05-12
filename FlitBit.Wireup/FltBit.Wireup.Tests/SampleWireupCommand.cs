using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlitBit.Wireup;
using FlitBit.Wireup.Recording;

namespace FltBit.Wireup.Tests
{
	[SampleWireupTask]
	public class SampleWireupCommand: WireupCommand
	{
		protected override void PerformWireup(IWireupCoordinator coordinator)
		{
			var context = WireupContext.Current;
			context.Sequence.Push("SampleWireupCommand wired.");
		}
	}
}
