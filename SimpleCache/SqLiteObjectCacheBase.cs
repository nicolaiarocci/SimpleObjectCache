using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SQLite.Net.Async;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

[assembly:InternalsVisibleTo("Tests, PublicKey ="+
 "0024000004800000940000000602000000240000525341310004000001000100c30aa9a63d6e"+
 "0b46598d4a2cd3a17cd848dc468e443ecc27a5e910bf77bb357a9d78c161431c3cc51bb61c72"+
 "6547a6c68219f59a6eeeb2f5a92f708d49db1e63466cb53ef05e4988d6185a8d779d66b01431"+
 "877b75f02109d3e3a54b5eb87d29f180417bd2a03384cbb3b9692df63161313dc770682e7fb81"+
 "14e8cf5120cdfb2")]

namespace Amica.vNext
{

    public abstract class SqliteObjectCacheBase : IBulkObjectCache
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
                    throw new SimpleCacheApplicationNameNullException();

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

        protected abstract SQLiteAsyncConnection GetPlatformConnection();


        private SQLiteAsyncConnection GetConnection()
        {
            if (_connection != null) return _connection;

            _connection = GetPlatformConnection();
			_connection.CreateTableAsync<CacheElement>().ConfigureAwait(false);

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

        /// <summary>
        /// This method is called immediately before writing any data to disk.
        /// Override this in encrypting data stores in order to encrypt the
        /// data.
        /// </summary>
        /// <param name="data">The byte data about to be written to disk.</param>
        /// <returns>A result representing the encrypted data</returns>
        protected virtual byte[] BeforeWriteToDiskFilter(byte[] data)
        {
            return data;
        }

        /// <summary>
        /// This method is called immediately after reading any data to disk.
        /// Override this in encrypting data stores in order to decrypt the
        /// data.
        /// </summary>
        /// <param name="data">The byte data that has just been read from
        /// disk.</param>
        /// <returns>A result representing the decrypted data</returns>
        protected virtual byte[] AfterReadFromDiskFilter(byte[] data)
        {
            return data;
        }

        public async Task<T> Get<T>(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var element = await GetConnection().FindAsync<CacheElement>(key).ConfigureAwait(false);
            if (element == null)
                throw new KeyNotFoundException(nameof(key));

            return DeserializeObject<T>(AfterReadFromDiskFilter(element.Value));
        }

        public async Task<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var element = await GetConnection().FindAsync<CacheElement>(key);

            return element?.CreatedAt;
        }

        public async Task<IEnumerable<T>> GetAll<T>()
        {
            var query = GetConnection().Table<CacheElement>().Where(v => v.TypeName == typeof (T).FullName);

            var elements = new List<T>();
            await query.ToListAsync().ContinueWith(t =>
            {
                elements.AddRange(t.Result.Select(element => DeserializeObject<T>(AfterReadFromDiskFilter(element.Value))));
            }
	    );
            return elements.AsEnumerable();
        }

        public async Task<int> Insert<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var data = BeforeWriteToDiskFilter(SerializeObject(value));
            var exp = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;
            var createdAt = DateTimeOffset.Now.UtcDateTime;

            return await GetConnection().InsertOrReplaceAsync(new CacheElement()
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

            var element = await GetConnection().FindAsync<CacheElement>(key);
            if (element == null)
                throw new KeyNotFoundException(nameof(key));

            var typeName = typeof (T).FullName;
            if (element.TypeName != typeName)
                throw new SimpleCacheTypeMismatchException();

            return await GetConnection().DeleteAsync(element);
        }

        public async Task<int> InvalidateAll<T>()
        {
            var typeName = typeof (T).FullName;
            return await GetConnection().ExecuteAsync($"DELETE FROM CacheElement WHERE TypeName = '{typeName}'");
        }

        public async Task<int> Vacuum()
        {
            var challenge = DateTime.UtcNow.Ticks;
            var deleted = await GetConnection().ExecuteAsync($"DELETE FROM CacheElement WHERE Expiration < {challenge}");

            await GetConnection().ExecuteAsync("VACUUM");

            return deleted;
        }

        public void Dispose()
        {
                _connection = null;
        }

        public async Task<IDictionary<string, T>> Get<T>(IEnumerable<string> keys)
        {
            var results = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                try
                {
                    var result = await Get<T>(key);
                    results.Add(key, result);
                }
		catch (KeyNotFoundException) { }
            }
            return results;
        }

        public async Task<int> Insert<T>(IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            var inserted = 0;
            foreach (var keyValuePair in keyValuePairs)
            {
                inserted += await Insert(keyValuePair.Key, keyValuePair.Value, absoluteExpiration);
            }
            return inserted;
        }

        public async Task<int> Invalidate<T>(IEnumerable<string> keys)
        {
            var invalidated = 0;
            foreach (var key in keys)
            {
                try
                {
                    invalidated += await Invalidate<T>(key);
                }
                catch (KeyNotFoundException) { }
            }
            return invalidated;
        }

        public async Task<IDictionary<string, DateTimeOffset?>> GetCreatedAt(IEnumerable<string> keys)
        {
            var results = new Dictionary<string, DateTimeOffset?>();
            foreach (var key in keys)
            {
                results.Add(key, await GetCreatedAt(key));
            }
            return results;
        }
    }
}
