using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>The small drawn icons of the chat (message, microphone, send, close) and the balloon shape with its tip.</summary>
	internal static class ChatIcons
	{
		public static readonly Color Blue = Color.FromArgb(37, 99, 235), Green = Color.FromArgb(22, 150, 80);

		static Pen Line(Color c, RectangleF r) => new Pen(c, Math.Max(1.6f, r.Width / 9f)) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round, StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };

		/// <summary>A speech balloon: rounded square with a little tail.</summary>
		public static void Message(Graphics g, RectangleF r, Color c)
		{
			using (var pen = Line(c, r))
			{
				var body = new RectangleF(r.X, r.Y + r.Height * 0.04f, r.Width, r.Height * 0.72f);
				using (var path = Theme.RoundedRect(body, r.Width * 0.24f)) g.DrawPath(pen, path);
				using (var b = new SolidBrush(c))
					g.FillPolygon(b, new[] { new PointF(r.X + r.Width * 0.2f, body.Bottom - pen.Width / 2), new PointF(r.X + r.Width * 0.16f, r.Bottom), new PointF(r.X + r.Width * 0.48f, body.Bottom - pen.Width / 2) });
			}
		}

		/// <summary>A microphone: capsule, the holder under it and the stand.</summary>
		public static void Mic(Graphics g, RectangleF r, Color c)
		{
			using (var pen = Line(c, r))
			{
				float cx = r.X + r.Width / 2, cw = r.Width * 0.38f;
				var capsule = new RectangleF(cx - cw / 2, r.Y, cw, r.Height * 0.6f);
				using (var path = Theme.RoundedRect(capsule, cw / 2)) g.DrawPath(pen, path);
				var arc = new RectangleF(cx - r.Width * 0.34f, r.Y + r.Height * 0.18f, r.Width * 0.68f, r.Height * 0.58f);
				g.DrawArc(pen, arc, 0, 180);
				g.DrawLine(pen, cx, arc.Bottom, cx, r.Bottom);
				g.DrawLine(pen, cx - r.Width * 0.18f, r.Bottom, cx + r.Width * 0.18f, r.Bottom);
			}
		}

		/// <summary>A paper plane pointing right.</summary>
		public static void Send(Graphics g, RectangleF r, Color c)
		{
			using (var pen = Line(c, r))
			{
				var pts = new[] { new PointF(r.X, r.Y + r.Height * 0.05f), new PointF(r.Right, r.Y + r.Height / 2), new PointF(r.X, r.Bottom - r.Height * 0.05f), new PointF(r.X + r.Width * 0.2f, r.Y + r.Height / 2) };
				g.DrawPolygon(pen, pts);
				g.DrawLine(pen, pts[3], new PointF(r.X + r.Width * 0.55f, r.Y + r.Height / 2));
			}
		}

		/// <summary>A new conversation: a speech balloon with a plus.</summary>
		public static void NewChat(Graphics g, RectangleF r, Color c)
		{
			Message(g, r, c);
			using (var pen = Line(c, r))
			{
				float cx = r.X + r.Width / 2, cy = r.Y + r.Height * 0.4f, s = r.Width * 0.17f;
				g.DrawLine(pen, cx - s, cy, cx + s, cy);
				g.DrawLine(pen, cx, cy - s, cx, cy + s);
			}
		}

		public static void Close(Graphics g, RectangleF r, Color c)
		{
			using (var pen = Line(c, r)) { g.DrawLine(pen, r.Left, r.Top, r.Right, r.Bottom); g.DrawLine(pen, r.Right, r.Top, r.Left, r.Bottom); }
		}

		/// <summary>The shape of a balloon window: a rounded card of w x h and a small tip under its middle.</summary>
		public static System.Drawing.Drawing2D.GraphicsPath Balloon(int w, int h, int tip, int radius, float tipX)
		{
			var path = Theme.RoundedRect(new RectangleF(0, 0, w, h), radius);
			path.AddPolygon(new[] { new PointF(tipX - tip - 1, h - 2), new PointF(tipX, h + tip), new PointF(tipX + tip + 1, h - 2) });
			path.FillMode = System.Drawing.Drawing2D.FillMode.Winding;
			return path;
		}

		/// <summary>The border of <see cref="Balloon"/>: the card and the two sides of the tip, open where they meet.</summary>
		public static void DrawBalloonOutline(Graphics g, int w, int h, int tip, int radius, Color border, Color fill, float tipX)
		{
			using (var path = Theme.RoundedRect(new RectangleF(0.5f, 0.5f, w - 1, h - 1.5f), radius))
			using (var p = new Pen(border)) g.DrawPath(p, path);
			using (var b = new SolidBrush(fill))
				g.FillPolygon(b, new[] { new PointF(tipX - tip, h - 2), new PointF(tipX, h + tip - 1), new PointF(tipX + tip, h - 2) });
			using (var p = new Pen(border))
			{
				g.DrawLine(p, tipX - tip, h - 1, tipX, h + tip - 1);
				g.DrawLine(p, tipX, h + tip - 1, tipX + tip, h - 1);
			}
		}
	}
}
