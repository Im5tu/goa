using Goa.Clients.Dynamo;
using System.Collections.ObjectModel;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "COLLECTION#<Id>", SK = "DATA#<Type>")]
[GlobalSecondaryIndex(Name = "TypeIndex", PK = "TYPE#<Type>", SK = "COLLECTION#<Id>")]
public record CollectionTestModel(
    string Id,
    CollectionType Type,
    DateTime CreatedAt,

    // IEnumerable<T> - Basic enumerable interface
    IEnumerable<string> StringEnumerable,
    IEnumerable<int> IntEnumerable,
    IEnumerable<double> DoubleEnumerable,
    IEnumerable<DateTime> DateTimeEnumerable,
    IEnumerable<Priority> EnumEnumerable,

    // List<T> - Concrete list implementation
    List<string> StringList,
    List<int> IntList,
    List<double> DoubleList,
    List<DateTime> DateTimeList,
    List<Priority> EnumList,

    // IList<T> - List interface
    IList<string> StringIList,
    IList<int> IntIList,
    IList<double> DoubleIList,
    IList<DateTime> DateTimeIList,
    IList<Priority> EnumIList,

    // ICollection<T> - Collection interface
    ICollection<string> StringCollection,
    ICollection<int> IntCollection,
    ICollection<double> DoubleCollection,
    ICollection<DateTime> DateTimeCollection,
    ICollection<Priority> EnumCollection,

    // Collection<T> - Concrete collection
    Collection<string> StringConcreteCollection,
    Collection<int> IntConcreteCollection,
    Collection<double> DoubleConcreteCollection,
    Collection<DateTime> DateTimeConcreteCollection,
    Collection<Priority> EnumConcreteCollection,

    // ISet<T> - Set interface
    ISet<string> StringSet,
    ISet<int> IntSet,
    ISet<double> DoubleSet,
    ISet<DateTime> DateTimeSet,
    ISet<Priority> EnumSet,

    // HashSet<T> - Concrete set implementation
    HashSet<string> StringHashSet,
    HashSet<int> IntHashSet,
    HashSet<double> DoubleHashSet,
    HashSet<DateTime> DateTimeHashSet,
    HashSet<Priority> EnumHashSet,

    // IReadOnlyCollection<T> - Read-only collection interface
    IReadOnlyCollection<string> StringReadOnlyCollection,
    IReadOnlyCollection<int> IntReadOnlyCollection,
    IReadOnlyCollection<double> DoubleReadOnlyCollection,
    IReadOnlyCollection<DateTime> DateTimeReadOnlyCollection,
    IReadOnlyCollection<Priority> EnumReadOnlyCollection,

    // IReadOnlyList<T> - Read-only list interface
    IReadOnlyList<string> StringReadOnlyList,
    IReadOnlyList<int> IntReadOnlyList,
    IReadOnlyList<double> DoubleReadOnlyList,
    IReadOnlyList<DateTime> DateTimeReadOnlyList,
    IReadOnlyList<Priority> EnumReadOnlyList,

    // IReadOnlySet<T> - Read-only set interface (.NET 5+)
    IReadOnlySet<string> StringReadOnlySet,
    IReadOnlySet<int> IntReadOnlySet,
    IReadOnlySet<double> DoubleReadOnlySet,
    IReadOnlySet<DateTime> DateTimeReadOnlySet,
    IReadOnlySet<Priority> EnumReadOnlySet,

    // T[] - Arrays
    string[] StringArray,
    int[] IntArray,
    double[] DoubleArray,
    DateTime[] DateTimeArray,
    Priority[] EnumArray,

    // IDictionary<TKey, TValue> - Dictionary interface
    IDictionary<string, string> StringStringDictionary,
    IDictionary<string, int> StringIntDictionary,
    IDictionary<string, double> StringDoubleDictionary,
    IDictionary<string, DateTime> StringDateTimeDictionary,
    IDictionary<string, Priority> StringEnumDictionary,

    // Dictionary<TKey, TValue> - Concrete dictionary
    Dictionary<string, string> StringStringConcreteDictionary,
    Dictionary<string, int> StringIntConcreteDictionary,
    Dictionary<string, double> StringDoubleConcreteDictionary,
    Dictionary<string, DateTime> StringDateTimeConcreteDictionary,
    Dictionary<string, Priority> StringEnumConcreteDictionary,

    // IReadOnlyDictionary<TKey, TValue> - Read-only dictionary interface
    IReadOnlyDictionary<string, string> StringStringReadOnlyDictionary,
    IReadOnlyDictionary<string, int> StringIntReadOnlyDictionary,
    IReadOnlyDictionary<string, double> StringDoubleReadOnlyDictionary,
    IReadOnlyDictionary<string, DateTime> StringDateTimeReadOnlyDictionary,
    IReadOnlyDictionary<string, Priority> StringEnumReadOnlyDictionary,

    // Nullable collections
    IEnumerable<string>? NullableStringEnumerable,
    List<int>? NullableIntList,
    ISet<double>? NullableDoubleSet,
    string[]? NullableStringArray,
    IDictionary<string, string>? NullableStringDictionary,

    // Complex nested types
    IEnumerable<IEnumerable<string>> NestedStringEnumerable,
    List<List<int>> NestedIntList,
    Dictionary<string, List<string>> StringToStringListDictionary,
    Dictionary<string, Dictionary<string, string>> NestedStringDictionary,

    // Edge cases
    IEnumerable<Guid> GuidEnumerable,
    List<TimeSpan> TimeSpanList,
    ISet<DateTimeOffset> DateTimeOffsetSet,
    Dictionary<Guid, string> GuidStringDictionary,
    Dictionary<Guid, int>? GuidStringDictionary2
);
