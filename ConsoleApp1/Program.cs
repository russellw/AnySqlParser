﻿using AnySqlParser;

class Program {
	static void Main(string[] args) {
		var schema = new Schema();
		foreach (var file in args)
			foreach (var _ in Parser.Parse(file, schema)) {
			}
		Console.Write(SqlServerComposer.Compose(schema));
	}
}
