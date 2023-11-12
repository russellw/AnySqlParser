using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnySqlParser
{
    public class ColumnDefinition : AST
    {
        public readonly string name;

        public string? typeSchemaName;
        public string typeName="";
        public int precision = -1;
        public int scale = -1;

        public ColumnDefinition(Location location,string name) : base(location)
        {
            this.name = name;
        }
    }

    public class Statement : AST
    {
        public Statement(Location location) : base(location)
        {
        }
    }

    public class CreateTable : Statement
    {
        public string? databaseName;
        public string? schemaName ;
        public string tableName;
        public List<ColumnDefinition>columnDefinitions = new();

        public CreateTable(Location location,string tableName) : base(location)
        {
            this.tableName = tableName;
        }
    }
}
