namespace AnySqlParser;
public sealed class ForeignKey: Element {
	public List<ColumnRef> Columns = new();
	public Action OnDelete = Action.NoAction;
	public Action OnUpdate = Action.NoAction;
	public List<ColumnRef> RefColumns = new();
	public TableRef RefTable;

	public ForeignKey(Location location): base(location) {
	}
}
