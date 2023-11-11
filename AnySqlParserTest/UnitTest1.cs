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
        public void LineComments()
        {
            Assert.Empty(Parser.Parse("--"));
            Assert.Empty(Parser.Parse("--\n--\n"));
        }

        [Fact]
        public void BlockComments()
        {
            Assert.Empty(Parser.Parse("/**/"));
            Assert.Empty(Parser.Parse(" /*.*/ "));
            Assert.Throws<FormatException>(()=> Parser.Parse("/*/"));
            Assert.Empty(Parser.Parse("/**************/"));
            Assert.Empty(Parser.Parse("/*////////////*/"));
        }
    }
}