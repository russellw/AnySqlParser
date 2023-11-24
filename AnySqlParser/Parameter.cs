namespace AnySqlParser;
public sealed class Parameter {
	public readonly Location Location;
	public string Name;
	public DataType DataType;
	public bool Varying;
	public bool Nullable;
	public Expression? Default;
	public bool Out;

	public Parameter(Location location, string name, DataType dataType) {
		Location = location;
		Name = name;
		DataType = dataType;
	}
}
