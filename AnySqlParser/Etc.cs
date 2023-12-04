using System.Text;

namespace AnySqlParser;
public static class Etc {
	public static bool IsWordPart(int ch) {
		if (char.IsLetterOrDigit((char)ch))
			return true;
		return ch == '_';
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
