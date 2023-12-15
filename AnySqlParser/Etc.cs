using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AnySqlParser;
public static class Etc {
	public static bool IsWordPart(int c) {
		if (char.IsLetterOrDigit((char)c))
			return true;
		return '_' == c;
	}

	public static void Print(object a, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) {
		Console.WriteLine($"{file}:{line}: {a}");
	}

	public static string Quote(string s, char q) {
		var sb = new StringBuilder();
		sb.Append(q);
		foreach (char c in s) {
			sb.Append(c);
			if (c == q)
				sb.Append(c);
		}
		sb.Append(q);
		return sb.ToString();
	}

	public static string Unquote(string s) {
		Debug.Assert(s[0] == s[^1]);
		return Unquote(s, s[0]);
	}

	public static string Unquote(string s, char q) {
		var sb = new StringBuilder();
		for (int i = 1; i < s.Length - 1;) {
			var c = s[i++];
			if (c == q && q == s[i])
				i++;
			sb.Append(c);
		}
		return sb.ToString();
	}
}
