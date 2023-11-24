namespace AnySqlParser;
public sealed class Exists: Expression {
	public Select Query;

	public Exists(Location location, Select query): base(location) {
		Query = query;
	}
}
