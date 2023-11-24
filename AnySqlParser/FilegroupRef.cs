namespace AnySqlParser;
public sealed class FilegroupRef: StorageOption {
	public string FilegroupName;

	public FilegroupRef(Location location, string filegroupName): base(location) {
		FilegroupName = filegroupName;
	}
}
