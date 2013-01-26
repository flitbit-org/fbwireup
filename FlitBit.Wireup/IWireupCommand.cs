#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion


namespace FlitBit.Wireup
{
  /// <summary>
  /// Interface for commands executed at wireup time.
  /// </summary>
  public interface IWireupCommand
  {
    /// <summary>
    /// Executes the command.
    /// </summary>
		/// <param name="coordinator">the wireup coordinator</param>
    void Execute(IWireupCoordinator coordinator);
  }
}
