namespace AnySqlParser;
public sealed class Raiserror: Statement {
	public List<Expression> Arguments = new();
	public bool Log;
	public bool Nowait;
	public bool Seterror;

	public Raiserror(Location location): base(location) {
	}
}
