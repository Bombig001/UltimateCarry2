using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace UltimateCarry
{
	class Overlay
	{
		public Render.Sprite HUD;
		public Overlay()
		{
			if(Drawing.Width != 1920 || Drawing.Height != 1080 || Utility.Map.GetMap() != Utility.Map.MapType.SummonersRift)
				return;
			Program.Menu.AddSubMenu(new Menu("HUD", "HUD"));
			Program.Menu.SubMenu("HUD").AddItem(new MenuItem("showHud", "Show HUD").SetValue(true));

			HUD = new Render.Sprite(Properties.Resources.Overlay2, new Vector2(1, 1));
			HUD.Add();

			Drawing.OnDraw += Drawing_OnDraw;
		}

		private void Drawing_OnDraw(EventArgs args)
		{
			if (Program.Menu.Item("showHud").GetValue<bool>())
			{
				HUD.Visible = true;
				Drawing.DrawLine(new Vector2(1275, 860), new Vector2(1610, 860), 200, Color.Black);
			}
			else
			{
				HUD.Visible = false;
			}
		}
	}
}
