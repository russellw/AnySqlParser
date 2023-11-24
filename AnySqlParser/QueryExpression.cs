namespace AnySqlParser;
public abstract class QueryExpression {
	public readonly Location Location;

	public QueryExpression(Location location) {
		Location = location;
	}
}
