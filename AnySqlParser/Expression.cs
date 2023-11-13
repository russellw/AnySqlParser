namespace AnySqlParser
{
    public abstract class Expression : AST
    {
        protected Expression(Location location) : base(location)
        {
        }
    }

    public abstract class AtomicExpression : Expression
    {
        protected AtomicExpression(Location location) : base(location)
        {
        }
    }

    public sealed class StringLiteral : AtomicExpression
    {
        public string Value;

        public StringLiteral(Location location, string value) : base(location)
        {
            Value = value;
        }
    }

    public sealed class Number : AtomicExpression
    {
        public string Value;

        public Number(Location location, string value) : base(location)
        {
            Value = value;
        }
    }

    public sealed class Null : AtomicExpression
    {
        public Null(Location location) : base(location)
        {
        }
    }
}
