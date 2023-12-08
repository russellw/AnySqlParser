using AnySqlParser;

class Program {
	static void Main(string[] args) {
		foreach (var file in args)
			foreach (var a in Parser.Parse(file)) {
				if (!(a is ExtraText))
					Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(a);
				Console.ResetColor();
			}
	}
}
