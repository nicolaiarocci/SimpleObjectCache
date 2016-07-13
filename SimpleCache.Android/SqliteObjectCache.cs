using System;
using System.IO;
using SQLite.Net;
using SQLite.Net.Async;
using SimpleCache;


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
				new SQLite.Net.Platform.XamarinAndroid.SQLitePlatformAndroid(),
				new SQLiteConnectionString(
					Path.Combine(dbPath, "cache.db3"),
					true));

			return new SQLiteAsyncConnection(() => lockedConnection);
		}
	}
}
