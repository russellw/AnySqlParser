using AnySqlParser;

class Program {
	static void Main(string[] args) {
		var extraText = false;
		var files = new List<string>();
		foreach (var arg in args) {
			var s = arg;
			if (s.StartsWith('-')) {
				while (s.StartsWith('-'))
					s = s[1..];
				switch (s) {
				case "x":
					extraText = true;
					continue;
				default:
					Console.WriteLine(arg + ": unknown option");
					Environment.Exit(1);
					break;
				}
			}
			files.Add(s);
		}
		if (extraText) {
			foreach (var file in args)
				foreach (var a in Parser.Parse(file)) {
					if (a is ExtraText extra) {
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine(extra.Location);
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine(extra.Text);
						Console.ResetColor();
						continue;
					}
					Console.WriteLine(a);
				}
			return;
		}
	}
}
