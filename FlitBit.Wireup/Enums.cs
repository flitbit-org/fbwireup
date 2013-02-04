#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion


namespace FlitBit.Wireup
{
	/// <summary>
	/// Declares wireup behaviors.
	/// </summary>
	public enum WireupBehaviors
	{
		/// <summary>
		/// Indicates the wireup coordinator should only use declarations to 
		/// guide the wireup.
		/// </summary>
		DeclarationsOnly = 0,
		/// <summary>
		/// Indicates the wireup coordinator should use discovery to guide
		/// wireup.
		/// </summary>
		Discovery = 1,
	}

	/// <summary>
	/// Wireup phases relate to an assembly.
	/// </summary>
	public enum WireupPhase
	{
		/// <summary>
		/// Immediately upon discovery.
		/// </summary>
		Immediate = -5,
		/// <summary>
		/// Indicates before dependencies are resolved.
		/// </summary>
		BeforeDependencies = -4,
		/// <summary>
		/// Indicates as dependencies are resolved.
		/// </summary>
		Dependencies = -3,
		/// <summary>
		/// Indicates before tasks are executed.
		/// </summary>
		BeforeTasks = -2,
		/// <summary>
		/// Indicates as tasks are executed.
		/// </summary>
		Tasks = -1,
		/// <summary>
		/// Indicates before wireup.
		/// </summary>
		BeforeWireup = 0,
		/// <summary>
		/// Indicates after wireup.
		/// </summary>
		AfterWireup = 1
	}
}
