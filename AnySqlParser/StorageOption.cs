namespace AnySqlParser;
public abstract class StorageOption {
	public readonly Location Location;

	protected StorageOption(Location location) {
		Location = location;
	}
}
