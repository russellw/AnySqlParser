namespace AnySqlParser
{
    public abstract class Expression : AST
    {
        protected Expression(Location location) : base(location)
        {
        }

        //The omission of an override for Equals is intentional
        //in most cases, syntax trees have reference semantics
        //equality comparison by value is useful only in unusual situations
        //and should not be the default
        public virtual bool Eq(AST b)
        {
            return this == b;
        }
    }

    public sealed class Call : Expression
    {
        public QualifiedName Function;
        public List<Expression> Arguments = new();

        public Call(Location location, QualifiedName function) : base(location)
        {
            Function = function;
        }
    }

    public sealed class Exists : Expression
    {
        public AST Query;

        public Exists(Location location, AST query) : base(location)
        {
            Query = query;
        }
    }

    public sealed class StringLiteral : Expression
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

    public sealed class QualifiedName : Expression
    {
        public List<string> Names = new();

        public QualifiedName(Location location) : base(location)
        {
        }

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

    public sealed class Number : Expression
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

    public sealed class Null : Expression
    {
        public Null(Location location) : base(location)
        {
        }

        public override bool Eq(AST b)
        {
            return b is Null;
        }
    }
}
