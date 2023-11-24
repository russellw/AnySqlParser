namespace AnySqlParser;
public sealed class ExecuteProcedure: Statement {
	public string ProcedureName;
	public List<Expression> Arguments = new();

	public ExecuteProcedure(Location location, string procedureName): base(location) {
		ProcedureName = procedureName;
	}
}
