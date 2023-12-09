using System.Xml.Linq;

namespace AnySqlParser;
public sealed class Column: Element {
	public string Name;
	public DataType Type;
	public Expression? Default;
	public bool AutoIncrement;
	public bool PrimaryKey;
	public bool Nullable = true;

	public Column(Location location, string name, DataType type): base(location) {
		Type = type;
		Name = name;
	}
}
