using FlitBit.Core.Configuration;

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
		protected override string PerformGetElementKey(WireupConfigurationElement element)
		{
			return element.AssemblyName;
		}
	}
}