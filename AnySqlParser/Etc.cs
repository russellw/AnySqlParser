using System.Diagnostics;
using System.Text;

namespace AnySqlParser;
public static class Etc {
	public static bool IsWordPart(int c) {
		if (char.IsLetterOrDigit((char)c))
			return true;
		return c == '_';
	}

	public static string Unquote(string s) {
		Debug.Assert(s[0] == s[^1]);
		return Unquote(s, s[0]);
	}

	public static string Unquote(string s, char q) {
		var sb = new StringBuilder();
		for (int i = 1; i < s.Length - 1;) {
			var c = s[i++];
			if (c == q && s[i] == q)
				i++;
			sb.Append(c);
		}
		return sb.ToString();
	}
}
