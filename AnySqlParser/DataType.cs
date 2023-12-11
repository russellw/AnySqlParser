namespace AnySqlParser;
public record struct DataType {
	public string Name;
	public Expression? Size;
	public Expression? Scale;
	public List<string>? Values;

	public DataType(string name) {
		Name = name;
	}
}
