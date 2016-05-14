using System;
using System.IO;
using SQLite;

namespace SimpleCache
{
    public class SqliteObjectCache : SqliteObjectCacheBase
    {
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
