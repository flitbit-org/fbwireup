using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlitBit.Wireup.Meta;
using FlitBit.Wireup.Recording;

namespace FltBit.Wireup.Tests
{
	public class SampleWireupTask: WireupTaskAttribute
	{
		protected override void PerformTask(FlitBit.Wireup.IWireupCoordinator coordinator, WireupContext context)
		{
			context.Sequence.Push("SampleWireupTask wired.");
		}
	}
}
