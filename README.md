DapperLite - a very simple object mapper for .NET, .NET Compact Framework and Mono
==================================================================================

Features
--------
DapperLite is a [single file](https://github.com/ryankirkman/DapperLite/blob/master/DapperLite.NETCF35/SqlMapper.cs) you can drop in to your .NET Compact Framework or .NET project that will extend your IDbConnection interface.

This project is a subset and simplification of dapper-dot-net: https://github.com/SamSaffron/dapper-dot-net

Currently provides 2 helpers:

Execute a query and map the results to a strongly typed List
------------------------------------------------------------

```csharp
public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, object param)

public static IEnumerable<T> Query<T>(this IDbConnection conn, string sql)
```

Example usage:

```csharp
public class Dog
{
    public int? Age { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; }
    public float? Weight { get; set; }
    public string Size { get; set; }
}            
            
var guid = Guid.NewGuid();
var dog = connection.Query<Dog>("select Age = @Age, Id = @Id", new { Age = (int?)null, Id = guid });
            
dog.Count()
    .IsEqualTo(1);

dog.First().Age
    .IsNull();

dog.First().Id
    .IsEqualTo(guid);
```

Execute a Command that returns no results
-----------------------------------------

```csharp
public static int Execute(this IDbConnection cnn, string sql, object param, IDbTransaction transaction)

public static int Execute(this IDbConnection cnn, string sql)
```

Example usage:

```csharp
connection.Execute(@"
  set nocount on 
  create table #t(i int) 
  set nocount off 
  insert #t 
  select @a a union all select @b 
  set nocount on 
  drop table #t", new {a=1, b=2 })
   .IsEqualTo(2);
```


Using the Micro-ORM Database.cs class
-------------------------------------

To use this Micro-ORM, in addition to [SqlMapper.cs](https://github.com/ryankirkman/DapperLite/blob/master/DapperLite.NETCF35/SqlMapper.cs) you will also need to include [SqlMapperInsertUpdate.cs](https://github.com/ryankirkman/DapperLite/blob/master/DapperLite.NETCF35/SqlMapperInsertUpdate.cs), [Database.cs](https://github.com/ryankirkman/DapperLite/blob/master/DapperLite.NETCF35/Database.cs) and [SqlCeDatabase.cs](https://github.com/ryankirkman/DapperLite/blob/master/DapperLite.NETCF35/SqlCeDatabase.cs). These are all extremely simple files and worth reading if you'd like to modify their functionality.

```csharp
// Basic constructor.
protected Database(IDbConnection connection)

// Provides advanced configuration for the behaviour of the class when Exceptions are encountered.
protected Database(IDbConnection connection, DapperLiteException exceptionHandler, bool throwExceptions)

public T Get<T>(TId id)

public T Get<T>(string columnName, object data)

public IEnumerable<T> All<T>()

public IEnumerable<T> All<T>(string columnName, object data)

public virtual void Insert(object obj)

public virtual void Update(object obj)

// Also includes wrapper methods for Query<T>() and Execute() to save typing.
```

`Database.cs` is an abstract class designed to be extended with SQL version specific implementations. An `SqlCeDatabase` implementation is provided.

Example usage:

```csharp
SqlCeConnection conn = new SqlCeConnection("Data Source=MyDatabase.sdf");

// The type we pass in (Guid) is the type of the Id column that is assumed to be present in every table.
SqlCeDatabase<Guid> db = new SqlCeDatabase<Guid>(conn);
// Calling Init() automatically generates a table name map, used to map type names to table names.
// e.g. for the type "Dog", it will first search for a table name == "Dog", then (pluralized) "Dogs"
db.Init();

// Get all Dogs.
IEnumerable<Dog> allDogs = db.All<Dog>();

// Get all Dogs where Size == "big"
IEnumerable<Dog> bigDogs = db.All<Dog>("Size", "big");

// Get a dog by Id
Dog myDog = db.Get<Dog>(someGuid);

// Insert a new Dog
Dog newDog = new Dog { Age = 3, Id = Guid.New(), Name = "Ralph", Weight = 12.3, Size = "Small" };
db.Insert(newDog);

// Get the Dog with Name == "Fido"
Dog yourDog = db.Get<Dog>("Name", "Fido");

// Update a Dog
yourDog.Size = "Small";
db.Update(yourDog);
```
