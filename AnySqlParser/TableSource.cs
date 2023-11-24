namespace AnySqlParser;
public abstract class TableSource {
	public readonly Location Location;

	public TableSource(Location location) {
		Location = location;
	}
}
