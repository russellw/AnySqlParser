namespace AnySqlParser;
public sealed class RollbackAfter: Termination {
	public int Seconds;

	public RollbackAfter(int seconds) {
		Seconds = seconds;
	}
}
