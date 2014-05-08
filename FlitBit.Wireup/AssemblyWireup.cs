#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System;
using System.Linq;
using FlitBit.Wireup;
using FlitBit.Wireup.Configuration;
using FlitBit.Wireup.Meta;
using FlitBit.Wireup.Recording;

[assembly: Wireup(typeof(AssemblyWireup))]

namespace FlitBit.Wireup
{
	/// <summary>
	///   Wires up this assembly.
	/// </summary>
	public sealed class AssemblyWireup : IWireupCommand
	{
		#region IWireupCommand Members

		/// <summary>
		///   Wires up this assembly.
		/// </summary>
		/// <param name="coordinator"></param>
		public void Execute(IWireupCoordinator coordinator)
		{
			var context = WireupContext.Current;
			if (context == null)
			{
				throw new InvalidOperationException("Missing ambient wireup context.");
			}

			var config = WireupConfigurationSection.Instance;
			foreach (var e in config.Assemblies.OrderBy(e => e.Ordinal))
			{
				context.Sequence.Push(String.Concat("Configured to wire on bootstrap: ", e.AssemblyName));
				e.PerformWireup(coordinator, context);
			}
			var domain = AppDomain.CurrentDomain;
			if (config.WireupAllRunningAssemblies)
			{
				context.Sequence.Push("Configured to wireup all running assemblies...");
				foreach (var asm in domain.GetAssemblies())
				{
					if (config.IsIgnored(asm))
					{
						context.Sequence.Push(String.Concat("... ignoring ", asm.FullName));
					}
					else
					{
						context.Sequence.Push(String.Concat("... wiring ", asm.FullName));
						coordinator.NotifyAssemblyLoaded(asm);
					}
				}
			}
			if (config.HookAssemblyLoad)
			{
				context.Sequence.Push("Configured to hook AssemblyLoad event; attaching event handler.");
				domain.AssemblyLoad +=
					(sender, e) =>
					{
						var asm = e.LoadedAssembly;
					  using (var cx = WireupContext.NewOrShared(coordinator, c => c.InitialAssembly(coordinator, asm)))
					  {
					    cx.Sequence.Push(String.Concat("AssemblyLoad event assembly: ", asm.FullName));

					    if (config.IsIgnored(e.LoadedAssembly))
					    {
					      cx.Sequence.Push(String.Concat("... ignoring ", asm.FullName));
					    }
					    else
					    {
					      cx.Sequence.Push(String.Concat("... wiring ", asm.FullName));
					      coordinator.NotifyAssemblyLoaded(e.LoadedAssembly);
					    }
					  }
					};
			}
		}

		#endregion
	}
}