using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnySqlParser
{
    public class Statement : AST
    {
        public Statement(Location location) : base(location)
        {
        }
    }

    public class CreateTable : Statement
    {
        public string databaseName="";
        public string schemaName="";
        public string tableName="";

        public CreateTable(Location location) : base(location)
        {
        }
    }
}
