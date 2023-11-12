using AnySqlParser;

namespace AnySqlParserTest
{
    public class UnitTest1
    {
        [Fact]
        public void Blank()
        {
            Assert.Empty(Parser.Parse(""));
            Assert.Empty(Parser.Parse("\t\n"));
        }

        [Fact]
        public void LineComment()
        {
            Assert.Empty(Parser.Parse("--"));
            Assert.Empty(Parser.Parse("--\n--\n"));
        }

        [Fact]
        public void BlockComment()
        {
            Assert.Empty(Parser.Parse("/**/"));
            Assert.Empty(Parser.Parse(" /*.*/ "));
            Assert.Empty(Parser.Parse("/**************/"));
            Assert.Empty(Parser.Parse("/*////////////*/"));

            var e = Assert.Throws<FormatException>(() => Parser.Parse("/*/"));
            Assert.Matches(".*:1: ", e.Message);

            e = Assert.Throws<FormatException>(() => Parser.Parse("/*/", "foo", 5));
            Assert.Matches("foo:5: ", e.Message);

            e = Assert.Throws<FormatException>(() => Parser.Parse("\n\n/*/", "foo", 5));
            Assert.Matches("foo:7: ", e.Message);
        }

        [Fact]
        public void StrayCharacter()
        {
            Assert.Throws<FormatException>(() => Parser.Parse("!"));
            Assert.Throws<FormatException>(() => Parser.Parse("|"));
        }

        [Fact]
        public void StringLiteral()
        {
            Assert.Throws<FormatException>(() => Parser.Parse("'"));
        }

        [Fact]
        public void QuotedName()
        {
            Assert.Throws<FormatException>(() => Parser.Parse("\"..."));
            Assert.Throws<FormatException>(() => Parser.Parse("`"));
            Assert.Throws<FormatException>(() => Parser.Parse("["));
        }
    }
}