DapperLite - a very simple object mapper for .Net & .NetCF
==========================================================

A Dapper compatible library that works on the .NET Compact Framework.

For more thorough documentation please see: https://github.com/SamSaffron/dapper-dot-net

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
db.Init();

IEnumerable<Dog> allDogs = db.All<Dog>();

IEnumerable<Dog> bigDogs = db.All<Dog>("Size", "big");

Dog myDog = db.Get<Dog>(someGuid);

Dog newDog = new Dog { Age = 3, Id = Guid.New(), Name = "Ralph", Weight = 12.3, Size = "Small" };
db.Insert(newDog);

Dog yourDog = db.Get<Dog>("Name", "Fido");
yourDog.Size = "Small";
db.Update(yourDog);
```