#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Configuration;
using FlitBit.Core.Configuration;
using FlitBit.Wireup.Properties;

namespace FlitBit.Wireup.Configuration
{
	/// <summary>
	///   Configuration element collection for wireup elements.
	/// </summary>
	public class WireupConfigurationElementCollection :
		AbstractConfigurationElementCollection<WireupConfigurationElement, string>
	{
		/// <summary>
		///   Gets the element's key
		/// </summary>
		/// <param name="element">the element</param>
		/// <returns>the key</returns>
		protected override string PerformGetElementKey(WireupConfigurationElement element) { return element.AssemblyName; }
	}

	/// <summary>
	///   Configuration section for wireup.
	/// </summary>
	public sealed class WireupConfigurationSection : ConfigurationSection
	{
		internal const string SectionName = "flitbit.wireup";
		const string PropertyNameAssemblies = "assemblies";
		const string PropertyNameHookAssemblyLoad = "hookAssemblyLoad";
		const string PropertyNameType = "type";
		const string PropertyNameWireupAllRunningAssemblies = "wireupAllRunningAssemblies";

		/// <summary>
		///   Gets the collection of configured assemblies.
		/// </summary>
		[ConfigurationProperty(PropertyNameAssemblies, IsDefaultCollection = true)]
		public WireupConfigurationElementCollection Assemblies
		{
			get { return (WireupConfigurationElementCollection) this[PropertyNameAssemblies]; }
		}

		/// <summary>
		///   Indicates whether assemblies should be automatically wired up upon assembly load.
		/// </summary>
		[ConfigurationProperty(PropertyNameHookAssemblyLoad, DefaultValue = true)]
		public bool HookAssemblyLoad
		{
			get { return (bool) this[PropertyNameHookAssemblyLoad]; }
			set { this[PropertyNameHookAssemblyLoad] = value; }
		}

		/// <summary>
		///   Gets and sets the name of the configuration coordinator type.
		/// </summary>
		[ConfigurationProperty(PropertyNameType, IsRequired = false)]
		public String TypeName
		{
			get { return (String) this[PropertyNameType]; }
			set { this[PropertyNameType] = value; }
		}

		/// <summary>
		///   Indicates whether a call to the wireup coordinator's SelfConfigure method should wireup all
		///   running assemblies.
		/// </summary>
		[ConfigurationProperty(PropertyNameWireupAllRunningAssemblies, DefaultValue = true)]
		public bool WireupAllRunningAssemblies
		{
			get { return (bool) this[PropertyNameWireupAllRunningAssemblies]; }
			set { this[PropertyNameWireupAllRunningAssemblies] = value; }
		}

		internal IWireupCoordinator Coordinator
		{
			get
			{
				IWireupCoordinator coordinator = null;
				var typeName = this.TypeName;
				if (!String.IsNullOrEmpty(typeName))
				{
					var type = Type.GetType(typeName, true, false);
					if (!typeof(IWireupCoordinator).IsAssignableFrom(type))
					{
						throw new ConfigurationErrorsException(String.Concat(Resources.Err_ConfiguredWireupCoordinatorTypeMismatch,
																																typeName));
					}

					coordinator = (IWireupCoordinator) Activator.CreateInstance(type);
				}
				return coordinator ?? new DefaultWireupCoordinator();
			}
		}

		internal static WireupConfigurationSection Instance
		{
			get
			{
				var config = ConfigurationManager.GetSection(SectionName)
					as WireupConfigurationSection;
				return config ?? new WireupConfigurationSection();
			}
		}
	}
}