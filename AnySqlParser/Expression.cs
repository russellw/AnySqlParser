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

        public override bool Eq(AST b)
        {
            if (b is Exists b1)
                return Operand.Eq(b1.Operand);
            return false;
        }
    }

    public sealed class StringLiteral : AtomicExpression
    {
        public string Value;

        public StringLiteral(Location location, string value) : base(location)
        {
            Value = value;
        }

        public override bool Eq(AST b)
        {
            if (b is StringLiteral b1)
                return Value == b1.Value;
            return false;
        }
    }

    public sealed class QualifiedName : AtomicExpression
    {
        public List<string> Names = new();

        public QualifiedName(Location location, string name) : base(location)
        {
            Names.Add(name);
        }

        public override bool Eq(AST b)
        {
            if (b is QualifiedName b1)
                return Names == b1.Names;
            return false;
        }
    }

    public sealed class Number : AtomicExpression
    {
        public string Value;

        public Number(Location location, string value) : base(location)
        {
            Value = value;
        }

        public override bool Eq(AST b)
        {
            if (b is Number b1)
                return Value == b1.Value;
            return false;
        }
    }

    public sealed class Null : AtomicExpression
    {
        public Null(Location location) : base(location)
        {
        }

        public override bool Eq(AST b)
        {
            return b is Null;
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

        public override bool Eq(AST b)
        {
            if (b is UnaryExpression b1)
                return Op == b1.Op && Operand.Eq(b1.Operand);
            return false;
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

        public override bool Eq(AST b)
        {
            if (b is BinaryExpression b1)
                return Op == b1.Op && Left.Eq(b1.Left) && Right.Eq(b1.Right);
            return false;
        }
    }

    //arity N
    public sealed class Call : Expression
    {
        public QualifiedName Function;
        public List<Expression> Arguments = new();

        public Call(Location location, QualifiedName function) : base(location)
        {
            Function = function;
        }
    }
}
