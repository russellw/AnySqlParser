namespace AnySqlParser;
public sealed class TornPageDetection: AlterDatabaseSetOption {
	public bool On;

	public TornPageDetection(bool on) {
		On = on;
	}
}
