#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System;
using System.Configuration;
using System.Reflection;
using System.Threading;
using FlitBit.Core;

namespace FlitBit.Wireup
{
	/// <summary>
	/// Utility class for coordinating wireup.
	/// </summary>
	public static class WireupCoordinator
	{
		static bool __initialized;
		static readonly Lazy<IWireupCoordinator> _coordinator = new Lazy<IWireupCoordinator>(PerformBootstrapCurrentProcess, LazyThreadSafetyMode.ExecutionAndPublication);
		static WeakReference<WireupConfigurationSection> _config;
		/// <summary>
		/// Accesses the singleton IWireupCoordinator instance.
		/// </summary>
		public static IWireupCoordinator Instance
		{
			get
			{
				var coord = _coordinator.Value;
				if (!__initialized)
				{
					foreach (WireupConfgiurationElement e in _config.StrongTarget.Assemblies)
					{
						e.PerformWireup(coord);
					}
					// in case there is no config; make sure this assembly is whole...
					coord.WireupDependencies(Assembly.GetExecutingAssembly());

					__initialized = true;
				}
				return coord;
			}
		}

		/// <summary>
		/// Causes the wireup coordinator to self-configure.
		/// </summary>
		/// <returns>the coordinator after it self-configures</returns>
		public static IWireupCoordinator SelfConfigure()
		{
			var coordinator = WireupCoordinator.Instance;
			coordinator.WireupDependencies(Assembly.GetCallingAssembly());
			return coordinator;
		}

		static IWireupCoordinator PerformBootstrapCurrentProcess()
		{
			WireupConfigurationSection config = ConfigurationManager.GetSection(WireupConfigurationSection.SectionName)
							as WireupConfigurationSection;
			config = config ?? new WireupConfigurationSection();
			_config = new WeakReference<WireupConfigurationSection>(config);

			return config.Coordinator;
		}
	}

}
