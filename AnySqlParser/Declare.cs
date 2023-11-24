namespace AnySqlParser;
public sealed class Declare: Statement {
	public List<LocalVariable> LocalVariables = new();
	public List<CursorVariable> CursorVariables = new();

	public Declare(Location location): base(location) {
	}
}
