namespace AnySqlParser {
public sealed class Procedure: Statement {
	public bool OrAlter;
	public QualifiedName Name = null!;
	public int Number;
	public List<string> Parameters = new();
	public bool Encryption;
	public bool Recompile;
	public bool ForReplication;
	public List<Statement> Body = new();

	public Procedure(Location location): base(location) {
	}
}
}
