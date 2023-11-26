# Object Trackers (.NET)

Author: Ryan Kueter  
Updated: November, 2023

## About

**Object Trackers** is a free .NET library, available from the [NuGet Package Manager](https://www.nuget.org/packages/ObjectTrackers), that provides a simple way to track changes made to an object, like a class or list of classes. It allows you to capture those changes as ***before*** and ***after*** json serialized dictionary arrays that contain the property names and their values for quickly and easily storing and retrieving your audit data.  

### Targets:
- .NET 5, .NET 6, .NET 7, .NET 8

## Why Client-Side Auditing is Better Engineering

**Object Tackers** was originally written to provide functionality for client-side auditing scenarios. This works best with clients that can persist data (e.g., Blazor WASM, MAUI, WPF). By moving the user auditing logic to the client, you can dramatically improve the readability and maintainability of your code while improving the response-time and performance of your backend services and DBMSs. Backend data structures are typically very different from those consumed by the client. Consequently, you capture a lot of useless and irrelevant data, and substantially add to processor consumption, especially if you are using interceptors or database triggers. If you are creating your own custom functions in the service or the DBMS, then you need to locate and track all of those functions, which can be difficult if they are in stored procedures. By moving all the user auditing to the client, you eliminate all of that excess confusion, excess data, and processor consumption, which improves the response time of the service or DBMS, and only captures the data your users care about in a format they understand.

In the following feature demonstration, notice how the HasChanges() method must be called to get the changes.

```csharp
using ObjectTrackers;

// Create a new person
var person = new Person()
{
    Id = 1,
    FirstName = "Ryan",
    LastName = "Kueter",
    CreatedDate = DateTime.Now,
    Address = new Address() { Street = "1st Street", City = "FunTown" }
};

// Track the person
var trackedPerson = new Track<Person>(person);

// Make some changes to the person
person.LastName = "Silly";
person.CreatedDate = DateTime.Now.AddDays(-1);

// YOU MUST RUN HasChanges() to see if any changes were made
// to the state of your objects. 
if (trackedPerson.HasChanges())
{
    string beforeJson = trackedPerson.Before;
    string afterJson = trackedPerson.After;

    Console.WriteLine("Before Values:");
    Console.WriteLine(beforeJson);
    Console.WriteLine();
    Console.WriteLine("After Values:");
    Console.WriteLine(afterJson);
}

// You also have an async option
if (await trackedPerson.HasChangesAsync())
{
    string beforeJson = trackedPerson.Before;
    string afterJson = trackedPerson.After;

    Console.WriteLine("Before Values:");
    Console.WriteLine(beforeJson);
    Console.WriteLine();
    Console.WriteLine("After Values:");
    Console.WriteLine(afterJson);
}
```
###
#### Output:

Since we only changed the last name and created date, those are the only two properties listed.

```console
Before Values:
{"LastName":"Kueter","CreatedDate":"3/1/2022 1:19:53 PM"}

After Values:
{"LastName":"Silly","CreatedDate":"2/28/2022 1:19:53 PM"}
```
###
#### Deserializing the Values:
```csharp
var b = JsonSerializer.Deserialize<Dictionary<string, string>>(trackedPerson.After);
foreach (var kv in b)
{
    Console.WriteLine($"{kv.Key}: {kv.Value}");
    if (kv.Key == "Address")
    {
        var address = JsonSerializer.Deserialize<Address>(kv.Value);
        Console.WriteLine(address.Street);
    }
}
```
   
When you have properties that are custom datatypes or arrays, the value itself is stored as a json object. For example, in the previous example, we could also change the Address street from "1st Street" to "2nd Street" it would store the values like the following.

```console
Before Values:
{"LastName":"Kueter","Address":"{\"Street\":\"1st Street\",\"City\":\"FunTown\"}","CreatedDate":"3/1/2022 1:21:22 PM"}

After Values:
{"LastName":"Silly","Address":"{\"Street\":\"2nd Street\",\"City\":\"FunTown\"}","CreatedDate":"2/28/2022 1:21:22 PM"}
```
While this may not be ideal, you have the option of tracking nested classes separately, or use inclusion filters to filter out the unnecessary properties.
```csharp
// An example of using inclusion filters
var trackedPerson = new Track<Person>(person, 
    "FirstName", 
    "LastName");

// An example of tracking a nested class
var trackedAddress = new Track<Address>(person.Address);
```
###
## Tracking Lists

Object Tracker also allows you to track a list of objects, which can be powerful if your users are editing a list of items.

```csharp
// Create some persons
var person1 = new Person() { Id = 1, FirstName = "Ryan", LastName = "Kueter" };
var person2 = new Person() { Id = 2, FirstName = "John", LastName = "Doe" };
var person3 = new Person() { Id = 3, FirstName = "Jane", LastName = "Doe" };

// Add some persons to the list
var personsList = new List<Person>() { person1, person2 };

// Track that list of persons
var personsListTracked = new TrackList<Person>(personsList);

// **************************
// Adding items to the list:
// **************************

// If you add an item to the original list, 
// the tracker will detect this change, 
// but will not track the object
personsList.Add(person3);

// If you add an item to the tracked list, 
// it will add the item to the original list,
// detect the change, and add the object to the tracker
personsListTracked.Add(person3);

// **************************
// Removing items from the list:
// **************************

// If you remove an item from the original list, 
// the tracker will detect this change, 
// but will not remove the object from tracking.
personsList.Remove(person1);

// If you remove an item from the tracked list, 
// it will remove the item from the original list,
// detect the change (and allow you to see this change), 
// and will remove the object from the tracker
personsListTracked.Remove(person1);

// Change some values
person1.LastName = "Silly";
person3.LastName = "Silly";

// Check for any changes.
if (personsListTracked.HasChanges())
{
    // Check for any changes in the tracked objects
    foreach (var c in personsListTracked.TrackedChanges)
    {
        // Get the original item Id
        Console.WriteLine($"Id {c.Item.Id}");

        // Get the before and after values
        Console.WriteLine($"Before: {c.Before}");
        Console.WriteLine($"After: {c.After}");
    }

    // Check for items added or removed.
    Console.WriteLine();
    Console.WriteLine("Actual rows added");
    foreach (var c in personsListTracked.ItemsAdded())
    {
        Console.WriteLine($"{c.Id}, {c.FirstName}, {c.LastName}");
    }
    Console.WriteLine();
    Console.WriteLine("Json serialized rows added");
    foreach (var c in personsListTracked.ItemsAddedJson())
    {
        Console.WriteLine($"{c}");
    }

    Console.WriteLine();
    Console.WriteLine("Actual rows removed");
    foreach (var c in personsListTracked.ItemsRemoved())
    {
        Console.WriteLine($"{c.Id}, {c.FirstName}, {c.LastName}");
    }

    Console.WriteLine();
    Console.WriteLine("Json serialized rows removed");
    foreach (var c in personsListTracked.ItemsRemovedJson())
    {
        Console.WriteLine($"{c}");
    }
}
```  
###
#### Output:

In the first part of this example, we retrieve only the changes made to the tracked objects. Since we removed the first user, it's no longer being tracked and is not listed among the tracked changes. To track rows that were added or removed, Object Tracker provides the ItemsAdded() and ItemsRemoved() methods.

```console
Id 3
Before: {"LastName":"Doe"}
After: {"LastName":"Silly"}

Actual rows added
3, Jane, Silly

Json serialized rows added
{"Id":3,"FirstName":"Jane","LastName":"Silly","Age":0,"Address":null,"Addresses":null,"CreatedDate":"0001-01-01T00:00:00","ModifiedDate":"0001-01-01T00:00:00"}

Actual rows removed
1, Ryan, Silly

Json serialized rows removed
{"Id":1,"FirstName":"Ryan","LastName":"Silly","Age":0,"Address":null,"Addresses":null,"CreatedDate":"0001-01-01T00:00:00","ModifiedDate":"0001-01-01T00:00:00"}
```
###
## Inclusion Filters

The Track and TrackList classes allow you to supply inclusion filters, which allows you to specify what properties to track. This can help you to provide more meaningful information to your users and, in some cases, improve performance and prevent exceptions.

```csharp
// Create a new person
var person = new Person()
{
    Id = 1,
    FirstName = "Ryan",
    LastName = "Kueter",
    CreatedDate = DateTime.Now,
    Address = new Address() { Street = "1st Street", City = "FunTown" }
};

// Track the person
var trackedPerson = new Track<Person>(person, 
    "FirstName", 
    "LastName");

// Make some changes to the person
person.LastName = "Silly";
person.CreatedDate = DateTime.Now.AddDays(-1);
person.Address.Street = "2nd Street";
```  
###
#### Output:
```console
Before Values:
{"LastName":"Kueter"}

After Values:
{"LastName":"Silly"}
```
###
## Example Usage

How the Object Tracker could be used in a view model.

```csharp
/// <summary>
/// Usage example:
/// </summary>
public class ExampleViewModel
{
    /// <summary>
    /// The tracker
    /// </summary>
    private Track<Person> _personTracked;
    public Person SelectedPerson { get; set; }

    /// <summary>
    /// The GetPerson() method that fetches the data
    /// </summary>
    public void GetPerson()
    {
        // Get the data and track it
        SelectedPerson = new Person() { Id = 1, FirstName = "Ryan", LastName = "Kueter" };
        _personTracked = new Track<Person>(SelectedPerson);
    }

    /// <summary>
    /// Fake save button
    /// </summary>
    public void SaveButton_Click()
    {
        // YOU MUST CALL HasChanges() to get the changes
        if (!_personTracked.HasChanges()) { return; }

        var audit = new ExampleAudit()
        {
            Id = _personTracked.Value.Id,
            Module = "Console",
            Before = _personTracked.Before,
            After = _personTracked.After
        };

        // Do something with the data...
    }

    /// <summary>
    /// Low effort example audit class
    /// </summary>
    public class ExampleAudit
    { 
        public int Id { get; set; }
        public string Module { get; set; }
        public string Before { get; set; }
        public string After { get; set; }
    }
}
```  
###
## Contributions

Object Tracker is being developed for free by me, Ryan Kueter, in my spare time. So, if you use this library and see a need for improvement, please send your ideas.