using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class LuaPictureBox<TTable> : PictureBox
	{
		private readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();

		private readonly Dictionary<Color, SolidBrush> _solidBrushes = new Dictionary<Color, SolidBrush>();
		private readonly Dictionary<Color, Pen> _pens = new Dictionary<Color, Pen>();

		internal ILuaTableHelper<TTable> TableHelper { private get; set; }

		private SolidBrush GetBrush(Color color)
		{
			if (!_solidBrushes.TryGetValue(color, out var b))
			{
				b = new SolidBrush(color);
				_solidBrushes[color] = b;
			}

			return b;
		}

		private Pen GetPen(Color color)
		{
			if (!_pens.TryGetValue(color, out var p))
			{
				p = new Pen(color);
				_pens[color] = p;
			}

			return p;
		}

		private Color _defaultForeground = Color.Black;
		private Color? _defaultBackground;
		private Color? _defaultTextBackground = Color.FromArgb(128, 0, 0, 0);

		public LuaPictureBox()
		{
			Image = new Bitmap(Width, Height);
		}
		
		public void LuaResize(int width, int height)
		{
			Width = width;
			Height = height;
			Image = new Bitmap(width, height);
		}

		public void Clear(Color color)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.Clear(color);
		}

		public void SetDefaultForegroundColor(Color color)
		{
			_defaultForeground = color;
		}

		public void SetDefaultBackgroundColor(Color color)
		{
			_defaultBackground = color;
		}

		public void SetDefaultTextBackground(Color color)
		{
			_defaultTextBackground = color;
		}

		public void DrawBezier(TTable points, Color color)
		{
			var pointsArr = new Point[4];

			var i = 0;
			foreach (var point in TableHelper.EnumerateValues<TTable>(points)
				.Select(table => TableHelper.EnumerateValues<double>(table).ToList()))
			{
				pointsArr[i] = new Point((int) point[0], (int) point[1]);
				i++;
				if (i >= 4)
				{
					break;
				}
			}

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawBezier(GetPen(color), pointsArr[0], pointsArr[1], pointsArr[2], pointsArr[3]);
		}

		public void DrawBox(int x, int y, int x2, int y2, Color? line = null, Color? background = null)
		{
			if (x < x2)
			{
				x2 = Math.Abs(x - x2);
			}
			else
			{
				x2 = x - x2;
				x -= x2;
			}

			if (y < y2)
			{
				y2 = Math.Abs(y - y2);
			}
			else
			{
				y2 = y - y2;
				y -= y2;
			}

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawRectangle(GetPen(line ?? _defaultForeground), x, y, x2, y2);

			var bg = background ?? _defaultBackground;
			if (bg.HasValue)
			{
				boxBackground = Graphics.FromImage(Image);
				boxBackground.FillRectangle(GetBrush(bg.Value), x + 1, y + 1, x2 - 1, y2 - 1);
			}
		}

		public void DrawEllipse(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			var bg = background ?? _defaultBackground;
			var boxBackground = Graphics.FromImage(Image);
			if (bg.HasValue)
			{
				var brush = GetBrush(bg.Value);
				boxBackground.FillEllipse(brush, x, y, width, height);
				boxBackground = Graphics.FromImage(Image);
			}
			
			boxBackground.DrawEllipse(GetPen(line ?? _defaultForeground), x, y, width, height);
		}

		public void DrawIcon(string path, int x, int y, int? width = null, int? height = null)
		{
			Icon icon;
			if (width.HasValue && height.HasValue)
			{
				icon = new Icon(path, width.Value, height.Value);
			}
			else
			{
				icon = new Icon(path);
			}

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawIcon(icon, x, y);
		}

		public void DrawImage(string path, int x, int y, int? width = null, int? height = null, bool cache = true)
		{
			Image img;
			if (_imageCache.ContainsKey(path))
			{
				img = _imageCache[path];
			}
			else
			{
				img = Image.FromFile(path);
				if (cache)
				{
					_imageCache.Add(path, img);
				}
			}

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawImage(img, x, y, width ?? img.Width, height ?? img.Height);
		}

		public void ClearImageCache()
		{
			foreach (var image in _imageCache)
			{
				image.Value.Dispose();
			}

			_imageCache.Clear();
		}

		public void DrawImageRegion(string path, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int destX, int destY, int? destWidth = null, int? destHeight = null)
		{
			Image img;
			if (_imageCache.ContainsKey(path))
			{
				img = _imageCache[path];
			}
			else
			{
				img = Image.FromFile(path);
				_imageCache.Add(path, img);
			}

			var destRect = new Rectangle(destX, destY, destWidth ?? sourceWidth, destHeight ?? sourceHeight);

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawImage(img, destRect, sourceX, sourceY, sourceWidth, sourceHeight, GraphicsUnit.Pixel);
		}

		public void DrawLine(int x1, int y1, int x2, int y2, Color? color = null)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawLine(GetPen(color ?? _defaultForeground), x1, y1, x2, y2);
		}

		public void DrawAxis(int x, int y, int size, Color? color = null)
		{
			DrawLine(x + size, y, x - size, y, color);
			DrawLine(x, y + size, x, y - size, color);
		}

		public void DrawArc(int x, int y, int width, int height, int startAngle, int sweepAngle, Color? line = null)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawArc(GetPen(line ?? _defaultForeground), x, y, width, height, startAngle, sweepAngle);
		}

		public void DrawPie(
			int x,
			int y,
			int width,
			int height,
			int startAngle,
			int sweepAngle,
			Color? line = null,
			Color? background = null)
		{
			var bg = background ?? _defaultBackground;
			var boxBackground = Graphics.FromImage(Image);
			if (bg.HasValue)
			{
				var brush = GetBrush(bg.Value);
				boxBackground.FillPie(brush, x, y, width, height, startAngle, sweepAngle);
				boxBackground = Graphics.FromImage(Image);
			}
			
			boxBackground.DrawPie(GetPen(line ?? _defaultForeground), x + 1, y + 1, width - 1, height - 1, startAngle, sweepAngle);
		}

		public void DrawPixel(int x, int y, Color? color = null)
		{
			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawLine(GetPen(color ?? _defaultForeground), x, y, x + 0.1F, y);
		}

		public void DrawPolygon(TTable points, int? x = null, int? y = null, Color? line = null, Color? background = null)
		{
			var pointsList = TableHelper.EnumerateValues<TTable>(points)
				.Select(table => TableHelper.EnumerateValues<double>(table).ToList()).ToList();
			var pointsArr = new Point[pointsList.Count];
			var i = 0;
			foreach (var point in pointsList)
			{
				pointsArr[i] = new Point((int) point[0] + x ?? 0, (int) point[1] + y ?? 0);
				i++;
			}

			var boxBackground = Graphics.FromImage(Image);
			boxBackground.DrawPolygon(GetPen(line ?? _defaultForeground), pointsArr);
			var bg = background ?? _defaultBackground;
			if (bg.HasValue)
			{
				boxBackground = Graphics.FromImage(Image);
				boxBackground.FillPolygon(GetBrush(bg.Value), pointsArr);
			}
		}

		public void DrawRectangle(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			var bg = background ?? _defaultBackground;
			var boxBackground = Graphics.FromImage(Image);
			if (bg.HasValue)
			{
				boxBackground.FillRectangle(GetBrush(bg.Value), x, y, width, height);
				boxBackground = Graphics.FromImage(Image);
			}
			
			boxBackground.DrawRectangle(GetPen(line ?? _defaultForeground), x, y, width, height);
		}

		public void DrawText(
			int x,
			int y,
			string message,
			Color? foreColor = null,
			Color? backColor = null,
			int? fontSize = null,
			string fontFamily = null,
			string fontStyle = null,
			string horizAlign = null,
			string vertAlign = null)
		{
			var family = FontFamily.GenericMonospace;
			if (fontFamily != null)
			{
				family = new FontFamily(fontFamily);
			}

			var fStyle = FontStyle.Regular;
			if (fontStyle != null)
			{
				switch (fontStyle.ToLower())
				{
					default:
					case "regular":
						break;
					case "bold":
						fStyle = FontStyle.Bold;
						break;
					case "italic":
						fStyle = FontStyle.Italic;
						break;
					case "strikethrough":
						fStyle = FontStyle.Strikeout;
						break;
					case "underline":
						fStyle = FontStyle.Underline;
						break;
				}
			}

			var f = new StringFormat(StringFormat.GenericDefault);
			var font = new Font(family, fontSize ?? 12, fStyle, GraphicsUnit.Pixel);
			var boxBackground = Graphics.FromImage(Image);

			Size sizeOfText = boxBackground.MeasureString(message, font, 0, f).ToSize();

			if (horizAlign != null)
			{
				switch (horizAlign.ToLower())
				{
					default:
					case "left":
						break;
					case "center":
					case "middle":
						x -= sizeOfText.Width / 2;
						break;
					case "right":
						x -= sizeOfText.Width;
						break;
				}
			}

			if (vertAlign != null)
			{
				switch (vertAlign.ToLower())
				{
					default:
					case "top":
						break;
					case "center":
					case "middle":
						y -= sizeOfText.Height / 2;
						break;
					case "bottom":
						y -= sizeOfText.Height;
						break;
				}
			}
			Rectangle rect = new Rectangle(new Point(x, y), sizeOfText);
			boxBackground = Graphics.FromImage(Image);
			boxBackground.FillRectangle(GetBrush(backColor ?? _defaultTextBackground.Value), rect);
			boxBackground = Graphics.FromImage(Image);
			boxBackground.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
			boxBackground.DrawString(message, font, new SolidBrush(foreColor ?? Color.Black), x, y);
		}
		
		public Point GetMouse()
		{
			var p = PointToClient(MousePosition);
			return p;
		}

		private void DoLuaClick(object sender, EventArgs e)
		{
			LuaWinform parent = Parent as LuaWinform;
			parent?.DoLuaEvent(Handle);
		}

		protected override void OnClick(EventArgs e)
		{
			DoLuaClick(this, e);
			base.OnClick(e);
		}
	}
}
