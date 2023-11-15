using AnySqlParser;

namespace AnySqlParserTest
{
    public class UnitTest1
    {
        [Fact]
        public void Blank()
        {
            Assert.Empty(Parser.ParseText(""));
            Assert.Empty(Parser.ParseText("\t\n"));
        }

        [Fact]
        public void LineComment()
        {
            Assert.Empty(Parser.ParseText("--"));
            Assert.Empty(Parser.ParseText("--\n--\n"));
        }

        [Fact]
        public void BlockComment()
        {
            Assert.Empty(Parser.ParseText("/**/"));
            Assert.Empty(Parser.ParseText(" /*.*/ "));
            Assert.Empty(Parser.ParseText("/**************/"));
            Assert.Empty(Parser.ParseText("/*////////////*/"));

            var e = Assert.Throws<FormatException>(() => Parser.ParseText("/*/"));
            Assert.Matches(".*:1: ", e.Message);

            e = Assert.Throws<FormatException>(() => Parser.ParseText("/*/", "foo", 5));
            Assert.Matches("foo:5: ", e.Message);

            e = Assert.Throws<FormatException>(() => Parser.ParseText("\n\n/*/", "foo", 5));
            Assert.Matches("foo:7: ", e.Message);
        }

        [Fact]
        public void StrayCharacter()
        {
            Assert.Throws<FormatException>(() => Parser.ParseText("!"));
            Assert.Throws<FormatException>(() => Parser.ParseText("|"));
        }

        [Fact]
        public void StringLiteral()
        {
            Assert.Throws<FormatException>(() => Parser.ParseText("'"));
        }

        [Fact]
        public void QuotedName()
        {
            Assert.Throws<FormatException>(() => Parser.ParseText("\"..."));
            Assert.Throws<FormatException>(() => Parser.ParseText("`"));
            Assert.Throws<FormatException>(() => Parser.ParseText("["));
        }

        [Fact]
        public void SampleDB1()
        {
            var statements = Parser.ParseFile("sql-server-samples/sampleDB1.sql");
            Assert.True(statements[0] is Table);
        }

        [Fact]
        public void Select()
        {
            var statements = Parser.ParseText("select 1");
            var a = ((Select)statements[0]).SelectList[0];
            Assert.True(a is Number);

            statements = Parser.ParseText("select ~1");
            a = ((Select)statements[0]).SelectList[0];
            Assert.True(a is UnaryExpression);

            statements = Parser.ParseText("select -1");
            a = ((Select)statements[0]).SelectList[0];
            Assert.True(a is UnaryExpression);

            statements = Parser.ParseText("select exists(select 1)");
        }

        [Fact]
        public void Northwind()
        {
            //var statements = Parser.ParseFile("sql-server-samples/instnwnd.sql");
            //Assert.True(statements.Count > 0);
        }
    }
}
