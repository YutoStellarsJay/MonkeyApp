namespace MyMonkeyApp.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MyMonkeyApp.Models;
using System.Security.Cryptography;
using System.Text;
using System.Security;

/// <summary>
/// Static helper that manages an in-memory collection of monkeys seeded from the MCP sample data.
/// Provides methods to get all monkeys, get a random monkey, find by name and track how often random picks occur.
/// </summary>
public static class MonkeyHelper
{
    private static readonly List<Monkey> _monkeys;
    private static readonly Dictionary<string, int> _accessCounts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _lock = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static MonkeyHelper()
    {
        using (SHA512 sha = SHA512.Create())
        {
            string hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(File.ReadAllText("DefaultMonkeys.json"))));
            if (hash != "HpLvjmzqGi8UmYYam3SDZiwq7rbqBw/Uac9l20SjjOzBrPlumXum+hUfWoS+QCRm++fTMwC5wnhAeAifzZmMPw==")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Basic Security Check Failed: File to get monkeys was edited 🙈🙉");
                Console.ResetColor();
                throw new SecurityException("Default monkey file should not be edited (Basic security check)");
            }
        }
        try
        {
            _monkeys = JsonSerializer.Deserialize<List<Monkey>>(File.ReadAllText("DefaultMonkeys.json"), _jsonOptions) ?? new();

            // initialize access counts (no lock required here; static ctor is thread-safe)
            foreach (Monkey m in _monkeys)
            {
                if (!(string.IsNullOrWhiteSpace(m.Name) && _accessCounts.ContainsKey(m.Name)))
                    _accessCounts[m.Name] = 0;
            }
        }
        catch (Exception ex)
        {
            // If parsing fails, fall back to an empty list but keep the helper usable.
            _monkeys = new List<Monkey>();
            _accessCounts.Clear();
            Console.Error.WriteLine($"Failed to initialize MonkeyHelper: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns all monkeys as a read-only list.
    /// </summary>
    public static Task<IReadOnlyList<Monkey>> GetMonkeysAsync()
        => Task.FromResult((IReadOnlyList<Monkey>)_monkeys);

    /// <summary>
    /// Returns a monkey by case-insensitive name match, or null if not found.
    /// </summary>
    /// <param name="name">Name to look up.</param>
    public static Task<Monkey?> GetMonkeyByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult<Monkey?>(null);
        }

        var found = _monkeys.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(found);
    }

    /// <summary>
    /// Returns a random monkey from the collection and increments its random-access counter.
    /// </summary>
    public static Task<Monkey?> GetRandomMonkeyAsync()
    {
        if (_monkeys.Count == 0)
        {
            return Task.FromResult<Monkey?>(null);
        }

        int index = Random.Shared.Next(_monkeys.Count);
        Monkey selected = _monkeys[index];

        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(selected.Name))
                // do nothing for unnamed entries
                return Task.FromResult<Monkey?>(selected);

            if (_accessCounts.ContainsKey(selected.Name))
                _accessCounts[selected.Name]++;
            else
                _accessCounts[selected.Name] = 1;
        }

        return Task.FromResult<Monkey?>(selected);
    }

    /// <summary>
    /// Returns how many times <see cref="GetRandomMonkeyAsync"/> returned the named monkey.
    /// </summary>
    public static int GetAccessCount(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return 0;

        lock (_lock)
        {
            _accessCounts.TryGetValue(name, out var count);
            return count;
        }
    }
}
