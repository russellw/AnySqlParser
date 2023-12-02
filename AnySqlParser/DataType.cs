﻿namespace AnySqlParser;
public record struct DataType {
	public QualifiedName TypeName;
	public int Size = -1;
	public int Scale = -1;

	public DataType(QualifiedName typeName) {
		TypeName = typeName;
	}
}