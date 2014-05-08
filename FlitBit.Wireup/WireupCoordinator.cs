#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Reflection;
using System.Threading;
using FlitBit.Wireup.Configuration;
using FlitBit.Wireup.Recording;

namespace FlitBit.Wireup
{
	/// <summary>
	///   Utility class for coordinating wireup.
	/// </summary>
	public static class WireupCoordinator
	{
		static readonly Lazy<IWireupCoordinator> Coordinator = new Lazy<IWireupCoordinator>(PerformBootstrapCurrentProcess,
																																												LazyThreadSafetyMode.ExecutionAndPublication);

		static readonly object Sync = new Object();

		static bool __initialized;
		static bool __reentryDetected;

		/// <summary>
		///   Accesses the singleton IWireupCoordinator instance.
		/// </summary>
		public static IWireupCoordinator Instance
		{
			get
			{
				var coord = Coordinator.Value;
				if (!__initialized)
				{
					lock (Sync)
					{
						if (!__initialized)
						{
							if (!__reentryDetected)
							{
								try
								{
									__reentryDetected = true;

									var thisAssembly = Assembly.GetExecutingAssembly();
									using (var context = WireupContext.NewOrShared(coord, c => c.InitialAssembly(coord, thisAssembly)))
									{
										__initialized = true;
										context.Sequence.EndScope();
									}
								}
								finally
								{
									__reentryDetected = false;
								}
							}
						}
					}
				}
				return coord;
			}
		}

		/// <summary>
		///   Causes the wireup coordinator to self-configure; this will bootstrap the WireupCoordinator if it
		///   has not already been wired, then wireup the calling assembly.
		/// </summary>
		/// <returns>the coordinator after it self-configures</returns>
		public static IWireupCoordinator SelfConfigure()
		{
			// resolve the singleton instance seperately
			// so we cleanly record the bootstrap, then the caller's request.
			var coordinator = Instance;
			var caller = Assembly.GetCallingAssembly();
			using (WireupContext.NewOrShared(coordinator, c => c.InitialAssembly(coordinator, caller)))
			{
				return coordinator;
			}
		}

		static IWireupCoordinator PerformBootstrapCurrentProcess()
		{
			return WireupConfigurationSection.Instance.CreateConfiguredCoordinator();
		}
	}
}