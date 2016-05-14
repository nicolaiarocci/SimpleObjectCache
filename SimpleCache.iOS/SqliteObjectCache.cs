using System;
using System.IO;
using SQLite.Net;
using SQLite.Net.Async;
using SimpleCache;

[assembly: Xamarin.Forms.Dependency(typeof(SqliteObjectCache))]

namespace SimpleCache
{
    public class SqliteObjectCache : SqliteObjectCacheBase
    {
        protected override SQLiteAsyncConnection PlatformConnection()
        {
            var dbPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.Personal), 
				Path.Combine(ApplicationName, "SimpleCache"));

            Directory.CreateDirectory(dbPath);

            var lockedConnection = new SQLiteConnectionWithLock(
				new SQLite.Net.Platform.XamarinIOS.SQLitePlatformIOS(), 
				new SQLiteConnectionString(
					Path.Combine(dbPath, "cache.db3"), 
					true));

            return new SQLiteAsyncConnection(() => lockedConnection);
        }
    }
}
