namespace AnySqlParser
{
    public abstract class Expression : AST
    {
        protected Expression(Location location) : base(location)
        {
        }
    }

    //arity 0
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

    //arity 1
    public abstract class UnaryExpression : Expression
    {
        public Expression Operand;

        protected UnaryExpression(Location location, Expression operand) : base(location)
        {
            Operand = operand;
        }
    }

    public sealed class BitNot : UnaryExpression
    {
        public BitNot(Location location, Expression operand) : base(location, operand)
        {
        }
    }

    public sealed class Minus : UnaryExpression
    {
        public Minus(Location location, Expression operand) : base(location, operand)
        {
        }
    }
}
