using AnySqlParser;

namespace AnySqlParserTest {
public class UnitTest1 {
	[Fact]
	public void Blank() {
		Assert.Empty(ParseText(""));
		Assert.Empty(ParseText("\t\n"));
	}

	[Fact]
	public void LineComment() {
		Assert.Empty(ParseText("--"));
		Assert.Empty(ParseText("--\n--\n"));

		var e = Assert.Throws<SqlError>(() => ParseText("--\n--\n!"));
		Assert.Matches(".*:3: ", e.Message);
	}

	[Fact]
	public void BlockComment() {
		Assert.Empty(ParseText("/**/"));
		Assert.Empty(ParseText(" /*.*/ "));
		Assert.Empty(ParseText("/**************/"));
		Assert.Empty(ParseText("/*////////////*/"));

		var e = Assert.Throws<SqlError>(() => ParseText("/*/"));
		Assert.Matches(".*:1: ", e.Message);

		e = Assert.Throws<SqlError>(() => ParseText("/*/", "foo", 5));
		Assert.Matches("foo:5: ", e.Message);

		e = Assert.Throws<SqlError>(() => ParseText("\n\n/*/", "foo", 5));
		Assert.Matches("foo:7: ", e.Message);
	}

	[Fact]
	public void StrayCharacter() {
		Assert.Throws<SqlError>(() => ParseText("!"));
		Assert.Throws<SqlError>(() => ParseText("|"));
	}

	[Fact]
	public void StringLiteral() {
		Assert.Throws<SqlError>(() => ParseText("'"));
	}

	[Fact]
	public void QuotedName() {
		Assert.Throws<SqlError>(() => ParseText("\"..."));
		Assert.Throws<SqlError>(() => ParseText("`"));
		Assert.Throws<SqlError>(() => ParseText("["));
	}

	[Fact]
	public void SampleDB1() {
		var statements = ParseFile("sql-server-samples/sampleDB1.sql");
		Assert.True(statements[1] is Table);
	}

	[Fact]
	public void Eq() {
		Expression a = new StringLiteral("x");
		Expression b = new StringLiteral("y");
		Assert.NotEqual(a, b);

		a = new StringLiteral("x");
		b = new StringLiteral("x");
		Assert.Equal(a, b);

		a = new BinaryExpression(BinaryOp.Add, new StringLiteral("x"), new StringLiteral("y"));
		b = new BinaryExpression(BinaryOp.Add, new StringLiteral("x"), new StringLiteral("y"));
		Assert.Equal(a, b);
	}

	[Fact]
	public void Select() {
		var statements = ParseText("create table t(c int default 1)");
		var a = Default(statements);
		Assert.True(a is Number);

		statements = ParseText("create table t(c int default ~1)");
		a = Default(statements);
		Assert.True(a is UnaryExpression);
		Assert.True(a.Equals(new UnaryExpression(UnaryOp.BitNot, new Number("1"))));

		statements = ParseText("create table t(c int default -1)");
		a = Default(statements);
		Assert.True(a is UnaryExpression);
		Assert.True(a.Equals(new UnaryExpression(UnaryOp.Minus, new Number("1"))));

		a = Default(ParseText("create table t(c int default 1*2)"));
		Expression b;
		b = new BinaryExpression(BinaryOp.Multiply, new Number("1"), new Number("2"));
		Assert.True(a.Equals(b));

		a = Default(ParseText("create table t(c int default 1*2*3)"));
		b = new BinaryExpression(
			BinaryOp.Multiply, new BinaryExpression(BinaryOp.Multiply, new Number("1"), new Number("2")), new Number("3"));
		Assert.True(a.Equals(b));

		a = Default(ParseText("create table t(c int default (1*2)*3)"));
		b = new BinaryExpression(
			BinaryOp.Multiply, new BinaryExpression(BinaryOp.Multiply, new Number("1"), new Number("2")), new Number("3"));
		Assert.True(a.Equals(b));

		a = Default(ParseText("create table t(c int default 1*(2*3))"));
		b = new BinaryExpression(
			BinaryOp.Multiply, new Number("1"), new BinaryExpression(BinaryOp.Multiply, new Number("2"), new Number("3")));
		Assert.True(a.Equals(b));

		a = Default(ParseText("create table t(c int default 1*2+3)"));
		b = new BinaryExpression(
			BinaryOp.Add, new BinaryExpression(BinaryOp.Multiply, new Number("1"), new Number("2")), new Number("3"));
		Assert.True(a.Equals(b));

		a = Default(ParseText("create table t(c int default 1+2*3)"));
		b = new BinaryExpression(
			BinaryOp.Add, new Number("1"), new BinaryExpression(BinaryOp.Multiply, new Number("2"), new Number("3")));
		Assert.True(a.Equals(b));

		a = Default(ParseText("create table t(c int default 1=2*3)"));
		b = new BinaryExpression(
			BinaryOp.Equal, new Number("1"), new BinaryExpression(BinaryOp.Multiply, new Number("2"), new Number("3")));
		Assert.True(a.Equals(b));
	}

	static Expression Default(List<Statement> statements) {
		foreach (var a in statements)
			if (a is Table table) {
				var column = table.Columns[0];
				return column.Default!;
			}
		throw new Exception(statements.ToString());
	}

	[Fact]
	public void Northwind() {
		var statements = ParseFile("sql-server-samples/instnwnd.sql");
		Assert.True(statements.Count > 0);
	}

	[Fact]
	public void NorthwindAzure() {
		var statements = ParseFile("sql-server-samples/instnwnd (Azure SQL Database).sql");
		Assert.True(statements.Count > 0);
	}

	[Fact]
	public void InstPubs() {
		var statements = ParseFile("sql-server-samples/instpubs.sql");
		Assert.True(statements.Count > 0);
	}

	static List<Statement> ParseFile(string file) {
		return new List<Statement>(Parser.Parse(file));
	}

	static List<Statement> ParseText(string sql, string file = "SQL", int line = 1) {
		return new List<Statement>(Parser.Parse(new StringReader(sql), file, line));
	}
}
}
