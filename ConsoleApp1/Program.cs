using AnySqlParser;

class Program {
	static void Main(string[] args) {
		var database = new Schema();
		foreach (var file in args)
			foreach (var a in Parser.Parse(file))
				a.AddTo(database);
		Console.Write(SqlServerComposer.Compose(database));
	}
}
