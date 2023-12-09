using AnySqlParser;
using System.Diagnostics;

class Program {
	static void Main(string[] args) {
		var stopwatch = Stopwatch.StartNew();
		int n = 0;
		for (int i = 0; i < 200; i++)
			foreach (var file in args)
				foreach (var a in Parser.Parse(file))
					n++;
		Console.WriteLine(n);
		Console.WriteLine(stopwatch.Elapsed);
	}
}
