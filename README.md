# SimpleCache
Simple asynchronous, permanent and cross-platform key-value object cache.

## Usage
```C#
    /* First you need to set ApplicatioName.
       This is also the folder where your cache will reside.
       Depending on the host OS the location of this folder
       might be different. On windows it would be something
       like C:\ProgramData\<ApplicationName>\SimpleCache
    */ 
    var cache = new SimpleCache.SqliteObjectCache { ApplicationName = "MyApplication"};

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

## Supported systems
iOS, .NET4, .NET45. Android is planned (or you can add it yourself, pull requests are welcome!).

## Current status
Work in progress. Not on NuGet yet. Contributors welcome.

## Licence
SimpleCache a [Nicola Iarocci][ni] open source project, and it is [BSD][bsd] licensed.

[bsd]: http://github.com/nicolaiarocci/SimpleCache/blob/master/LICENSE
[ni]: http://nicolaiarocci.com