using FlitBit.Wireup;
using FlitBit.Wireup.Meta;
using AssemblyWireup = $rootnamespace$.AssemblyWireup;

[assembly: Wireup(typeof(AssemblyWireup))]

namespace $rootnamespace$
{
  /// <summary>
  ///   Wires up this assembly.
  /// </summary>
  public sealed class AssemblyWireup : IWireupCommand
  {
    /// <summary>
    ///   Called by the wireup framework when this assembly is wired.
    /// </summary>
    /// <param name="coordinator"></param>
    public void Execute(IWireupCoordinator coordinator)
    {
      // TODO: Add code that prepares your assembly for use. 
      // When this method exits your assembly should be in a ready-state.

    }

  }
}