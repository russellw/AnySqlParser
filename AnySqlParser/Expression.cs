namespace AnySqlParser;
public abstract class Expression {
	public readonly Location Location;

	protected Expression(Location location) {
		Location = location;
	}

	// The omission of an override for Equals is intentional
	// in most cases, syntax trees have reference semantics
	// equality comparison by value is useful only in unusual situations
	// and should not be the default
	public virtual bool Eq(Expression b) {
		return this == b;
	}
}
