#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion


using System;
using System.Configuration;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace FlitBit.Wireup
{
	/// <summary>
	/// Configuration element for wiring up an assembly.
	/// </summary>
	public class WireupConfgiurationElement : ConfigurationElement
	{
		const string PropertyName_assembly = "assembly";
		Assembly _asm;

		/// <summary>
		/// The name of the assembly upon which wireup is performed.
		/// </summary>
		[ConfigurationProperty(PropertyName_assembly
			, IsKey = true
			, IsRequired = true)]
		public string AssemblyName
		{
			get { return (string)this[PropertyName_assembly]; }
			set { this[WireupConfgiurationElement.PropertyName_assembly] = value; }
		}

		internal Assembly ResolveAssembly
		{
			get
			{
				if (_asm == null && !String.IsNullOrEmpty(this.AssemblyName))
				{
					_asm = Assembly.Load(this.AssemblyName);
				}
				return _asm;
			}
		}

		internal void PerformWireup(IWireupCoordinator coordinator)
		{
			Contract.Requires<ArgumentNullException>(coordinator != null);
			coordinator.WireupDependencies(this.ResolveAssembly);
		}
	}
}
