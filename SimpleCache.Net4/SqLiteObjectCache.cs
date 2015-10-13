using System;
using System.IO;

namespace Amica.vNext
{
    public class SqliteObjectCache : SqliteObjectCacheBase
    {
        protected override string GetDatabasePath()
        {
            const string sqliteFilename = "cache.db3";
            var cacheFolder = Path.Combine(ApplicationName, "SimpleCache");

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), cacheFolder);
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, sqliteFilename);
            return path;
        }
    }
}
