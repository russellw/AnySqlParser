namespace AnySqlParser;
public sealed class Column {
	public string Name;
	public DataType DataType;
	public Expression? Default;
	public bool AutoIncrement;
	public bool PrimaryKey;
	public bool Nullable = true;
	public List<string> Ignored = new();

	public Column(string name, DataType dataType) {
		Name = name;
		DataType = dataType;
	}
}
