namespace AnySqlParser;
public struct Location {
	public string File;
	public int Line;

	public Location(string file, int line) {
		File = file;
		Line = line;
	}

	public override string ToString() {
		return $"{File}:{Line}";
	}
}
