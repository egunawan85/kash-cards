using System.Data.Entity;

namespace QryptoCard.INT
{
    // Hand-written partial extending the EDMX-generated DBEntities. The generated DB.Context.cs
    // only declares the parameterless constructor that reads `name=DBEntities` from config.
    // Integration tests need a per-fixture connection string so each test class can point EF at
    // its own throwaway LocalDB database (see QryptoCard.Tests.Fixtures.LocalDbFixture). Lives in
    // this partial so EDMX regeneration never wipes it. Mirrors the sister projects' pattern.
    public partial class DBEntities
    {
        public DBEntities(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }
    }
}
