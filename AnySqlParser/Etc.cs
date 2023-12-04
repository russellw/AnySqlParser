namespace AnySqlParser;
public static class Etc {
	public static bool IsWordPart(int ch) {
		if (char.IsLetterOrDigit((char)ch))
			return true;
		return ch == '_';
	}
}
