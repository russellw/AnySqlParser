namespace AnySqlParser;
public sealed class LocalVariable {
	public readonly Location Location;
	public string Name;
	public DataType DataType;
	public Expression? Value;

	public LocalVariable(Location location, string name, DataType dataType) {
		Location = location;
		Name = name;
		DataType = dataType;
	}
}
