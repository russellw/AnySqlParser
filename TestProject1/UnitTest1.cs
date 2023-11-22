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
	}

	[Fact]
	public void BlockComment() {
		Assert.Empty(ParseText("/**/"));
		Assert.Empty(ParseText(" /*.*/ "));
		Assert.Empty(ParseText("/**************/"));
		Assert.Empty(ParseText("/*////////////*/"));

		var e = Assert.Throws<FormatException>(() => ParseText("/*/"));
		Assert.Matches(".*:1: ", e.Message);

		e = Assert.Throws<FormatException>(() => ParseText("/*/", "foo", 5));
		Assert.Matches("foo:5: ", e.Message);

		e = Assert.Throws<FormatException>(() => ParseText("\n\n/*/", "foo", 5));
		Assert.Matches("foo:7: ", e.Message);
	}

	[Fact]
	public void StrayCharacter() {
		Assert.Throws<FormatException>(() => ParseText("!"));
		Assert.Throws<FormatException>(() => ParseText("|"));
	}

	[Fact]
	public void StringLiteral() {
		Assert.Throws<FormatException>(() => ParseText("'"));
	}

	[Fact]
	public void QuotedName() {
		Assert.Throws<FormatException>(() => ParseText("\"..."));
		Assert.Throws<FormatException>(() => ParseText("`"));
		Assert.Throws<FormatException>(() => ParseText("["));
	}

	[Fact]
	public void SampleDB1() {
		var statements = Parser.ParseFile("sql-server-samples/sampleDB1.sql");
		Assert.True(statements[0] is Table);
	}

	[Fact]
	public void Eq() {
		Expression a = new StringLiteral(new Location("", 0), "x");
		Expression b = new StringLiteral(new Location("", 0), "y");
		Assert.NotEqual(a, b);
		Assert.False(a.Equals(b));
		Assert.False(a.Eq(b));

		a = new StringLiteral(new Location("", 0), "x");
		b = new StringLiteral(new Location("", 0), "x");
		Assert.NotEqual(a, b);
		Assert.False(a.Equals(b));
		Assert.True(a.Eq(b));

		var L = new Location("", 0);
		a = new BinaryExpression(L, BinaryOp.Add, new StringLiteral(L, "x"), new StringLiteral(L, "y"));
		b = new BinaryExpression(L, BinaryOp.Add, new StringLiteral(L, "x"), new StringLiteral(L, "y"));
		Assert.True(a.Eq(b));
	}

	[Fact]
	public void Select() {
		var statements = ParseText("select 1");
		var a = Selected(statements);
		Assert.True(a is Number);

		statements = ParseText("select ~1");
		a = Selected(statements);
		Assert.True(a is UnaryExpression);
		var L = new Location("", 0);
		Assert.True(a.Eq(new UnaryExpression(L, UnaryOp.BitNot, new Number(L, "1"))));

		statements = ParseText("select -1");
		a = Selected(statements);
		Assert.True(a is UnaryExpression);
		Assert.True(a.Eq(new UnaryExpression(L, UnaryOp.Minus, new Number(L, "1"))));

		a = Selected(ParseText("select 1*2"));
		Expression b;
		b = new BinaryExpression(L, BinaryOp.Multiply, new Number(L, "1"), new Number(L, "2"));
		Assert.True(a.Eq(b));

		a = Selected(ParseText("select 1*2*3"));
		b = new BinaryExpression(L,
								 BinaryOp.Multiply,
								 new BinaryExpression(L, BinaryOp.Multiply, new Number(L, "1"), new Number(L, "2")),
								 new Number(L, "3"));
		Assert.True(a.Eq(b));

		a = Selected(ParseText("select (1*2)*3"));
		b = new BinaryExpression(L,
								 BinaryOp.Multiply,
								 new BinaryExpression(L, BinaryOp.Multiply, new Number(L, "1"), new Number(L, "2")),
								 new Number(L, "3"));
		Assert.True(a.Eq(b));

		a = Selected(ParseText("select 1*(2*3)"));
		b = new BinaryExpression(L,
								 BinaryOp.Multiply,
								 new Number(L, "1"),
								 new BinaryExpression(L, BinaryOp.Multiply, new Number(L, "2"), new Number(L, "3")));
		Assert.True(a.Eq(b));

		a = Selected(ParseText("select 1*2+3"));
		b = new BinaryExpression(L,
								 BinaryOp.Add,
								 new BinaryExpression(L, BinaryOp.Multiply, new Number(L, "1"), new Number(L, "2")),
								 new Number(L, "3"));
		Assert.True(a.Eq(b));

		a = Selected(ParseText("select 1+2*3"));
		b = new BinaryExpression(L,
								 BinaryOp.Add,
								 new Number(L, "1"),
								 new BinaryExpression(L, BinaryOp.Multiply, new Number(L, "2"), new Number(L, "3")));
		Assert.True(a.Eq(b));

		a = Selected(ParseText("select 1=2*3"));
		b = new BinaryExpression(L,
								 BinaryOp.Equal,
								 new Number(L, "1"),
								 new BinaryExpression(L, BinaryOp.Multiply, new Number(L, "2"), new Number(L, "3")));
		Assert.True(a.Eq(b));
	}

	static Expression Selected(List<Statement> statements) {
		var select = (Select)statements[0];
		var querySpecification = (QuerySpecification)select.QueryExpression;
		return querySpecification.SelectList[0].Expression;
	}

	[Fact]
	public void Northwind() {
		var statements = Parser.ParseFile("sql-server-samples/instnwnd.sql");
		// Assert.True(statements.Count > 0);
	}

	static List<Statement> ParseText(string sql, string file = "SQL", int line = 1) {
		return Parser.ParseText(new StringReader(sql), file, line);
	}
}
}
