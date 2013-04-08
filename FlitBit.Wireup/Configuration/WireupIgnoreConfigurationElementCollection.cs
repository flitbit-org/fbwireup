using FlitBit.Core.Configuration;

namespace FlitBit.Wireup.Configuration
{
	/// <summary>
	///   Configuration element collection for ignore elements.
	/// </summary>
	public class WireupIgnoreConfigurationElementCollection :
		AbstractConfigurationElementCollection<WireupIgnoreConfigurationElement, string>
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public WireupIgnoreConfigurationElementCollection()	: base("add", "clear", "remove")
		{
			this.Add(new WireupIgnoreConfigurationElement()
			{
				AssemblyMatchSpec = "mscorlib",
				Strategy = WireupIgnoreStrategy.Exact
			});
			this.Add(new WireupIgnoreConfigurationElement()
			{
				AssemblyMatchSpec = "System.",
				Strategy = WireupIgnoreStrategy.StartsWith
			});
			this.Add(new WireupIgnoreConfigurationElement()
			{
				AssemblyMatchSpec = "Microsoft.",
				Strategy = WireupIgnoreStrategy.StartsWith
			});
			this.Add(new WireupIgnoreConfigurationElement()
			{
				AssemblyMatchSpec = "Newtonsoft.",
				Strategy = WireupIgnoreStrategy.StartsWith
			});
		}
		/// <summary>
		///   Gets the element's key
		/// </summary>
		/// <param name="element">the element</param>
		/// <returns>the key</returns>
		protected override string PerformGetElementKey(WireupIgnoreConfigurationElement element) { return element.AssemblyMatchSpec; }
	}
}