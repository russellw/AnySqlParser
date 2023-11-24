namespace AnySqlParser;
public sealed class Checkpoint: Statement {
	public int Duration;

	public Checkpoint(Location location): base(location) {
	}
}
