using System;
using System.IO;
using SQLite;

namespace SimpleCache
{
    /// <summary>
    /// Permanent key-value object cache.
    /// </summary>
    public class SqliteObjectCache : SqliteObjectCacheBase
    {
        /// <summary>
        /// Platform (Windows Desktop) connection.
        /// </summary>
        protected override SQLiteAsyncConnection PlatformConnection()
        {
            var dbPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
				Path.Combine(ApplicationName, "SimpleCache"));

            Directory.CreateDirectory(dbPath);

            return new SQLiteAsyncConnection(Path.Combine(dbPath, "cache.db3"));
        }
    }
}
