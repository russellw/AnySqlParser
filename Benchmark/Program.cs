using AnySqlParser;

class Program {
	static void Main(string[] args) {
        int n = 0;
        foreach (var file in args)
            foreach (var a in Parser.Parse(file))
                n++;
        Console.WriteLine(n);
    }
}
