using System.Text;

namespace AnySqlParser;
public static class Etc {
	public static bool IsWordPart(int c) {
		if (char.IsLetterOrDigit((char)c))
			return true;
		return c == '_';
	}

	public static string Unquote(string s) {
		var q = s[0];
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
