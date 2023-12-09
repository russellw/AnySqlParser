﻿namespace AnySqlParser;
public sealed class Call: Expression {
	public QualifiedName Function;
	public List<Expression> Arguments = new();

	public Call(QualifiedName function) {
		Function = function;
	}

	public override bool Equals(object? obj) {
		return obj is Call call && EqualityComparer<QualifiedName>.Default.Equals(Function, call.Function) &&
			   Arguments.SequenceEqual(call.Arguments);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Function, Arguments);
	}
}
