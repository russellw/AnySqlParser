namespace AnySqlParser;
public readonly struct Token {
	public readonly Location Location;
	public readonly int Type;
	public readonly string? String;

	public Token(Location location, int type): this() {
		Location = location;
		Type = type;
	}
}
