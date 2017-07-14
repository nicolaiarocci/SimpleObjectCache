# SimpleObjectCache
Simple asynchronous, permanent key-value object cache for .NET systems.

## Supported platforms
SimpleObjectCache is released as a NetStandard 1.1 package, which makes 
it compatible with a [wide range][ns] of platforms.


## Usage
```C#
    var cache = new SqliteObjectCache { DatabasePath = "cache.db"};

    // Create an object.
    var john = new Person() {Name = "john", Age = 19};

    // Insert it into the cache.
    await cache.Insert("key", john);

    // Or you can also add an expiration date.
    // This will overwrite the previous item with same key.
    await cache.Insert("key", john, DateTimeOffset.Now.AddDays(30));

    // Retrieve the object from the cache.
    var person = await cache.Get<Person>("key");
    Assert.That(person.Name, Is.EqualTo(john.Name));

    // Remove the object from cache.
    await cache.Invalidate<Person>("key");

    // Bulk inserts are also possible.
    var persons = new Dictionary<string, Person>()
    {
	  {"tom", new Person {Name = "tom", Age = 19}},
	  {"mike", new Person {Name = "mike", Age = 30}},
    };

    // Tom and Mike expiration date is set to... yesterday(!).
    var inserted = await cache.Insert(persons, DateTime.Now.AddDays(-1));
    Assert.That(inserted, Is.EqualTo(2));

    // Let's add John again, but with a longer expiration.
    await cache.Insert("john", john, DateTimeOffset.Now.AddDays(30));

    // The Vacuum method removes expired objects from the cache, so
    // Tom and Mike are going to be purged by this command.
    await cache.Vacuum();

    // Now let's get all the available Person objects from the cache.
    var returnedPersons = await cache.GetAll<Person>();

    // Since Tom and Mike are gone, we only got one object
    // back, and that's our very own John.
    Assert.That(returnedPersons.Count(), Is.EqualTo(1));
    Assert.That(returnedPersons[0].Name, Is.EqualTo(john.Name));
```
Note that all methods are Async, even if they don't have the suffix.

## Installation
SimpleObjectCache is on [NuGet][nu]. Run the following command on the Package Manager Console:

```
    PM> Install-Package SimpleObjectCache
```

Or install via the NuGet Package Manager in Visual Studio.

## License
SimpleObjectCache a [Nicola Iarocci][ni] open source project, and it is [BSD][bsd] licensed.

[bsd]: http://github.com/nicolaiarocci/SimpleObjectCache/blob/master/LICENSE
[ni]: http://nicolaiarocci.com
[nu]: https://www.nuget.org/packages/SimpleObjectCache/
[ns]: https://github.com/dotnet/standard/blob/master/docs/versions/netstandard1.1.md