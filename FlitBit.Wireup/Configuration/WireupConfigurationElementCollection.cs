﻿#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

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