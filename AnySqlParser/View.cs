namespace AnySqlParser;
public sealed class View: Statement {
	public QualifiedName Name = null!;
	public Select Query = null!;

	public View(Location location): base(location) {
	}
}
