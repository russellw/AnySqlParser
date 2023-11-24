namespace AnySqlParser;
public sealed class Column {
	public readonly Location Location;
	public string Name;
	public DataType DataType;

	// Etc
	public bool Filestream;
	public bool Sparse;
	public Expression? Default;

	public bool Identity;
	public int IdentitySeed = -1;
	public int IdentityIncrement = -1;

	public bool ForReplication = true;
	public bool Nullable = true;
	public bool Rowguidcol;

	// Constraints
	public bool PrimaryKey;

	public Column(Location location, string name, DataType dataType) {
		Location = location;
		Name = name;
		DataType = dataType;
	}
}
