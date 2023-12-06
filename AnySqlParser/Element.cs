namespace AnySqlParser;
public abstract class Element {
	public readonly Location Location;
	public List<string> Ignored = new();

	protected Element(Location location) {
		Location = location;
	}
}
