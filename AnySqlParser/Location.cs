namespace AnySqlParser;
public readonly struct Location {
	public readonly string File;
	public readonly int Line;

	public Location(string file, int line) {
		File = file;
		Line = line;
	}
}
