#region COPYRIGHT© 2009-2013 Phillip Clark. All rights reserved.
// For licensing information see License.txt (MIT style licensing).
#endregion

using System;
using System.Collections.Generic;
using FlitBit.Core;
using FlitBit.Wireup.Properties;

namespace FlitBit.Wireup.Meta
{
	/// <summary>
	/// Attribute declaring a wireup command for an assembly.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true)]
	public sealed class WireupAttribute : Attribute
	{
		Type[] _commands;

		/// <summary>
		/// Creates a new WireupAttribute and initializes the command type.
		/// </summary>
		/// <param name="behaviors">the assembly's wireup behavior</param>
		/// <param name="commandTypes">command types</param>
		public WireupAttribute(WireupBehaviors behaviors, params Type[] commandTypes)
		{
			Behaviors = behaviors;
			var commands = new List<Type>();
			foreach (var t in commandTypes)
			{
				if (typeof(IWireupCommand).IsAssignableFrom(t))
				{
					commands.Add(t);
				}
				else
				{
					throw new ArgumentException(Resources.Chk_TypeMustBeAssignableToIWireupCommand);
				}
			}
			this._commands = commands.ToArray();
		}

		/// <summary>
		/// Indicates the assembly's wireup behaviors.
		/// </summary>
		public WireupBehaviors Behaviors { get; private set; }

		/// <summary>
		/// The command types to be invoked during wireup.
		/// </summary>
		public IEnumerable<Type> CommandType
		{
			get { return _commands.ToReadOnly(); }
		}
	}
}