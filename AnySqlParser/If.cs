namespace AnySqlParser;
public sealed class If: Statement {
	public Expression condition = null!;
	public Statement then = null!;
	public Statement? @else;

	public If(Location location): base(location) {
	}
}
