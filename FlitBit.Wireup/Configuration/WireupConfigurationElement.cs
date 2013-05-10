#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Configuration;
using System.Diagnostics.Contracts;
using System.Reflection;
using FlitBit.Wireup.Recording;

namespace FlitBit.Wireup.Configuration
{
	/// <summary>
	///   Configuration element for wiring up an assembly.
	/// </summary>
	public class WireupConfigurationElement : ConfigurationElement
	{
		const string PropertyNameAssembly = "assembly";
		const string PropertyNameOrdinal = "ordinal";
		Assembly _asm;

		/// <summary>
		///   The name of the assembly upon which wireup is performed.
		/// </summary>
		[ConfigurationProperty(PropertyNameAssembly
			, IsKey = true
			, IsRequired = true)]
		public string AssemblyName { get { return (string) this[PropertyNameAssembly]; } set { this[PropertyNameAssembly] = value; } }

		/// <summary>
		///   The ordinal; indicates the order in which assemblies are registered.
		/// </summary>
		[ConfigurationProperty(PropertyNameOrdinal, DefaultValue = 0)]
		public int Ordinal { get { return (int) this[PropertyNameOrdinal]; } set { this[PropertyNameOrdinal] = value; } }

		internal Assembly ResolveAssembly
		{
			get
			{
				if (this._asm == null && !String.IsNullOrEmpty(this.AssemblyName))
				{
					this._asm = Assembly.Load(this.AssemblyName);
				}
				return this._asm;
			}
		}

		internal void PerformWireup(IWireupCoordinator coordinator, WireupContext context)
		{
			Contract.Requires<ArgumentNullException>(coordinator != null);
			Contract.Requires<ArgumentNullException>(context != null);
			coordinator.WireupDependencies(context, this.ResolveAssembly);
		}
	}
}