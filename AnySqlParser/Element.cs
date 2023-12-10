namespace AnySqlParser;
public abstract class Element {
	public Location Location;

	protected Element(Location location) {
		Location = location;
	}
}
