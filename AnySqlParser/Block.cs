namespace AnySqlParser;
public sealed class Block: Statement {
	public List<Statement> Body = new();

	public Block(Location location): base(location) {
	}
}
