using AnySqlParser;

class Program {
	static void Descend(string path) {
		try {
			if (Directory.Exists(path))
				foreach (var entry in Directory.GetFileSystemEntries(path))
					Descend(entry);
			else if (string.Equals(Path.GetExtension(path), ".sql", StringComparison.OrdinalIgnoreCase))
				Do(path);
		} catch (UnauthorizedAccessException e) {
			Console.WriteLine(e.Message);
		}
	}

	static void Do(string file) {
		Console.WriteLine(file);
		var schema = new Schema();
		foreach (var _ in Parser.Parse(file, schema)) {
		}
		var s1 = SqlServerComposer.Compose(schema);
	}

	static void Help() {
		var name = typeof(Program).Assembly.GetName().Name;
		Console.WriteLine($"Usage: {name} [options] file...");
		Console.WriteLine();
		Console.WriteLine("-h  Show help");
		Console.WriteLine("-V  Show version");
	}

	static void Main(string[] args) {
		var options = true;
		var paths = new List<string>();
		foreach (var arg in args) {
			var s = arg;
			if (options) {
				if ("--" == s) {
					options = false;
					continue;
				}
				if (s.StartsWith('-')) {
					while (s.StartsWith('-'))
						s = s[1..];
					switch (s) {
					case "?":
					case "h":
					case "help":
						Help();
						return;
					case "V":
					case "v":
					case "version":
						Version();
						return;
					default:
						Console.WriteLine(arg + ": unknown option");
						Environment.Exit(1);
						break;
					}
				}
			}
			paths.Add(s);
		}
		if (0 == paths.Count)
			paths.Add(".");

		foreach (var path in paths)
			Descend(path);
	}

	static void Version() {
		var name = typeof(Program).Assembly.GetName().Name;
		var version = typeof(Program).Assembly.GetName()?.Version?.ToString(2);
		Console.WriteLine($"{name} {version}");
	}
}
