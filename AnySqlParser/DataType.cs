namespace AnySqlParser;
public record struct DataType {
	public string Name;
	public int Size = -1;
	public int Scale = -1;
	public List<string>? Values;

	public DataType(string name) {
		Name = name;
	}
}
