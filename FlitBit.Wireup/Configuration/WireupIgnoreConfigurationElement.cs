#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System.Configuration;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FlitBit.Wireup.Configuration
{
	/// <summary>
	///   Determines how wireup ignore configuration elements are processed.
	/// </summary>
	public enum WireupIgnoreStrategy
	{
		/// <summary>
		///   Default behavior: matches.
		/// </summary>
		Default = 0,

		/// <summary>
		///   Indicates the assemby name should be matched using regex.
		/// </summary>
		Regex = 0,

		/// <summary>
		///   Indicates the assembly name is a match if it begins with the given value.
		/// </summary>
		StartsWith = 1,

		/// <summary>
		///   Indicates the assembly name is a match if it ends with the given value.
		/// </summary>
		EndsWith = 2,

		/// <summary>
		///   Indicates the assembly name must match exactly.
		/// </summary>
		Exact = 3
	}

	/// <summary>
	///   Configuration element for assemblies to ignore during wireup.
	/// </summary>
	public class WireupIgnoreConfigurationElement : ConfigurationElement
	{
		const string PropertyNameAssembly = "match";
		const string PropertyNameStrategy = "strategy";

		/// <summary>
		///   Assembly match specification.
		/// </summary>
		[ConfigurationProperty(PropertyNameAssembly
			, IsKey = true
			, IsRequired = true)]
		public string AssemblyMatchSpec { get { return (string) this[PropertyNameAssembly]; } set { this[PropertyNameAssembly] = value; } }

		/// <summary>
		///   The strategy used when checking whether assemblies match
		/// </summary>
		[ConfigurationProperty(PropertyNameStrategy, DefaultValue = WireupIgnoreStrategy.Regex)]
		public WireupIgnoreStrategy Strategy { get { return (WireupIgnoreStrategy) this[PropertyNameStrategy]; } set { this[PropertyNameStrategy] = value; } }

		internal bool Matches(Assembly asm)
		{
			if (asm == null)
			{
				return false;
			}
			var name = asm.FullName;
			switch (Strategy)
			{
				case WireupIgnoreStrategy.StartsWith:
					return name.StartsWith(AssemblyMatchSpec);
				case WireupIgnoreStrategy.EndsWith:
					return name.EndsWith(AssemblyMatchSpec);
				default:
					var dmatch = Regex.Match(name, AssemblyMatchSpec, RegexOptions.IgnoreCase);
					return dmatch.Success;
			}
		}
	}
}