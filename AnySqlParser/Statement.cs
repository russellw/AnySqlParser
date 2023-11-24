namespace AnySqlParser;
public abstract class Statement {
	public readonly Location Location;

	protected Statement(Location location) {
		Location = location;
	}
}
