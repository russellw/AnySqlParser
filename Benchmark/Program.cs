using AnySqlParser;
using System.Diagnostics;

class Program {
	static void Main(string[] args) {
		var stopwatch = Stopwatch.StartNew();
		int n = 0;
		for (int i = 0; i < 200; i++)
			foreach (var file in args) {
				var schema = new Schema();
				foreach (var _ in Parser.Parse(file, schema))
					n++;
			}
		Console.WriteLine(n);
		Console.WriteLine(stopwatch.Elapsed);
	}
}
