// Every test class shares one in-memory server per class; the classes themselves may run side by side.
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
