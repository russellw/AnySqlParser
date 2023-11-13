namespace AnySqlParser
{
    public sealed class ColumnDefinition : AST
    {
        public string name;

        //data type
        public string? typeSchemaName;
        public string typeName = null!;
        public int size = -1;
        public int scale = -1;

        //constraints
        public bool nullable = true;
        public bool primaryKey;

        //etc
        public bool filestream;
        public bool sparse;
        public bool forReplication = true;
        public bool rowguidcol;

        public ColumnDefinition(Location location, string name) : base(location)
        {
            this.name = name;
        }
    }

    public abstract class Statement : AST
    {
        public Statement(Location location) : base(location)
        {
        }
    }

    public sealed class Insert : Statement
    {
        public string tableName;
        public List<string> columns = new();
        public List<Expression> values = new();

        public Insert(Location location, string tableName) : base(location)
        {
            this.tableName = tableName;
        }
    }

    public sealed class StartTransaction : Statement
    {
        public StartTransaction(Location location) : base(location)
        {
        }
    }

    public sealed class CreateTable : Statement
    {
        public string? databaseName;
        public string? schemaName;
        public string tableName;
        public List<ColumnDefinition> columnDefinitions = new();

        public CreateTable(Location location, string tableName) : base(location)
        {
            this.tableName = tableName;
        }
    }
}
