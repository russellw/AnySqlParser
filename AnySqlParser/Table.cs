namespace AnySqlParser;
public sealed class Table: Statement {
	public QualifiedName Name;
	public List<Column> Columns = new();
	public List<Key> Keys = new();
	public List<ForeignKey> ForeignKeys = new();
	public List<Check> Checks = new();
	public StorageOption? On;
	public StorageOption? TextimageOn;
	public StorageOption? FilestreamOn;

	public Table(Location location, QualifiedName name): base(location) {
		Name = name;
	}
}
