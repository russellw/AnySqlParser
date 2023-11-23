namespace AnySqlParser {
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

public sealed class Procedure: Statement {
	public bool OrAlter;
	public QualifiedName Name;
	public int Number;
	public List<Parameter> Parameters = new();
	public bool Encryption;
	public bool Recompile;
	public bool ForReplication;
	public List<Statement> Body = new();

	public Procedure(Location location, QualifiedName name): base(location) {
		Name = name;
	}
}
}
