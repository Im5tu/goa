using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "COMPLEX#<Id>", SK = "DATA#<Category>", PKName = "CustomPK", SKName = "CustomSK")]
[GlobalSecondaryIndex(Name = "TypeIndex", PK = "TYPE#<ModelType>", SK = "COMPLEX#<Id>")]
[GlobalSecondaryIndex(Name = "StatusIndex", PK = "STATUS#<Status>", SK = "PRIORITY#<Priority>")]
[GlobalSecondaryIndex(Name = "DateIndex", PK = "DATE#<CreatedDate>", SK = "COMPLEX#<Id>", PKName = "DatePK", SKName = "DateSK")]
public record ComplexTestModel(
    // Primary identifiers
    string Id,
    string Category,
    ComplexModelType ModelType,
    ComplexStatus Status,

    // Primitive types
    bool BoolValue,
    byte ByteValue,
    sbyte SByteValue,
    char CharValue,
    short ShortValue,
    ushort UShortValue,
    int IntValue,
    uint UIntValue,
    long LongValue,
    ulong ULongValue,
    float FloatValue,
    double DoubleValue,
    decimal DecimalValue,

    // Nullable primitive types
    bool? NullableBoolValue,
    byte? NullableByteValue,
    sbyte? NullableSByteValue,
    char? NullableCharValue,
    short? NullableShortValue,
    ushort? NullableUShortValue,
    int? NullableIntValue,
    uint? NullableUIntValue,
    long? NullableLongValue,
    ulong? NullableULongValue,
    float? NullableFloatValue,
    double? NullableDoubleValue,
    decimal? NullableDecimalValue,

    // Date/Time types
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    DateTimeOffset CreatedOffset,
    DateTimeOffset? UpdatedOffset,
    TimeSpan Duration,
    TimeSpan? OptionalDuration,

    // String types
    string Name,
    string? Description,

    // Enum types
    Priority Priority,
    Priority? OptionalPriority,

    // Collection types
    IEnumerable<string> Tags,
    IEnumerable<int> Numbers,
    IEnumerable<Priority> Priorities,
    IEnumerable<double> Measurements,
    IEnumerable<DateTime> ImportantDates,

    // Guid
    Guid UniqueId,
    Guid? OptionalId
);
