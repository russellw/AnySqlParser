namespace AnySqlParser;
public abstract class Element {
	public readonly Location Location;
	public List<string> ExtraTokens = new();

	protected Element(Location location) {
		Location = location;
	}
}
