using System;
using System.Reflection;
using FlitBit.Wireup.Recording;

namespace FlitBit.Wireup
{
	internal static class ProcessingExtensions
	{
		internal static void FirstWireupAssembly(this WiredAssembly wired, IWireupCoordinator coordinator,
			WireupContext context, Assembly asm)
		{
			if (!wired.HasDeclarations)
			{
				context.Sequence.Push(String.Concat("Assembly does not make wireup declarations: ", wired.AssemblyName.FullName));
			}
			else
			{
				context.Sequence.BeginScope();
				try
				{
					context.Sequence.Push(String.Concat("Wiring assembly: ", wired.AssemblyName.FullName));
					wired.PerformImmediatePhase(coordinator, context);
					wired.PerformWireup(coordinator, context, asm);
				}
				finally
				{
					context.Sequence.EndScope();
				}
			}
		}

		internal static void SubsequentWireupAssembly(this WiredAssembly wired, IWireupCoordinator coordinator,
			WireupContext context , Assembly asm)
		{
			if (wired.HasDeclarations && !wired.IsWireupComplete)
			{
				context.Sequence.BeginScope();
				try
				{
					context.Sequence.Push(String.Concat("Continuing wireup of assembly: ", wired.AssemblyName.FullName));
					wired.PerformImmediatePhase(coordinator, context);
					wired.PerformWireup(coordinator, context, asm);
				}
				finally
				{
					context.Sequence.EndScope();
				}
			}
		}
	}
}