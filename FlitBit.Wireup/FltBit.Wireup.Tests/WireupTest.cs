using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using FlitBit.Wireup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace FltBit.Wireup.Tests
{
	[TestClass]
	public class WireupTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			var coordinator = WireupCoordinator.SelfConfigure();
			Assert.IsNotNull(coordinator);
			var report = coordinator.ReportWireupHistory();
			Assert.IsNotNull(report);
		}
	}
}
