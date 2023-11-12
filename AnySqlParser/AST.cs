﻿namespace AnySqlParser
{
    public readonly record struct Location(string File, int Line);

    public class AST
    {
        public readonly Location location;

        public AST(Location location)
        {
            this.location = location;
        }
    }
}