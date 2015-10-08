using System;
using System.IO;
using Amica.vNext.SimpleCache;

namespace SimpleCache.Net4
{
    class SqLiteObjectCache : SqLiteObjectCacheBase
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
