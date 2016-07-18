using System;
using System.IO;
using SQLite.Net;
using SQLite.Net.Async;

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

            var lockedConnection = new SQLiteConnectionWithLock(
				new SQLite.Net.Platform.Generic.SQLitePlatformGeneric(),
				new SQLiteConnectionString(
					Path.Combine(dbPath, "cache.db3"), 
					true));

            return new SQLiteAsyncConnection(() => lockedConnection);
        }
    }
}
