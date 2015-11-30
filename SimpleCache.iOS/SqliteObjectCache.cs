using System;
using System.IO;
using Amica.vNext;
using SQLite.Net;
using SQLite.Net.Async;

[assembly: Xamarin.Forms.Dependency(typeof(SqliteObjectCache))]

namespace Amica.vNext
{
    public class SqliteObjectCache : SqliteObjectCacheBase
    {
        protected override SQLiteAsyncConnection GetPlatformConnection()
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
