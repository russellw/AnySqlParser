using AnySqlParser;

namespace AnySqlParserTest;
public class UnitTest1 {
	[Fact]
	public void Blank() {
		AssertEmpty(ParseText(""));
		AssertEmpty(ParseText("\t\n"));
	}

	[Fact]
	public void LineComment() {
		AssertEmpty(ParseText("--"));
		AssertEmpty(ParseText("--\n--\n"));

		var e = Assert.Throws<SqlError>(() => ParseText("--\n--\n!"));
		Assert.Matches(".*:3: ", e.Message);
	}

	[Fact]
	public void BlockComment() {
		AssertEmpty(ParseText("/**/"));
		AssertEmpty(ParseText(" /*.*/ "));
		AssertEmpty(ParseText("/**************/"));
		AssertEmpty(ParseText("/*////////////*/"));

		var e = Assert.Throws<SqlError>(() => ParseText("/*/"));
		Assert.Matches(".*:1: ", e.Message);

		e = Assert.Throws<SqlError>(() => ParseText("/*/", "foo", 5));
		Assert.Matches("foo:5: ", e.Message);

		e = Assert.Throws<SqlError>(() => ParseText("\n\n/*/", "foo", 5));
		Assert.Matches("foo:7: ", e.Message);
	}

	void AssertEmpty(Schema schema) {
		Assert.Empty(schema.Tables);
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
		var schema = ParseFile("sql-server-samples/sampleDB1.sql");
		Assert.Equal(2, schema.Tables.Count);
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
		var schema = ParseText("create table t(c int default 1)");
		var a = Default(schema);
		Assert.True(a is Number);

		schema = ParseText("create table t(c int default ~1)");
		a = Default(schema);
		Assert.True(a is UnaryExpression);
		Assert.True(a.Equals(new UnaryExpression(UnaryOp.BitNot, new Number("1"))));

		schema = ParseText("create table t(c int default -1)");
		a = Default(schema);
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

	static Expression Default(Schema schema) {
		foreach (var table in schema.Tables) {
			var column = table.Columns[0];
			return column.Default!;
		}
		throw new Exception(schema.ToString());
	}

	[Fact]
	public void Northwind() {
		var schema = ParseFile("sql-server-samples/instnwnd.sql");
		Assert.Equal(13, schema.Tables.Count);
	}

	[Fact]
	public void NorthwindAzure() {
		var schema = ParseFile("sql-server-samples/instnwnd (Azure SQL Database).sql");
		Assert.Equal(13, schema.Tables.Count);
	}

	[Fact]
	public void InstPubs() {
		var schema = ParseFile("sql-server-samples/instpubs.sql");
		Assert.Equal(11, schema.Tables.Count);
	}

	[Fact]
	public void Sqlite() {
		var schema = ParseFile("sqlite/cities.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("sqlite/movies.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("sqlite/quotes.sql");
		Assert.Single(schema.Tables);
	}

	[Fact]
	public void MySql() {
		var schema = ParseFile("mysql/cities.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("mysql/cities-dump.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("mysql/forward-ref.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("mysql/movies.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("mysql/quotes.sql");
		Assert.Single(schema.Tables);

		schema = ParseFile("mysql-samples/employees.sql");
		Assert.Equal(6, schema.Tables.Count);
	}

	[Fact]
	public void SqlServer() {
		var schema = ParseFile("sql-server/cities.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("sql-server/movies.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("sql-server/quotes.sql");
		Assert.Single(schema.Tables);
	}

	[Fact]
	public void Postgres() {
		var schema = ParseFile("northwind_psql/northwind.sql");
		// The postgres version has an extra table for US states
		Assert.Equal(14, schema.Tables.Count);

		schema = ParseFile("postgres/cities.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("postgres/cities-dump.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("postgres/movies.sql");
		Assert.Equal(2, schema.Tables.Count);

		schema = ParseFile("postgres/quotes.sql");
		Assert.Single(schema.Tables);

		schema = ParseFile("postgres/as.sql");
		Assert.Single(schema.Tables);
	}

	static Schema ParseFile(string file) {
		var schema = new Schema();
		foreach (var _ in Parser.Parse(file, schema)) {
		}
		return schema;
	}

	static Schema ParseText(string sql, string file = "SQL", int line = 1) {
		var schema = new Schema();
		foreach (var _ in Parser.Parse(new StringReader(sql), schema, file, line)) {
		}
		return schema;
	}
}
