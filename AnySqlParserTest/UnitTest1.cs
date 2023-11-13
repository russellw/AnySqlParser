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
    }
}
