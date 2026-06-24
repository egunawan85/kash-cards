using System;
using System.Data.Entity.Infrastructure;
using System.IO;
using QryptoCard.INT;
using Xunit;

namespace QryptoCard.Tests.Integration.LocalDb
{
    // One-shot generator for QryptoCard.Tests.Fixtures/TestData/init.sql. Skip-by-default —
    // un-skip locally, run, commit the resulting init.sql, re-skip. Re-run only when the EDMX
    // (QryptoCard.INT/DB.edmx) changes.
    //
    // EF6 ObjectContext.CreateDatabaseScript() walks the SSDL embedded in QryptoCard.INT.dll
    // (the DBEntities EDMX) and emits CREATE TABLE / CREATE INDEX statements. Output is not
    // idempotent (no IF NOT EXISTS guards), so the fixture drops + recreates the DB around each
    // test class rather than reapplying.
    public class GenerateInitSql_OneShot
    {
        [Fact(Skip = "one-shot — un-skip to regenerate QryptoCard.Tests.Fixtures/TestData/init.sql after EDMX changes")]
        public void GenerateInitSql()
        {
            // Uses the default DBEntities ctor so EF reads name=DBEntities from App.config (the
            // metadata path resolves the embedded EDMX). CreateDatabaseScript only needs the model
            // metadata, not a live database connection.
            using (var db = new DBEntities())
            {
                var script = ((IObjectContextAdapter)db).ObjectContext.CreateDatabaseScript();

                var dir = AppDomain.CurrentDomain.BaseDirectory;
                while (dir != null && !Directory.Exists(Path.Combine(dir, "QryptoCard.Tests.Fixtures", "TestData")))
                    dir = Directory.GetParent(dir)?.FullName;
                Assert.NotNull(dir);

                var outPath = Path.Combine(dir, "QryptoCard.Tests.Fixtures", "TestData", "init.sql");
                File.WriteAllText(outPath, script);
                Assert.True(new FileInfo(outPath).Length > 1000, "Generated init.sql is empty or trivial");
            }
        }
    }
}
