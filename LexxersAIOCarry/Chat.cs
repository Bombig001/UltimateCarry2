using LeagueSharp;

namespace LexxersAIOCarry
{
	public static class Chat
	{
		public const string Basiccolor = HtmlColor.Yellow;

		internal static void Print(string message, string color = Basiccolor)
		{
			Game.PrintChat("<font color='{0}'>{1}</font>", color, message);
		}
	}
}
