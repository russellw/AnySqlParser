namespace AnySqlParser;
public sealed class CursorVariable {
	public readonly Location Location;
	public string Name;

	public CursorVariable(Location location, string name) {
		Location = location;
		Name = name;
	}
}
