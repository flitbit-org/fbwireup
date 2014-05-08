# fbwireup - FlitBit Wireup

Tools for declaring and coordinating bootstrap activities in dotNet applications.

## Summary
**_FlitBit Wireup_** formalizes bootstrap processes by providing the ability to declare dependencies among assemblies and coordinate the tasks that bring an assembly to a ready-state.

It deals with the dependency among libraries, not dependency injection or inversion of control. Those concerns are covered by many libraries in the open-source community, including [FlitBit IoC](https://github.com/flitbit-org/fbioc) which scaffolds on top of this library.

Visit the [Core library's repository](https://github.com/flitbit-org/fbcore) to learn more about **_FlitBit Frameworks_**.

## Background

It may appear that we use the term _wireup_ and _bootstrap_ interchangeably, but it is not so. **Bootstrap** refers to the period between when the program is launched and when it becomes ready. 
We use the term **_wireup_** to refer to the more abstract phase that repeats throughout the life of a modern application as different modules are loaded _just-in-time_ and must be made ready.

Too often, applications either devise custom bootstrap processes and end up with hard-coded dependency logic; yet others rely on a mix of cusotm logic parts and _inversion of control_ to achieve a ready-state.
In the latter case, relying on an IoC container is a good move, but this library's purpose is to introduce a reusable convention for the other part; logic and tasks required to bring a module to its initial ready-state.

As an illustration of the coupling _FlitBit Wireup_ intends to modularize, take most ASP.net applications. They tend to centralize too much logic in the [HttpApplication](http://msdn.microsoft.com/en-us/library/system.web.httpapplication.aspx)'s **Application_Start** method, essentially creating a [know-it-all](http://en.wikipedia.org/wiki/God_object) bootstrapper.
We have learned through much tribulation to prefer a modular, decentralized approach for our applications.

The approach we take leads to modularity not only in packaging capability, but also in configuring it.

1. Each module should declare its own dependencies.
  * In this way, a coordinator can ensure that all dependencies are met before each module is _wired_ into an application.
2. If a module has logic that must be performed in order to bring itself to a ready-state, it must declare one or more wireup tasks that perform the required logic.
  * In this way, when the coordinator _wires_ a module into an application, it can ensure that the tasks are performed in the proper, declared order, and thereby reach certainty that the module has reached a ready-state without ever having to know the module's purpose.

In contrast to the _know-it-all_ model, modules can be easily _wired_ into a running application on demand, reducing the start-up time and eliminating the wireup logic for areas of the application that are unused.

```
\\ TODO: Copious examples
```
