# Bit.Bcrdt.Testing

Test support for [bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt): the tools its own suite uses to try to break
convergence. Framework-neutral - it works from MSTest, xUnit or NUnit.

```bash
dotnet add package Bit.Bcrdt.Testing
```

## An adversarial network

`BcrdtSimulatedNetwork` connects replicas through a deterministic, seeded network that loses, duplicates, delays and
reorders messages, partitions peers and takes them offline - and runs the real sync protocol over it, so a test of
"edit for an hour on a plane, then reconnect" is a test of the production code path.

```csharp
using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
{
    Seed = seed,
    DropProbability = 0.25,
    DuplicateProbability = 0.25,
    MaxLatencySteps = 5,
});

BcrdtSimulatedPeer[] peers = network.AddPeers(4);
peers[0].GoOffline();
peers[0].Doc.GetText("body").Insert(0, "written offline");
peers[1].Doc.GetText("body").Insert(0, "written online");
peers[0].GoOnline();

network.Flush();
network.SynchronizeAll();
BcrdtConvergenceHarness.AssertConverged(network);
```

## Random operations

`BcrdtOperationGenerator` applies random transactions - text, arrays, maps, formatting and nested types, as
`BcrdtOperationGeneratorOptions` selects - and returns a description of each, so a failure can say what happened:

```csharp
var generator = new BcrdtOperationGenerator(seed);
string applied = generator.ApplyRandomTransaction(peers[2].Doc);
```

`BcrdtConvergenceHarness.AssertConverged` compares replicas' contents and `AssertStructurallyIdentical` their block
structure, and both describe the divergence when they fail. Print the seed with every failure: a randomized test you
cannot replay is an anecdote, not a test.

## Fuzzing the wire format

`BcrdtUpdateCorrupter` turns a valid update into every truncation, single-byte mutations, insertions and plain
garbage. A rejected payload must raise `BcrdtFormatException` and leave the document exactly as it was:

```csharp
foreach (byte[] corrupted in BcrdtUpdateCorrupter.Truncations(update))
{
    // apply to a fresh replica; assert it is either applied or rejected cleanly, never left half-applied
}
```

The full guide is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bcrdt).
