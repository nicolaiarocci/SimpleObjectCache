using System;
using System.IO;
using Amica.vNext;

[assembly: Xamarin.Forms.Dependency(typeof(SqliteObjectCache))]

namespace Amica.vNext
{
    public class SqliteObjectCache : SqliteObjectCacheBase
    {
        protected override string GetDatabasePath()
        {
            const string sqliteFilename = "cache.db3";
            var cacheFolder = Path.Combine(ApplicationName, "SimpleCache");

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var cachePath = Path.Combine(documentsPath, cacheFolder);
            Directory.CreateDirectory(cachePath);
            return Path.Combine(cachePath, sqliteFilename);
        }
    }
}
