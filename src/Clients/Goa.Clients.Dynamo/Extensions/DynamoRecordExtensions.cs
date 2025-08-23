using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Extensions;

/// <summary>
/// Extension methods for DynamoRecord to provide type-safe value extraction.
/// </summary>
public static class DynamoRecordExtensions
{
    /// <summary>
    /// Attempts to get a nullable string value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted string value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableString(this DynamoRecord record, string columnName, out string? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (attributeValue.S == null)
            return false;

        value = attributeValue.S;
        return true;
    }

    /// <summary>
    /// Attempts to get a required (non-nullable) string value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted string value.</param>
    /// <returns>True if successful, false if the field is missing or null.</returns>
    public static bool TryGetString(this DynamoRecord record, string columnName, out string value)
    {
        value = string.Empty;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true || string.IsNullOrEmpty(attributeValue.S))
            return false;

        value = attributeValue.S;
        return true;
    }

    /// <summary>
    /// Attempts to get a boolean value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted boolean value, or default(bool) if not found.</param>
    /// <returns>True if the column exists and has a valid boolean value, false otherwise.</returns>
    public static bool TryGetBool(this DynamoRecord record, string columnName, out bool value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        if (attributeValue.BOOL == null)
            return false;

        value = attributeValue.BOOL.Value;
        return true;
    }

    /// <summary>
    /// Attempts to get a nullable boolean value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted boolean value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableBool(this DynamoRecord record, string columnName, out bool? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        value = attributeValue.BOOL;
        return true;
    }

    /// <summary>
    /// Attempts to get a short value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted short value, or default(short) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetShort(this DynamoRecord record, string columnName, out short value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && short.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable short value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted short value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableShort(this DynamoRecord record, string columnName, out short? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (short.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get an int value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted int value, or default(int) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetInt(this DynamoRecord record, string columnName, out int value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && int.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable int value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted int value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableInt(this DynamoRecord record, string columnName, out int? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (int.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a long value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted long value, or default(long) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetLong(this DynamoRecord record, string columnName, out long value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && long.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable long value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted long value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableLong(this DynamoRecord record, string columnName, out long? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (long.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a double value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted double value, or default(double) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetDouble(this DynamoRecord record, string columnName, out double value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && double.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable double value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted double value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableDouble(this DynamoRecord record, string columnName, out double? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (double.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a float value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted float value, or default(float) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetFloat(this DynamoRecord record, string columnName, out float value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && float.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable float value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted float value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableFloat(this DynamoRecord record, string columnName, out float? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (float.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a decimal value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted decimal value, or default(decimal) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetDecimal(this DynamoRecord record, string columnName, out decimal value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && decimal.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable decimal value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted decimal value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableDecimal(this DynamoRecord record, string columnName, out decimal? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (decimal.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a DateTime value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTime value, or default(DateTime) if not found.</param>
    /// <returns>True if the column exists and has a valid date string, false otherwise.</returns>
    public static bool TryGetDateTime(this DynamoRecord record, string columnName, out DateTime value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.S) && DateTime.TryParse(attributeValue.S, out value);
    }

    /// <summary>
    /// Attempts to get a nullable DateTime value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTime value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableDateTime(this DynamoRecord record, string columnName, out DateTime? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.S))
            return false;

        if (DateTime.TryParse(attributeValue.S, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get an enum value from the DynamoRecord.
    /// </summary>
    /// <typeparam name="T">The enum type to parse.</typeparam>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted enum value, or default(T) if not found.</param>
    /// <returns>True if the column exists and has a valid enum string, false otherwise.</returns>
    public static bool TryGetEnum<T>(this DynamoRecord record, string columnName, out T value) where T : struct, Enum
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.S) && Enum.TryParse<T>(attributeValue.S, out value);
    }

    /// <summary>
    /// Attempts to get a nullable enum value from the DynamoRecord.
    /// </summary>
    /// <typeparam name="T">The enum type to parse.</typeparam>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted enum value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableEnum<T>(this DynamoRecord record, string columnName, out T? value) where T : struct, Enum
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.S))
            return false;

        if (Enum.TryParse<T>(attributeValue.S, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a string set from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted string set, or empty collection if not found.</param>
    /// <returns>True if the column exists and has a valid string set, false otherwise.</returns>
    public static bool TryGetStringSet(this DynamoRecord record, string columnName, out IEnumerable<string> value)
    {
        value = Enumerable.Empty<string>();
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = Enumerable.Empty<string>();
            return true;
        }

        if (attributeValue.SS == null)
            return false;

        value = attributeValue.SS;
        return true;
    }

    /// <summary>
    /// Attempts to get an int set from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted int set, or empty collection if not found.</param>
    /// <returns>True if the column exists and has a valid number set, false otherwise.</returns>
    public static bool TryGetIntSet(this DynamoRecord record, string columnName, out IEnumerable<int> value)
    {
        value = Enumerable.Empty<int>();
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = Enumerable.Empty<int>();
            return true;
        }

        if (attributeValue.NS == null)
            return false;

        value = attributeValue.NS.Select(x => int.TryParse(x, out var val) ? val : (int?)null)
                                  .Where(x => x.HasValue)
                                  .Select(x => x!.Value);
        return true;
    }

    /// <summary>
    /// Attempts to get a long set from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted long set, or empty collection if not found.</param>
    /// <returns>True if the column exists and has a valid number set, false otherwise.</returns>
    public static bool TryGetLongSet(this DynamoRecord record, string columnName, out IEnumerable<long> value)
    {
        value = Enumerable.Empty<long>();
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = Enumerable.Empty<long>();
            return true;
        }

        if (attributeValue.NS == null)
            return false;

        value = attributeValue.NS.Select(x => long.TryParse(x, out var val) ? val : (long?)null)
                                  .Where(x => x.HasValue)
                                  .Select(x => x!.Value);
        return true;
    }

    /// <summary>
    /// Attempts to get a double set from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted double set, or empty collection if not found.</param>
    /// <returns>True if the column exists and has a valid number set, false otherwise.</returns>
    public static bool TryGetDoubleSet(this DynamoRecord record, string columnName, out IEnumerable<double> value)
    {
        value = Enumerable.Empty<double>();
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = Enumerable.Empty<double>();
            return true;
        }

        if (attributeValue.NS == null)
            return false;

        value = attributeValue.NS.Select(x => double.TryParse(x, out var val) ? val : (double?)null)
                                  .Where(x => x.HasValue)
                                  .Select(x => x!.Value);
        return true;
    }

    /// <summary>
    /// Attempts to get an enum set from the DynamoRecord.
    /// </summary>
    /// <typeparam name="T">The enum type to parse.</typeparam>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted enum set, or empty collection if not found.</param>
    /// <returns>True if the column exists and has a valid string set, false otherwise.</returns>
    public static bool TryGetEnumSet<T>(this DynamoRecord record, string columnName, out IEnumerable<T> value) where T : struct, Enum
    {
        value = Enumerable.Empty<T>();
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = Enumerable.Empty<T>();
            return true;
        }

        if (attributeValue.SS == null)
            return false;

        value = attributeValue.SS.Select(x => Enum.TryParse<T>(x, out var val) ? val : (T?)null)
                                  .Where(x => x.HasValue)
                                  .Select(x => x!.Value);
        return true;
    }

    /// <summary>
    /// Attempts to get a byte value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted byte value, or default(byte) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetByte(this DynamoRecord record, string columnName, out byte value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && byte.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable byte value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted byte value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableByte(this DynamoRecord record, string columnName, out byte? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (byte.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get an sbyte value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted sbyte value, or default(sbyte) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetSByte(this DynamoRecord record, string columnName, out sbyte value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && sbyte.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable sbyte value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted sbyte value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableSByte(this DynamoRecord record, string columnName, out sbyte? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (sbyte.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a ushort value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted ushort value, or default(ushort) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetUShort(this DynamoRecord record, string columnName, out ushort value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && ushort.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable ushort value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted ushort value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableUShort(this DynamoRecord record, string columnName, out ushort? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (ushort.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a uint value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted uint value, or default(uint) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetUInt(this DynamoRecord record, string columnName, out uint value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && uint.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable uint value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted uint value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableUInt(this DynamoRecord record, string columnName, out uint? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (uint.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a ulong value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted ulong value, or default(ulong) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric value, false otherwise.</returns>
    public static bool TryGetULong(this DynamoRecord record, string columnName, out ulong value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.N) && ulong.TryParse(attributeValue.N, out value);
    }

    /// <summary>
    /// Attempts to get a nullable ulong value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted ulong value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableULong(this DynamoRecord record, string columnName, out ulong? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (ulong.TryParse(attributeValue.N, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a DateTimeOffset value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTimeOffset value, or default(DateTimeOffset) if not found.</param>
    /// <returns>True if the column exists and has a valid date string, false otherwise.</returns>
    public static bool TryGetDateTimeOffset(this DynamoRecord record, string columnName, out DateTimeOffset value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.S) && DateTimeOffset.TryParse(attributeValue.S, out value);
    }

    /// <summary>
    /// Attempts to get a nullable DateTimeOffset value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTimeOffset value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableDateTimeOffset(this DynamoRecord record, string columnName, out DateTimeOffset? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.S))
            return false;

        if (DateTimeOffset.TryParse(attributeValue.S, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a TimeSpan value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted TimeSpan value, or default(TimeSpan) if not found.</param>
    /// <returns>True if the column exists and has a valid timespan string, false otherwise.</returns>
    public static bool TryGetTimeSpan(this DynamoRecord record, string columnName, out TimeSpan value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.S) && TimeSpan.TryParse(attributeValue.S, out value);
    }

    /// <summary>
    /// Attempts to get a nullable TimeSpan value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted TimeSpan value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableTimeSpan(this DynamoRecord record, string columnName, out TimeSpan? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.S))
            return false;

        if (TimeSpan.TryParse(attributeValue.S, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a Guid value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted Guid value, or default(Guid) if not found.</param>
    /// <returns>True if the column exists and has a valid GUID string, false otherwise.</returns>
    public static bool TryGetGuid(this DynamoRecord record, string columnName, out Guid value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        return !string.IsNullOrEmpty(attributeValue.S) && Guid.TryParse(attributeValue.S, out value);
    }

    /// <summary>
    /// Attempts to get a nullable Guid value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted Guid value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableGuid(this DynamoRecord record, string columnName, out Guid? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.S))
            return false;

        if (Guid.TryParse(attributeValue.S, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a DateTime set from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTime set, or empty collection if not found.</param>
    /// <returns>True if the column exists and has a valid string set, false otherwise.</returns>
    public static bool TryGetDateTimeSet(this DynamoRecord record, string columnName, out IEnumerable<DateTime> value)
    {
        value = Enumerable.Empty<DateTime>();
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = Enumerable.Empty<DateTime>();
            return true;
        }

        if (attributeValue.SS == null)
            return false;

        value = attributeValue.SS.Select(x => DateTime.TryParse(x, out var val) ? val : (DateTime?)null)
                                  .Where(x => x.HasValue)
                                  .Select(x => x!.Value);
        return true;
    }

    /// <summary>
    /// Attempts to get a dictionary of string keys and string values from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted dictionary, or empty dictionary if not found.</param>
    /// <returns>True if the column exists and has a valid map value, false otherwise.</returns>
    public static bool TryGetStringDictionary(this DynamoRecord record, string columnName, out Dictionary<string, string> value)
    {
        value = new Dictionary<string, string>();
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return true; // Empty dictionary for NULL

        if (attributeValue.M == null)
            return false;

        foreach (var kvp in attributeValue.M)
        {
            if (kvp.Value?.S != null)
            {
                value[kvp.Key] = kvp.Value.S;
            }
        }
        return true;
    }

    /// <summary>
    /// Attempts to get a nullable dictionary of string keys and string values from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted dictionary, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableStringDictionary(this DynamoRecord record, string columnName, out Dictionary<string, string>? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (attributeValue.M == null)
            return false;

        value = new Dictionary<string, string>();
        foreach (var kvp in attributeValue.M)
        {
            if (kvp.Value?.S != null)
            {
                value[kvp.Key] = kvp.Value.S;
            }
        }
        return true;
    }

    /// <summary>
    /// Attempts to get a dictionary with string keys and numeric values from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted dictionary, or empty dictionary if not found.</param>
    /// <returns>True if the column exists and has a valid map value, false otherwise.</returns>
    public static bool TryGetStringIntDictionary(this DynamoRecord record, string columnName, out Dictionary<string, int> value)
    {
        value = new Dictionary<string, int>();
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return true; // Empty dictionary for NULL

        if (attributeValue.M == null)
            return false;

        foreach (var kvp in attributeValue.M)
        {
            if (kvp.Value?.N != null && int.TryParse(kvp.Value.N, out var intValue))
            {
                value[kvp.Key] = intValue;
            }
        }
        return true;
    }

    /// <summary>
    /// Attempts to get a nullable dictionary with string keys and numeric values from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted dictionary, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableStringIntDictionary(this DynamoRecord record, string columnName, out Dictionary<string, int>? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (attributeValue.M == null)
            return false;

        value = new Dictionary<string, int>();
        foreach (var kvp in attributeValue.M)
        {
            if (kvp.Value?.N != null && int.TryParse(kvp.Value.N, out var intValue))
            {
                value[kvp.Key] = intValue;
            }
        }
        return true;
    }

    /// <summary>
    /// Attempts to get a DateTime value from a unix timestamp in seconds stored as numeric value in the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTime value, or default(DateTime) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric unix timestamp, false otherwise.</returns>
    public static bool TryGetUnixTimestampSeconds(this DynamoRecord record, string columnName, out DateTime value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (long.TryParse(attributeValue.N, out var unixSeconds))
        {
            try
            {
                value = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).DateTime;
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a nullable DateTime value from a unix timestamp in seconds stored as numeric value in the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTime value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableUnixTimestampSeconds(this DynamoRecord record, string columnName, out DateTime? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (long.TryParse(attributeValue.N, out var unixSeconds))
        {
            try
            {
                value = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).DateTime;
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a DateTime value from a unix timestamp in milliseconds stored as numeric value in the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTime value, or default(DateTime) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric unix timestamp, false otherwise.</returns>
    public static bool TryGetUnixTimestampMilliseconds(this DynamoRecord record, string columnName, out DateTime value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (long.TryParse(attributeValue.N, out var unixMilliseconds))
        {
            try
            {
                value = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).DateTime;
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a nullable DateTime value from a unix timestamp in milliseconds stored as numeric value in the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTime value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableUnixTimestampMilliseconds(this DynamoRecord record, string columnName, out DateTime? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (long.TryParse(attributeValue.N, out var unixMilliseconds))
        {
            try
            {
                value = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).DateTime;
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a DateTimeOffset value from a unix timestamp in seconds stored as numeric value in the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTimeOffset value, or default(DateTimeOffset) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric unix timestamp, false otherwise.</returns>
    public static bool TryGetUnixTimestampSecondsAsOffset(this DynamoRecord record, string columnName, out DateTimeOffset value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (long.TryParse(attributeValue.N, out var unixSeconds))
        {
            try
            {
                value = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a nullable DateTimeOffset value from a unix timestamp in seconds stored as numeric value in the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTimeOffset value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableUnixTimestampSecondsAsOffset(this DynamoRecord record, string columnName, out DateTimeOffset? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (long.TryParse(attributeValue.N, out var unixSeconds))
        {
            try
            {
                value = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a DateTimeOffset value from a unix timestamp in milliseconds stored as numeric value in the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTimeOffset value, or default(DateTimeOffset) if not found.</param>
    /// <returns>True if the column exists and has a valid numeric unix timestamp, false otherwise.</returns>
    public static bool TryGetUnixTimestampMillisecondsAsOffset(this DynamoRecord record, string columnName, out DateTimeOffset value)
    {
        value = default;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
            return false;

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (long.TryParse(attributeValue.N, out var unixMilliseconds))
        {
            try
            {
                value = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to get a nullable DateTimeOffset value from a unix timestamp in milliseconds stored as numeric value in the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DateTimeOffset value, or null if not found or NULL attribute.</param>
    /// <returns>True if the column exists, false otherwise.</returns>
    public static bool TryGetNullableUnixTimestampMillisecondsAsOffset(this DynamoRecord record, string columnName, out DateTimeOffset? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (string.IsNullOrEmpty(attributeValue.N))
            return false;

        if (long.TryParse(attributeValue.N, out var unixMilliseconds))
        {
            try
            {
                value = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Attempts to get a DynamoRecord (Map) value from the DynamoRecord.
    /// </summary>
    /// <param name="record">The DynamoRecord to extract from.</param>
    /// <param name="columnName">The column name to extract.</param>
    /// <param name="value">The extracted DynamoRecord, or null if not found.</param>
    /// <returns>True if the column exists and has a Map value, false otherwise.</returns>
    public static bool TryGetMap(this DynamoRecord record, string columnName, out DynamoRecord? value)
    {
        value = null;
        if (!record.TryGetValue(columnName, out var attributeValue))
            return false;

        if (attributeValue == null || attributeValue.NULL == true)
        {
            value = null;
            return true;
        }

        if (attributeValue.M == null)
            return false;

        value = new DynamoRecord(attributeValue.M);
        return true;
    }
}
