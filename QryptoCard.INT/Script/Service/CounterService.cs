using System.Data.SqlClient;
using System.Linq;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// Atomic monotonic counters (tblM_Setting_Counter). Replaces the read-increment-save pattern
    /// that could hand two concurrent callers the same value and so produce duplicate order IDs.
    /// A single UPDATE ... OUTPUT increments and returns the new value under the row lock, so
    /// concurrent opens/top-ups always get distinct sequence numbers.
    /// </summary>
    public static class CounterService
    {
        public static long Next(long counterId)
        {
            using (var ctx = new DBEntities())
            {
                return ctx.Database.SqlQuery<long>(
                    "UPDATE dbo.tblM_Setting_Counter SET Value = ISNULL(Value, 0) + 1 " +
                    "OUTPUT inserted.Value WHERE ID = @id",
                    new SqlParameter("@id", counterId)).Single();
            }
        }
    }
}
