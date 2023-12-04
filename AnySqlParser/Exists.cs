namespace AnySqlParser;
public sealed class Exists: Expression {
	public Select Query;

	public Exists(Select query) {
		Query = query;
	}
}
