using System.Xml.Linq;

namespace AnySqlParser;
public sealed class Column: Element {
	public bool AutoIncrement;
	public Expression? Default;
	public string Name;
	public bool Nullable = true;
	public bool PrimaryKey;
	public DataType Type;

	public Column(Location location, string name, DataType type): base(location) {
		Type = type;
		Name = name;
	}
}
