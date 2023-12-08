namespace AnySqlParser;
public sealed class ExtraText: Statement {
	public readonly Location Location;
	public string Text;

	public ExtraText(Location location, string text) {
		Location = location;
		Text = text;
	}
}
