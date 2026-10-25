// Every test builds its own documents, networks, hosts and stores, so test classes run side by side, the way
// xUnit ran them (each class was its own test collection there), while the tests within a class still run one
// after another.
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
