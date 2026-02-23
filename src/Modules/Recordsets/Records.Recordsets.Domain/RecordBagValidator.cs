using System.Collections;
using System.Text.Json;
using System.Text.RegularExpressions;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Domain;

public sealed class RecordBagValidator : IRecordBagValidator
{
    public void Validate(Recordset recordset, object bag)
    {
        var bagDict = GetBagDictionary(bag);

        foreach (var column in recordset.Columns)
        {
            var key = column.StorageKey ?? column.Property;
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            bagDict.TryGetValue(key, out var value);

            if (column.Required && IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"Column '{column.Name}' is required.");
            }

            if (value is null)
            {
                continue;
            }

            EnsureTypeCompatible(column, value);
            EnsureAllowedValues(column, value);
            EnsureNumberBounds(column, value);
            EnsureRegex(column, value);
        }

        EnsureStatusValid(recordset, bagDict);
    }

    public void ValidateTransition(Recordset recordset, object? previousBag, object nextBag)
    {
        var prevDict = previousBag is null ? null : GetBagDictionary(previousBag);
        var nextDict = GetBagDictionary(nextBag);

        var oldStatus = GetStatus(prevDict);
        var newStatus = GetStatus(nextDict);

        if (string.IsNullOrWhiteSpace(oldStatus) || string.IsNullOrWhiteSpace(newStatus))
        {
            return;
        }

        if (string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var transition = recordset.StatusTransitions
            .FirstOrDefault(t => string.Equals(t.From, oldStatus, StringComparison.OrdinalIgnoreCase));

        var allowed = transition?.AllowedNext
            .Any(next => string.Equals(next, newStatus, StringComparison.OrdinalIgnoreCase)) ?? false;

        if (!allowed)
        {
            throw new InvalidOperationException($"Transition from '{oldStatus}' to '{newStatus}' is not allowed.");
        }
    }

    private static Dictionary<string, object?> GetBagDictionary(object bag)
    {
        if (bag is Dictionary<string, object?> dict)
        {
            return new Dictionary<string, object?>(dict, StringComparer.OrdinalIgnoreCase);
        }

        if (bag is IDictionary<string, object?> idict)
        {
            return idict.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
        }

        if (bag is IDictionary nonGeneric)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in nonGeneric)
            {
                if (entry.Key is string key)
                {
                    result[key] = entry.Value;
                }
            }

            return result;
        }

        if (bag is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ConvertJsonElement(property.Value);
            }

            return result;
        }

        throw new InvalidOperationException("Bag must be an object or dictionary payload.");
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetDecimal(out var dec) ? dec : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => element,
            JsonValueKind.Array => element,
            _ => element.ToString()
        };
    }

    private static bool IsNullOrEmpty(object? value)
    {
        return value is null || (value is string s && string.IsNullOrWhiteSpace(s));
    }

    private static void EnsureTypeCompatible(Column column, object value)
    {
        var ok = column.Type switch
        {
            ColumnType.Text => value is string,
            ColumnType.Number => value is sbyte or byte or short or ushort or int or uint or long or ulong or float
                or double or decimal,
            ColumnType.Boolean => value is bool,
            ColumnType.Date => value is DateTime or DateTimeOffset or string,
            _ => true
        };

        if (!ok)
        {
            throw new InvalidOperationException($"Column '{column.Name}' expects {column.Type}.");
        }
    }

    private static void EnsureAllowedValues(Column column, object value)
    {
        if (column.AllowedValues is null || value is not string str)
        {
            return;
        }

        var allowed = column.AllowedValues.Any(v => string.Equals(v, str, StringComparison.OrdinalIgnoreCase));
        if (!allowed)
        {
            throw new InvalidOperationException($"Column '{column.Name}' has invalid value '{str}'.");
        }
    }

    private static void EnsureNumberBounds(Column column, object value)
    {
        if (column.MinNumber is null && column.MaxNumber is null)
        {
            return;
        }

        if (!TryToDecimal(value, out var number))
        {
            return;
        }

        if (column.MinNumber.HasValue && number < column.MinNumber.Value)
        {
            throw new InvalidOperationException($"Column '{column.Name}' is below minimum.");
        }

        if (column.MaxNumber.HasValue && number > column.MaxNumber.Value)
        {
            throw new InvalidOperationException($"Column '{column.Name}' exceeds maximum.");
        }
    }

    private static void EnsureRegex(Column column, object value)
    {
        if (string.IsNullOrWhiteSpace(column.Regex) || value is not string str)
        {
            return;
        }

        if (!Regex.IsMatch(str, column.Regex))
        {
            throw new InvalidOperationException($"Column '{column.Name}' does not match required pattern.");
        }
    }

    private static void EnsureStatusValid(Recordset recordset, Dictionary<string, object?> bagDict)
    {
        if (!bagDict.TryGetValue("status", out var raw) || raw is null)
        {
            return;
        }

        var status = raw.ToString();
        if (string.IsNullOrWhiteSpace(status))
        {
            return;
        }

        var isValid = recordset.Statuses.Any(s => string.Equals(s.Name, status, StringComparison.OrdinalIgnoreCase));
        if (!isValid)
        {
            throw new InvalidOperationException($"Status '{status}' is not valid for this recordset.");
        }
    }

    private static string? GetStatus(Dictionary<string, object?>? bagDict)
    {
        if (bagDict is null)
        {
            return null;
        }

        if (!bagDict.TryGetValue("status", out var raw) || raw is null)
        {
            return null;
        }

        return raw.ToString();
    }

    private static bool TryToDecimal(object value, out decimal number)
    {
        switch (value)
        {
            case decimal dec:
                number = dec;
                return true;
            case float f:
                number = (decimal)f;
                return true;
            case double d:
                number = (decimal)d;
                return true;
            case int i:
                number = i;
                return true;
            case long l:
                number = l;
                return true;
            case string s when decimal.TryParse(s, out var parsed):
                number = parsed;
                return true;
            default:
                number = 0;
                return false;
        }
    }
}