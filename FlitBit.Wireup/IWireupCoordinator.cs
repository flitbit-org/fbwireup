#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using FlitBit.Wireup.CodeContracts;
using FlitBit.Wireup.Meta;
using FlitBit.Wireup.Recording;

namespace FlitBit.Wireup
{
	/// <summary>
	///   Ensures assemblies are wired up according to their declared
	///   wireup dependencies and that all wireup commands are given
	///   a chance to run.
	/// </summary>
	[ContractClass(typeof(ContractForIWireupCoordinator))]
	public interface IWireupCoordinator
	{
		/// <summary>
		///   Gets the wireup context history.
		/// </summary>
		IEnumerable<WireupContext> ContextHistory { get; }

		/// <summary>
		///   Called by the framework when an assembly is loaded.
		/// </summary>
		/// <param name="assembly"></param>
		void NotifyAssemblyLoaded(Assembly assembly);

		/// <summary>
		///   Registers an observer.
		/// </summary>
		/// <param name="observer"></param>
		void RegisterObserver(IWireupObserver observer);

		/// <summary>
		///   Creates a string reporting of the wireup history.
		/// </summary>
		/// <returns></returns>
		string ReportWireupHistory();

		/// <summary>
		///   Unregisters an observer.
		/// </summary>
		/// <param name="observerKey"></param>
		void UnregisterObserver(Guid observerKey);

		/// <summary>
		///   Coordinates the wireup of an assembly.
		/// </summary>
		/// <param name="context">the context</param>
		/// <param name="assembly">the assembly</param>
		WiredAssembly WireupDependencies(WireupContext context, Assembly assembly);

		/// <summary>
		///   Directly wires up a dependency (if it is not already wired).
		/// </summary>
		/// <param name="context">the context</param>
		/// <param name="type">the type</param>
		WiredType WireupDependency(WireupContext context, Type type);

		/// <summary>
		/// Notifies the wireup coordinator and observers when a task is performed.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="task"></param>
		/// <param name="targetType"></param>
		void NotifyTaskObservers(WireupContext context, 
			WireupTaskAttribute task, Type targetType);
	}

	namespace CodeContracts
	{
		/// <summary>
		///   CodeContracts Class for IWireupCoordinator
		/// </summary>
		[ContractClassFor(typeof(IWireupCoordinator))]
		internal abstract class ContractForIWireupCoordinator : IWireupCoordinator
		{
			#region IWireupCoordinator Members

			/// <summary>
			///   Called by the framework when an assembly is loaded.
			/// </summary>
			/// <param name="assembly"></param>
			public void NotifyAssemblyLoaded(Assembly assembly)
			{
				Contract.Requires<ArgumentNullException>(assembly != null);
				throw new NotImplementedException();
			}

			/// <summary>
			///   Registers an observer.
			/// </summary>
			/// <param name="observer"></param>
			public void RegisterObserver(IWireupObserver observer)
			{
				Contract.Requires<ArgumentNullException>(observer != null);

				throw new NotImplementedException();
			}

			/// <summary>
			///   Unregisters an observer.
			/// </summary>
			/// <param name="observerKey"></param>
			public void UnregisterObserver(Guid observerKey)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			///   Coordinates the wireup of an assembly.
			/// </summary>
			/// <param name="context"></param>
			/// <param name="assembly"></param>
			public WiredAssembly WireupDependencies(WireupContext context, Assembly assembly)
			{
				Contract.Requires<ArgumentNullException>(assembly != null);
				Contract.Ensures(Contract.Result<WiredAssembly>() != null);

				throw new NotImplementedException();
			}

			/// <summary>
			///   Directly wires up a dependency (if it is not already wired).
			/// </summary>
			/// <param name="context"></param>
			/// <param name="type"></param>
			public WiredType WireupDependency(WireupContext context, Type type)
			{
				Contract.Requires<ArgumentNullException>(type != null);
				Contract.Ensures(Contract.Result<WiredType>() != null);

				throw new NotImplementedException();
			}

			/// <summary>
			///   Creates a string reporting of the wireup history.
			/// </summary>
			/// <returns></returns>
			public string ReportWireupHistory()
			{
				Contract.Ensures(Contract.Result<string>() != null);

				throw new NotImplementedException();
			}

			public IEnumerable<WireupContext> ContextHistory { get { throw new NotImplementedException(); } }

			#endregion


			public void NotifyTaskObservers(WireupContext context, WireupTaskAttribute task, Type targetType)
			{
				Contract.Requires<ArgumentNullException>(context != null);
				Contract.Requires<ArgumentNullException>(task != null);

				throw new NotImplementedException();
			}
		}
	}
}