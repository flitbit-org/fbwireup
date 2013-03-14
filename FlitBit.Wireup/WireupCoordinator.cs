#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using FlitBit.Wireup.Configuration;

namespace FlitBit.Wireup
{
	/// <summary>
	///   Utility class for coordinating wireup.
	/// </summary>
	public static class WireupCoordinator
	{
		static bool __initialized;
		static bool __reentryDetected;
		static readonly object Sync = new Object();

		static readonly Lazy<IWireupCoordinator> Coordinator = new Lazy<IWireupCoordinator>(PerformBootstrapCurrentProcess,
																																												LazyThreadSafetyMode.ExecutionAndPublication);

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

									// in case there is no config; make sure this assembly is whole...
									coord.WireupDependencies(Assembly.GetExecutingAssembly());
									var config = WireupConfigurationSection.Instance;
									foreach (var e in config.Assemblies.OrderBy(e => e.Ordinal))
									{
										e.PerformWireup(coord);
									}
									var domain = AppDomain.CurrentDomain;
									if (config.WireupAllRunningAssemblies)
									{
										foreach (var asm in domain.GetAssemblies())
										{
											coord.NotifyAssemblyLoaded(asm);
										}
									}
									if (config.HookAssemblyLoad)
									{
										domain.AssemblyLoad +=
											(sender, e) => coord.NotifyAssemblyLoaded(e.LoadedAssembly);
									}

									__initialized = true;
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
		///   Causes the wireup coordinator to self-configure.
		/// </summary>
		/// <returns>the coordinator after it self-configures</returns>
		public static IWireupCoordinator SelfConfigure()
		{
			var coordinator = Instance;
			coordinator.WireupDependencies(Assembly.GetCallingAssembly());
			return coordinator;
		}

		static IWireupCoordinator PerformBootstrapCurrentProcess() { return WireupConfigurationSection.Instance.Coordinator; }
	}
}