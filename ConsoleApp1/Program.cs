using AnySqlParser;

class Program {
	static void Main(string[] args) {
		foreach (var file in args)
			foreach (var a in Parser.Parse(file)) {
				if (a is ExtraText extraText) {
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine(extraText.Location);
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine(extraText.Text);
					Console.ResetColor();
					continue;
				}
				Console.WriteLine(a);
			}
	}
}
