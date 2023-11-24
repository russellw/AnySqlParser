namespace AnySqlParser;
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
