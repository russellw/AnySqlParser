namespace AnySqlParser
{
    public abstract class Expression : AST
    {
        protected Expression(Location location) : base(location)
        {
        }
    }

    //arity 0
    //or expressions whose operands are not expressions
    public abstract class AtomicExpression : Expression
    {
        protected AtomicExpression(Location location) : base(location)
        {
        }
    }

    public sealed class Exists : AtomicExpression
    {
        public AST Operand;

        public Exists(Location location, AST operand) : base(location)
        {
            Operand = operand;
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
    public enum UnaryOp
    {
        Not,
        BitNot,
        Minus,
        Exists,
    }

    public sealed class UnaryExpression : Expression
    {
        public UnaryOp Op;
        public Expression Operand;

        public UnaryExpression(Location location, UnaryOp op, Expression operand) : base(location)
        {
            Op = op;
            Operand = operand;
        }
    }

    //arity 2
    public enum BinaryOp
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Remainder,
        Equal,
        NotEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,
        And,
        Or,
        BitAnd,
        BitOr,
        BitXor,
        Concat,
    }

    public sealed class BinaryExpression : Expression
    {
        public BinaryOp Op;
        public Expression Left, Right;

        public BinaryExpression(Location location, BinaryOp op, Expression left, Expression right) : base(location)
        {
            Op = op;
            Left = left;
            Right = right;
        }
    }
}
