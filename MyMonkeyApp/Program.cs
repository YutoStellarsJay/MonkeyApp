namespace MyMonkeyApp;

using System;
using System.Threading.Tasks;
using MyMonkeyApp.Helpers;
using MyMonkeyApp.Models;

/// <summary>
/// Console UI for exploring monkeys via <see cref="MonkeyHelper"/>.
/// </summary>
internal static class Program
{
	private static readonly string[] AsciiArts =
	{
		"(\\_/)\n( •_•)\n/ >🐒",
		"  _\n ('_')\n/)>🐵",
		"  .-\"\"-.\n /      \\n|  O  O |\n|  \\__/ |\n \\      /\n  `----`"
	};

	private static async Task Main(string[] args)
	{
		Console.Clear();
		while (true)
		{
			PrintMenu();
			string? input = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(input)) // If its empty before all checks skip
				continue;
			else if (input == "exit")
			{
				Console.WriteLine("Goodbye!"); // If input is "exit" exit before all one character checks
				return;
			}
			if (input.Length == 1)
			{
				switch (input.Trim()[0])
				{
					case '1':
						await ListAllMonkeysAsync();
						break;
					case '2':
						await GetDetailsByNameAsync();
						break;
					case '3':
						await GetRandomMonkeyAsync();
						break;
					case '4':
					case 'q':
						Console.WriteLine("Goodbye!"); // 4 or q now also do quit (Before they stacked over default)
						return;
					default:
						Console.WriteLine("Unknown option. Choose 1-4.");
						break;
				}
			}
		}
	}

	private static void PrintMenu()
	{
		Console.Write(@"
Monkey Explorer
1) List all monkeys
2) Get details for a monkey by name
3) Get a random monkey
4) Exit (or q)
Select an option: ");
	}

	private static async Task ListAllMonkeysAsync()
	{
		Console.Clear();
		var monkeys = await MonkeyHelper.GetMonkeysAsync();
		if (monkeys == null || monkeys.Count == 0)
		{
			Console.WriteLine("No monkeys available.");
			return;
		}
		Console.WriteLine("\n{0,-25} {1,-30} {2,10} {3,10} {4,10}", "Name", "Location", "Population", "Latitude", "Longitude");
		Console.WriteLine(new string('-', 95));
		foreach (Monkey m in monkeys)
		{
			Console.WriteLine("{0,-25} {1,-30} {2,10} {3,10:F6} {4,10:F6}", Truncate(m.Name, 25), Truncate(m.Location, 30), m.Population, m.Latitude, m.Longitude);
		}
	}

	private static async Task GetDetailsByNameAsync()
	{
		Console.Clear();
		Console.Write("Enter monkey name: ");
		string? name = Console.ReadLine();
		if (string.IsNullOrWhiteSpace(name))
		{
			Console.WriteLine("Name cannot be empty.");
			return;
		}

		Monkey? monkey = await MonkeyHelper.GetMonkeyByNameAsync(name.Trim());
		if (monkey == null)
		{
			Console.WriteLine($"No monkey found with name '{name}'.");
			return;
		}

		PrintMonkeyDetails(monkey);
	}

	private static async Task GetRandomMonkeyAsync()
	{
		Console.Clear();
		Monkey? monkey = await MonkeyHelper.GetRandomMonkeyAsync();
		if (monkey == null)
		{
			Console.WriteLine("No monkeys available to pick.");
			return;
		}

		// show ASCII art randomly
		string art = AsciiArts[Random.Shared.Next(AsciiArts.Length)];
		Console.WriteLine($"\n{art}\n");
		PrintMonkeyDetails(monkey);
		int count = MonkeyHelper.GetAccessCount(monkey.Name);
		Console.WriteLine($"(random-picked count for {monkey.Name}: {count})");
	}

	private static void PrintMonkeyDetails(Monkey m)
	{
		Console.WriteLine($@"Name: {m.Name}
Location: {m.Location}
Population: {m.Population}
Coordinates: {m.Latitude:F6}, {m.Longitude:F6}
Image: {m.Image}
Details:
{m.Details}");
	}
	private static string Truncate(string? value, int maxLength)
		 => string.IsNullOrEmpty(value) ? "" : (value.Length > maxLength ? value[..(maxLength - 3)] + "..." : value);
}
