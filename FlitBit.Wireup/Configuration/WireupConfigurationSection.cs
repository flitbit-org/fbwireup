#region COPYRIGHT© 2009-2012 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion


using System;
using System.Configuration;
using FlitBit.Core.Configuration;
using FlitBit.Wireup.Properties;

namespace FlitBit.Wireup
{
	/// <summary>
	/// Configuration element collection for wireup elements.
	/// </summary>
	public class WireupConfigurationElementCollection : AbstractConfigurationElementCollection<WireupConfgiurationElement, string>
	{
		/// <summary>
		/// Gets the element's key
		/// </summary>
		/// <param name="element">the element</param>
		/// <returns>the key</returns>
		protected override string PerformGetElementKey(WireupConfgiurationElement element)
		{
			return element.AssemblyName;
		}
	}

	/// <summary>
	/// Configuration section for wireup.
	/// </summary>
	public sealed class WireupConfigurationSection : ConfigurationSection
	{
		const string PropertyName_type = "type";
		const string PropertyName_hookAssemblyLoad = "hookAssemblyLoad";
		const string PropertyName_wireupAllRunningAssemblies = "wireupAllRunningAssemblies";
		const string PropertyName_assemblies = "assemblies";
		internal const string SectionName = "flitbit.wireup";

		/// <summary>
		/// ??
		/// </summary>
		[ConfigurationProperty(WireupConfigurationSection.PropertyName_type, IsRequired = false)]
		public String TypeName
		{
			get { return (String)this[PropertyName_type]; }
			set { this[PropertyName_type] = value; }
		}

		/// <summary>
		/// Indicates whether assemblies should be automatically wired up upon assembly load.
		/// </summary>
		[ConfigurationProperty(WireupConfigurationSection.PropertyName_hookAssemblyLoad, IsRequired = false)]
		public bool HookAssemblyLoad
		{
			get { return (bool)this[PropertyName_hookAssemblyLoad]; }
			set { this[PropertyName_hookAssemblyLoad] = value; }
		}

		/// <summary>
		/// Indicates whether a call to the wireup coordinator's SelfConfigure method should wireup all
		/// running assemblies.
		/// </summary>
		[ConfigurationProperty(WireupConfigurationSection.PropertyName_wireupAllRunningAssemblies, IsRequired = false)]
		public bool WireupAllRunningAssemblies
		{
			get { return (bool)this[PropertyName_wireupAllRunningAssemblies]; }
			set { this[PropertyName_wireupAllRunningAssemblies] = value; }
		}

		/// <summary>
		/// Gets the collection of configured assemblies.
		/// </summary>
		[ConfigurationProperty(WireupConfigurationSection.PropertyName_assemblies, IsDefaultCollection = true)]
		public WireupConfigurationElementCollection Assemblies
		{
			get { return (WireupConfigurationElementCollection)this[PropertyName_assemblies]; }
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
						throw new ConfigurationErrorsException(String.Concat(Resources.Err_ConfiguredWireupCoordinatorTypeMismatch, typeName));
				
					coordinator = (IWireupCoordinator)Activator.CreateInstance(type);
				}				
				return coordinator ?? new DefaultWireupCoordinator();
			}
		}
	}
}