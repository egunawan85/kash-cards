using Xunit;

// DB-backed integration tests share a single (localdb)\MSSQLLocalDB instance and a fixed-name
// catalog (TestQryptoCard). Running test classes in parallel would let one fixture's
// drop/recreate clobber another's mid-run. Serialize the whole assembly; cross-process
// serialization is handled separately by the named global mutex in LocalDbFixture.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
