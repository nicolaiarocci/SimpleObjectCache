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
    internal class SqliteObjectCache
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
                    .TypeOf<ApplicationNameNullException>()
                    .With.Message.EqualTo("Make sure to set ApplicationName on startup"));

            _cache.ApplicationName = AppName;
            Assert.That(_cache.ApplicationName, Is.EqualTo(AppName));
        }

        [Test]
        public async Task Insert()
        {
            const string key = "key";

            Assert.That(async () => await _cache.Insert(null, "value"),
                Throws.Exception
                    .TypeOf<ArgumentNullException>()
                    .With.Property("ParamName")
                    .EqualTo(key));

            var person = new Person() {Name = "john", Age = 19};
            Assert.That(async () => await _cache.Insert(key, person),
                Is.EqualTo(1));

            var restoredPerson = await _cache.Get<Person>(key);
            Assert.That(restoredPerson.Name, Is.EqualTo(person.Name));
            Assert.That(restoredPerson.Age, Is.EqualTo(person.Age));

            // re-inserting with same key overwrites old value.
            var anotherPerson = new Person() {Name = "mike", Age = 30};
            Assert.That(async () => await _cache.Insert(key, anotherPerson),
                Is.EqualTo(1));

            restoredPerson = await _cache.Get<Person>(key);
            Assert.That(restoredPerson.Name, Is.EqualTo(anotherPerson.Name));
            Assert.That(restoredPerson.Age, Is.EqualTo(anotherPerson.Age));
        }

        [Test]
        public async Task Get()
        {

            const string key = "key";
            const string notExistingKey = "unkey";

            Assert.That(async () => await _cache.Get<Person>(key:null),
                Throws.Exception
                    .TypeOf<ArgumentNullException>()
                    .With.Property("ParamName")
                    .EqualTo(key));

            Assert.That(async () => await _cache.Get<Person>(notExistingKey),
                Throws.Exception
                    .TypeOf<KeyNotFoundException>()
                    .With.Message
                    .EqualTo(key));

            var person = new Person() {Name = "john", Age = 19};
            Assert.That(async () => await _cache.Insert(key, person),
                Is.EqualTo(1));

            var restoredPerson = await _cache.Get<Person>(key);
            Assert.That(restoredPerson.Name, Is.EqualTo(person.Name));
            Assert.That(restoredPerson.Age, Is.EqualTo(person.Age));
        }

        [Test]
        public async Task GetCreatedAt()
        {

            const string key = "key";
            const string notExistingKey = "unkey";

            Assert.That(async () => await _cache.GetCreatedAt(key:null),
                Throws.Exception
                    .TypeOf<ArgumentNullException>()
                    .With.Property("ParamName")
                    .EqualTo(key));

            var person = new Person() {Name = "john", Age = 19};
            Assert.That(async () => await _cache.Insert(key, person),
                Is.EqualTo(1));

            var createdAt = await _cache.GetCreatedAt(key);
            Assert.That(createdAt.Value.UtcDateTime, Is.EqualTo(DateTimeOffset.Now.UtcDateTime).Within(1).Seconds);

            Assert.That(async () => await _cache.GetCreatedAt(notExistingKey),
                Is.Null);
        }

        [Test]
        public async Task GetAll()
        {
            var peopleChallenge = new List<Person>()
            {
                new Person {Name = "john", Age = 10},
                new Person {Name = "mike", Age = 20}
            };
            await _cache.Insert("john", peopleChallenge[0]);
            await _cache.Insert("mike", peopleChallenge[1]);

            var addressChallenge = new List<Address>()
            {
                new Address {Street = "Hollywood"},
                new Address {Street = "Junction"},
                new Address {Street = "Grand Station"},
            };
            await _cache.Insert("address1", addressChallenge[0]);
            await _cache.Insert("address2", addressChallenge[1]);
            await _cache.Insert("address3", addressChallenge[2]);


            var expectedCount = 2;
            var returnedPersons = await _cache.GetAll<Person>();
            var persons = returnedPersons as IList<Person> ?? returnedPersons.ToList();

            Assert.That(persons.Count(), Is.EqualTo(expectedCount));
            for (var i = 0; i < expectedCount; i++)
            {
                Assert.That(persons[i].Name, Is.EqualTo(peopleChallenge[i].Name));
                Assert.That(persons[i].Age, Is.EqualTo(peopleChallenge[i].Age));
            }

            expectedCount = 3;
            var returnedAddresses = await _cache.GetAll<Address>();
            var addresses = returnedAddresses as IList<Address> ?? returnedAddresses.ToList();

            Assert.That(addresses.Count(), Is.EqualTo(expectedCount));
            for (var i = 0; i < expectedCount; i++)
            {
                Assert.That(addresses[i].Street, Is.EqualTo(addressChallenge[i].Street));
            }

            var returnedOthers = await _cache.GetAll<Other>();
            Assert.That(returnedOthers, Is.Empty);
        }

        [Test]
        public async Task Invalidate()
        {
            const string key = "key";
            const string notExistingKey = "unkey";

            Assert.That(async () => await _cache.Invalidate<Person>(key:null),
                Throws.Exception
                    .TypeOf<ArgumentNullException>()
                    .With.Property("ParamName")
                    .EqualTo(key));

            Assert.That(async () => await _cache.Invalidate<Person>(notExistingKey),
                Throws.Exception
                    .TypeOf<KeyNotFoundException>()
                    .With.Message
                    .EqualTo(key));

            var person = new Person() {Name = "john", Age = 19};
            Assert.That(async () => await _cache.Insert(key, person),
                Is.EqualTo(1));

            Assert.That(async () => await _cache.Invalidate<Address>(key),
                Throws.TypeOf<TypeMismatchException>());

            var deleted = await _cache.Invalidate<Person>(key);
            Assert.That(deleted, Is.EqualTo(1));

            Assert.That(async () => await _cache.Get<Person>(key),
                Throws.Exception
                    .TypeOf<KeyNotFoundException>()
                    .With.Message
                    .EqualTo(key));
        }

        [Test]
        public async Task InvalidateAll()
        {
            var peopleChallenge = new List<Person>()
            {
                new Person {Name = "john", Age = 10},
                new Person {Name = "mike", Age = 20}
            };
            await _cache.Insert("john", peopleChallenge[0]);
            await _cache.Insert("mike", peopleChallenge[1]);

            var addressChallenge = new List<Address>()
            {
                new Address {Street = "Hollywood"},
                new Address {Street = "Junction"},
                new Address {Street = "Grand Station"},
            };
            await _cache.Insert("address1", addressChallenge[0]);
            await _cache.Insert("address2", addressChallenge[1]);
            await _cache.Insert("address3", addressChallenge[2]);


            var deleted = await _cache.InvalidateAll<Person>();
            Assert.That(deleted, Is.EqualTo(2));

            var persons = await _cache.GetAll<Person>();
            Assert.That(persons, Is.Empty);

            const int expectedCount = 3;
            var returnedAddresses = await _cache.GetAll<Address>();
            var addresses = returnedAddresses as IList<Address> ?? returnedAddresses.ToList();

            Assert.That(addresses.Count(), Is.EqualTo(expectedCount));
            for (var i = 0; i < expectedCount; i++)
            {
                Assert.That(addresses[i].Street, Is.EqualTo(addressChallenge[i].Street));
            }
        }

        [Test]
        public async Task Vacuum()
        {

            const string vacuumMeKey1 = "key1";
            const string vacuumMeKey2 = "key2";
            const string doNotVacuumMeKey = "key3";

            Assert.That(async () => await _cache.Vacuum(), Is.EqualTo(0));

            var person = new Person() {Name = "john", Age = 19};
            await _cache.Insert(vacuumMeKey1, person, DateTime.Now);
            await _cache.Insert(vacuumMeKey2, person, DateTime.Now.AddSeconds(-1));
            await _cache.Insert(doNotVacuumMeKey, person, DateTime.Now.AddSeconds(1));

            Assert.That(async () => await _cache.Vacuum(), Is.EqualTo(2));

            Assert.That(async () => await _cache.Get<Person>(vacuumMeKey1),
                Throws.Exception
		.TypeOf<KeyNotFoundException>());

            Assert.That(async () => await _cache.Get<Person>(vacuumMeKey2),
                Throws.Exception.
		TypeOf<KeyNotFoundException>());

            Assert.That(async () => await _cache.Get<Person>(doNotVacuumMeKey), Is.Not.Null);
        }

	[Test]
        public async Task BulkInsertAndGet()
	{

	    var persons = new Dictionary<string, Person>()
	    {
	        {"key1", new Person {Name = "john", Age = 19}},
	        {"key2", new Person {Name = "mike", Age = 30}},
	    };

            const int expectedCount = 2;
            Assert.That(async () => await _cache.Insert(persons, DateTimeOffset.Now),
                Is.EqualTo(expectedCount));

	    var keys = new List<string> {"key1", "key2", "badkey"};
            var returnedPersons = await _cache.Get<Person>(keys);

	    // bad key has been ignored
            Assert.That(returnedPersons.Count(), Is.EqualTo(expectedCount));

	    for (var i = 1; i <= expectedCount; i++)
	    {
	        var key = $"key{i}";
	        Assert.That(persons[key].Name, Is.EqualTo(returnedPersons[key].Name));
	        Assert.That(persons[key].Age, Is.EqualTo(returnedPersons[key].Age));
	    }
        }

        [Test]
        public async Task BulkInvalidate()
        {
	    var persons = new Dictionary<string, Person>()
	    {
	        {"key1", new Person {Name = "john", Age = 19}},
	        {"key2", new Person {Name = "mike", Age = 30}},
	    };

            const int expectedCount = 2;
            Assert.That(async () => await _cache.Insert(persons),
                Is.EqualTo(expectedCount));

	    var keys = new List<string> {"key1", "key2", "badkey"};

            var invalidated = await _cache.Invalidate<Person>(keys);
            Assert.That(invalidated, Is.EqualTo(2));
        }

        [Test]
        public async Task BulkGetCreatedAt()
        {
	    var persons = new Dictionary<string, Person>()
	    {
	        {"key1", new Person {Name = "john", Age = 19}},
	        {"key2", new Person {Name = "mike", Age = 30}},
	    };

            const int expectedCount = 2;
            Assert.That(async () => await _cache.Insert(persons),
                Is.EqualTo(expectedCount));

	    var keys = new List<string> {"key1", "key2", "key3"};

            var results = await _cache.GetCreatedAt(keys);
            Assert.That(results.Count, Is.EqualTo(3));

	    for (var i = 1; i <= 3; i++)
	    {
	        var key = $"key{i}";
	        var dateTimeOffset = results[key];
	        if (dateTimeOffset != null)
	            Assert.That(dateTimeOffset.Value.UtcDateTime,
	                Is.EqualTo(DateTimeOffset.Now.UtcDateTime).Within(1).Seconds);
	        else
	            Assert.That(key, Is.EqualTo("key3"));
	    }
        }


	[Test]
        public async Task BulkInsertAbsoluteExpiration()
        {

            Assert.That(async () => await _cache.Vacuum(), Is.EqualTo(0));

	    var persons = new Dictionary<string, Person>()
	    {
	        {"key1", new Person {Name = "john", Age = 19}},
	        {"key2", new Person {Name = "mike", Age = 30}},
	    };
            await _cache.Insert(persons, DateTime.Now);

	    persons = new Dictionary<string, Person>()
	    {
	        {"key3", new Person {Name = "john", Age = 19}},
	        {"key4", new Person {Name = "mike", Age = 30}},
	    };
            await _cache.Insert(persons, DateTime.Now.AddMinutes(-1));

	    persons = new Dictionary<string, Person>()
	    {
	        {"key5", new Person {Name = "john", Age = 19}},
	        {"key6", new Person {Name = "mike", Age = 30}},
	    };
            await _cache.Insert(persons, DateTime.Now.AddMinutes(1));

            Assert.That(async () => await _cache.Vacuum(), Is.EqualTo(4));

	    var keys = new List<string> {"key1", "key2", "key3", "key4"};
	    var returnedPersons = await _cache.Get<Person>(keys);
	    Assert.That(returnedPersons, Is.Empty);

	    keys = new List<string> {"key5", "key6"};
	    var expectedCount = 2;
	    returnedPersons = await _cache.Get<Person>(keys);
	    Assert.That(returnedPersons.Count, Is.EqualTo(2));
	    for (var i = 0; i < expectedCount; i++)
	    {
                Assert.That(returnedPersons[keys[i]].Name, Is.EqualTo(persons[keys[i]].Name));
                Assert.That(returnedPersons[keys[i]].Age, Is.EqualTo(persons[keys[i]].Age));
	    }

        }
        private class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private class Address
        {
            public string Street { get; set; }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class Other
        {
        }
    }
}
