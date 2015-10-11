using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SQLite;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

[assembly:InternalsVisibleTo("Tests")]

namespace Amica.vNext.SimpleCache
{

    public abstract class SqLiteObjectCacheBase : IObjectCache
    {
        private static SQLiteAsyncConnection _connection;
        private string _applicationName;

        /// <summary>
        /// Your application's name. Set this at startup, this defines where
        /// your data will be stored (usually at %AppData%\[ApplicationName])
        /// </summary>
        public string ApplicationName
        {
            get
            {
                if (_applicationName == null)
                    throw new Exception("Make sure to set ApplicationName on startup");

                return _applicationName;
            }
	    set { _applicationName = value; }
        }

        /// <summary>
        /// Returns the appropriate database path according to the operating
        /// system on which we are running. If the path does not exist yet,
        /// create it.
        /// </summary>
        /// <returns>The intended location, filename included, for the cache 
        /// database.</returns>
        protected abstract string GetDatabasePath();


	/// <summary>
	///  Returns an open connection to the cache database. If necessary,
	/// initialize the database too.
	/// </summary>
	/// <returns>An active connection to the cache database.</returns>
        private SQLiteAsyncConnection GetConnection()
        {
            if (_connection != null) return _connection;

            _connection = new SQLiteAsyncConnection(GetDatabasePath(), storeDateTimeAsTicks: true);
            _connection.CreateTableAsync<CacheElement>();
            return _connection;
        }

	/// <summary>
	/// Deserializes a byte array into an object.
	/// </summary>
	/// <typeparam name="T">Object type.</typeparam>
	/// <param name="data">Input array.</param>
	/// <returns>The object resulting from the deseralization of the input array.</returns>
        private static T DeserializeObject<T>(byte[] data)
        {
            var serializer = JsonSerializer.Create();
            var reader = new BsonReader(new MemoryStream(data));

	    return serializer.Deserialize<T>(reader);
        }

	/// <summary>
	/// Serializes an object into a byte array.
	/// </summary>
	/// <typeparam name="T">Object type.</typeparam>
	/// <param name="value">Object instance.</param>
	/// <returns>A byte array representing the serialization of the input object.</returns>
	private static byte [] SerializeObject<T>(T value)
	{
	    var serializer = JsonSerializer.Create();
	    var ms = new MemoryStream();
	    var writer = new BsonWriter(ms);
	    serializer.Serialize(writer, value);
	    return ms.ToArray();
	}

        public async Task<T> Get<T>(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var conn = GetConnection();

            var element = await conn.FindAsync<CacheElement>(key);
            if (element == null)
                throw new KeyNotFoundException(nameof(key));

            return DeserializeObject<T>(element.Value);
        }

        public async Task<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var conn = GetConnection();

            var element = await conn.FindAsync<CacheElement>(key);

            return element?.CreatedAt;
        }

        public async Task<IEnumerable<T>> GetAll<T>()
        {
            var conn = GetConnection();
            var query = conn.Table<CacheElement>().Where(v => v.TypeName == typeof (T).FullName);

            var elements = new List<T>();
            await query.ToListAsync().ContinueWith(t =>
            {
                elements.AddRange(t.Result.Select(element => DeserializeObject<T>(element.Value)));
            }
	    );
            return elements.AsEnumerable();
        }

        public async Task<int> Insert<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var data = SerializeObject(value);
            var exp = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;
            var createdAt = DateTimeOffset.Now.UtcDateTime;

            var conn = GetConnection();
            return await conn.InsertOrReplaceAsync(new CacheElement()
            {
                Key = key,
                TypeName = typeof (T).FullName,
                Value = data,
                CreatedAt = createdAt,
                Expiration = exp
            });
        }

        public async Task<int> Invalidate<T>(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var conn = GetConnection();

            var element = await conn.FindAsync<CacheElement>(key);
            if (element == null)
                throw new KeyNotFoundException(nameof(key));

            var typeName = typeof (T).FullName;
            if (element.TypeName != typeName)
                throw new TypeMismatchException();

            return await conn.DeleteAsync(element);
        }

        public Task<int> InvalidateAll<T>()
        {
            var conn = GetConnection();

            var typeName = typeof (T).FullName;
            return conn.ExecuteAsync($"DELETE FROM CacheElement WHERE TypeName = '{typeName}'");
        }

        public Task Vacuum()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
                _connection = null;
        }
    }
}
