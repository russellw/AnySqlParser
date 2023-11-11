using AnySqlParser;

namespace AnySqlParserTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Assert.Empty(Parser.Parse(""));
        }
    }
}