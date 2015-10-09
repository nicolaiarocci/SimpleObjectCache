using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SQLite;

// ReSharper disable once CheckNamespace
namespace Amica.vNext.SimpleCache.Tests
{
    [TestFixture]
    class SqliteObjectCache
    {
        private string _expectedDatabasePath;
        private const string AppName = "test";

        private readonly SimpleCache.SqliteObjectCache _cache = new SimpleCache.SqliteObjectCache();
        private SQLiteConnection _connection;

        [SetUp]
        public void Setup()
        {
            _cache.ApplicationName = AppName;

            _expectedDatabasePath = Path.Combine(
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppName), 
		"SimpleCache");

            Directory.CreateDirectory(_expectedDatabasePath);
            _connection = new SQLiteConnection(Path.Combine(_expectedDatabasePath, "cache.db3"));
	    _connection.DropTable<CacheElement>();

        }

	[TearDown]
        public void TearDown()
        {
            _cache.Dispose();
	    _connection.Close();
        }


        [Test]
        public void ApplicationName()
        {
            _cache.ApplicationName = null;

	    Assert.That(() => _cache.ApplicationName,
            Throws.Exception
                .TypeOf<Exception>()
                .With.Message.EqualTo("Make sure to set ApplicationName on startup"));

            _cache.ApplicationName = AppName;
            Assert.That(_cache.ApplicationName, Is.EqualTo(AppName));
        }

        [Test]
        public async Task InsertAsync()
        {


            const string key = "key";

            Assert.That(async () => await _cache.InsertAsync(null, "value"),
                Throws.Exception
                    .TypeOf<ArgumentNullException>()
                    .With.Property("ParamName")
                    .EqualTo(key));

            var person = new Person() {Name = "john", Age = 19};
            Assert.That(async () => await _cache.InsertAsync(key, person), 
		Is.EqualTo(1));

            var restoredPerson = await _cache.GetAsync<Person>(key);
            Assert.That(restoredPerson.Name, Is.EqualTo(person.Name));
            Assert.That(restoredPerson.Age, Is.EqualTo(person.Age));

            // re-inserting with same key overwrites old value.
            var anotherPerson = new Person() { Name = "mike", Age = 30 };
            Assert.That(async () => await _cache.InsertAsync(key, anotherPerson),
                Is.EqualTo(1));

            restoredPerson = await _cache.GetAsync<Person>(key);
            Assert.That(restoredPerson.Name, Is.EqualTo(anotherPerson.Name));
            Assert.That(restoredPerson.Age, Is.EqualTo(anotherPerson.Age));
        }

	[Test]
        public async Task GetAsync()
        {

            const string key = "key";
            const string notExistingKey = "unkey";

            Assert.That(async () => await _cache.GetAsync<Person>(null),
                Throws.Exception
                    .TypeOf<ArgumentNullException>()
                    .With.Property("ParamName")
                    .EqualTo(key));

            Assert.That(async () => await _cache.GetAsync<Person>(notExistingKey),
                Throws.Exception
                    .TypeOf<KeyNotFoundException>()
                    .With.Message
                    .EqualTo(key));

            var person = new Person() {Name = "john", Age = 19};
            Assert.That(async () => await _cache.InsertAsync(key, person), 
		Is.EqualTo(1));

            var restoredPerson = await _cache.GetAsync<Person>(key);
            Assert.That(restoredPerson.Name, Is.EqualTo(person.Name));
            Assert.That(restoredPerson.Age, Is.EqualTo(person.Age));
        }

        [Test]
        public async Task GetAllAsync()
        {
            var peopleChallenge = new List<Person>()
            {
                new Person {Name = "john", Age = 10},
                new Person {Name = "mike", Age = 20}
            };
            await _cache.InsertAsync("john", peopleChallenge[0]); 
            await _cache.InsertAsync("mike", peopleChallenge[1]); 

            var addressChallenge = new List<Address>()
            {
                new Address {Street = "Hollywood"},
                new Address {Street = "Junction"},
                new Address {Street = "Grand Station"},
            };
            await _cache.InsertAsync("address1", addressChallenge[0]); 
            await _cache.InsertAsync("address2", addressChallenge[1]); 
            await _cache.InsertAsync("address3", addressChallenge[2]); 
	    

            var expectedCount = 2;
            var returnedPersons = await _cache.GetAllAsync<Person>();
            var persons = returnedPersons as IList<Person> ?? returnedPersons.ToList();

            Assert.That(persons.Count(), Is.EqualTo(expectedCount));
	    for (var i = 0; i < expectedCount; i++)
	    {
	        Assert.That(persons[i].Name, Is.EqualTo(peopleChallenge[i].Name));
	        Assert.That(persons[i].Age, Is.EqualTo(peopleChallenge[i].Age));
	    }

            expectedCount = 3;
            var returnedAddresses = await _cache.GetAllAsync<Address>();
            var addresses = returnedAddresses as IList<Address> ?? returnedAddresses.ToList();

            Assert.That(addresses.Count(), Is.EqualTo(expectedCount));
	    for (var i = 0; i < expectedCount; i++)
	    {
	        Assert.That(addresses[i].Street, Is.EqualTo(addressChallenge[i].Street));
	    }

            var returnedOthers = await _cache.GetAllAsync<Other>();
            Assert.That(returnedOthers.Count(), Is.EqualTo(0));
        }

	[Test]
        public async Task InvalidateAsync()
        {
            const string key = "key";
            const string notExistingKey = "unkey";

            Assert.That(async () => await _cache.InvalidateAsync<Person>(null), 
                Throws.Exception
                    .TypeOf<ArgumentNullException>()
                    .With.Property("ParamName")
                    .EqualTo(key));

            Assert.That(async () => await _cache.InvalidateAsync<Person>(notExistingKey), 
                Throws.Exception
                    .TypeOf<KeyNotFoundException>()
                    .With.Message
                    .EqualTo(key));

            var person = new Person() {Name = "john", Age = 19};
            Assert.That(async () => await _cache.InsertAsync(key, person), 
		Is.EqualTo(1));

	    var typeName = typeof (Address).FullName;
            Assert.That(async () => await _cache.InvalidateAsync<Address>(key), 
                Throws.Exception
                    .With.Message
                    .EqualTo("Cached item is not of type {typeName}"));

	    var deleted = await _cache.InvalidateAsync<Person>(key);
	    Assert.That(deleted, Is.EqualTo(1));

	    Assert.That(async () => await _cache.GetAsync<Person>(key),
                Throws.Exception
                    .TypeOf<KeyNotFoundException>()
                    .With.Message
                    .EqualTo(key));
        }
    }

    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    class Address
    {
        public string Street { get; set; }
    }

    class Other
    {
        public string Nil { get; set; }
    }
}
