namespace AnySqlParser
{
    public abstract class Expression : AST
    {
        public Expression(Location location) : base(location)
        {
        }
    }

    public sealed class StringLiteral : Expression
    {
        public string value;

        public StringLiteral(Location location, string value) : base(location)
        {
            this.value = value;
        }
    }

    public sealed class Number : Expression
    {
        public string value;

        public Number(Location location, string value) : base(location)
        {
            this.value = value;
        }
    }

    public sealed class Null : Expression
    {
        public Null(Location location) : base(location)
        {
        }
    }
}
