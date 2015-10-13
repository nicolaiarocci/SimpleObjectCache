using System;
using System.IO;

namespace Amica.vNext.SimpleCache
{
    public class SqliteObjectCache : SqliteObjectCacheBase
    {
        protected override string GetDatabasePath()
        {
            const string sqliteFilename = "cache.db3";
            var cacheFolder = Path.Combine(ApplicationName, "SimpleCache");

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var cachePath = Path.Combine(documentsPath, "..", "Library", cacheFolder);
            Directory.CreateDirectory(cachePath);
            return Path.Combine(cachePath, sqliteFilename);

        }
    }
}
