using FlitBit.Wireup.Meta;
using FltBit.Wireup.Tests;

[assembly: WireupDependency(typeof(FlitBit.Wireup.AssemblyWireup))]
[assembly: Wireup(typeof(SampleWireupCommand))]
