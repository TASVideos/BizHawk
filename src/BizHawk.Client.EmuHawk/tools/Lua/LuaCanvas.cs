using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using BizHawk.Client.Common;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.EmuHawk
{
	[Description("Represents a canvas object returned by the gui.createcanvas() method")]
	public sealed class LuaCanvas<TTable> : Form
	{
		private readonly Action<string> LogOutputCallback;

		private readonly LuaPictureBox<TTable> luaPictureBox;

		public LuaCanvas(int width, int height, int? x, int? y, ILuaTableHelper<TTable> tableHelper, Action<string> logOutputCallback)
		{
			LogOutputCallback = logOutputCallback;

			SuspendLayout();

			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			AutoSize = true;
			AutoSizeMode = AutoSizeMode.GrowAndShrink;
			ClientSize = new Size(284, 261);
			FormBorderStyle = FormBorderStyle.FixedSingle;
			Name = "LuaCanvas";
			Text = "LuaCanvas";

			if (x.HasValue)
			{
				StartPosition = FormStartPosition.Manual;
				Left = (int)x;
				if (y.HasValue)
				{
					Top = (int)y;
				}
			}

			luaPictureBox = new LuaPictureBox<TTable>
			{
				Image = Properties.Resources.LuaPictureBox,
				Location = new Point(0, 0),
				Margin = new Padding(0),
				Name = "luaPictureBox",
				Size = new Size(100, 50),
				SizeMode = PictureBoxSizeMode.AutoSize,
				TabIndex = 0,
				TableHelper = tableHelper,
				TabStop = false
			};
			Controls.Add(luaPictureBox);

			ResumeLayout();

			// was this done after reflow for a reason? --yoshi
			luaPictureBox.Width = width;
			luaPictureBox.Height = height;
			luaPictureBox.Image = new Bitmap(width, height);
			PerformLayout();
		}

		[LuaMethodExample(
			"LuaCanvas.setTitle( \"Title\" );")]
		[LuaMethod(
			"setTitle",
			"Sets the canvas window title")]
		public void SetTitle(string title)
		{
			Text = title;
		}

		[LuaMethodExample(
			"LuaCanvas.setLocation( 16, 32 );")]
		[LuaMethod(
			"setLocation",
			"Sets the location of the canvas window")]
		public void SetLocation(int x, int y) 
		{
			StartPosition = FormStartPosition.Manual;
			Left = x;
			Top = y;
		}

		[LuaMethodExample(
			"LuaCanvas.clear( 0x000000FF );")]
		[LuaMethod(
			"clear",
			"Clears the canvas")]
		public void Clear(Color color)
		{
			luaPictureBox.Clear(color);
		}

		[LuaMethodExample(
			"LuaCanvas.refresh( );")]
		[LuaMethod(
			"refresh",
			"Redraws the canvas")]
		public new void Refresh()
		{
			luaPictureBox.Refresh();
		}

		[LuaMethodExample(
			"LuaCanvas.setDefaultForegroundColor( 0x000000FF );")]
		[LuaMethod(
			"setDefaultForegroundColor",
			"Sets the default foreground color to use in drawing methods, white by default")]
		public void SetDefaultForegroundColor(Color color)
		{
			luaPictureBox.SetDefaultForegroundColor(color);
		}

		[LuaMethodExample(
			"LuaCanvas.setDefaultBackgroundColor( 0x000000FF );")]
		[LuaMethod(
			"setDefaultBackgroundColor",
			"Sets the default background color to use in drawing methods, transparent by default")]
		public void SetDefaultBackgroundColor(Color color)
		{
			luaPictureBox.SetDefaultBackgroundColor(color);
		}

		[LuaMethodExample(
			"LuaCanvas.setDefaultTextBackground( 0x000000FF );")]
		[LuaMethod(
			"setDefaultTextBackground",
			"Sets the default background color to use in text drawing methods, half-transparent black by default")]
		public void SetDefaultTextBackground(Color color)
		{
			luaPictureBox.SetDefaultTextBackground(color);
		}

		[LuaMethodExample(
			"LuaCanvas.drawBezier( { { 5, 10 }, { 10, 10 }, { 10, 20 }, { 5, 20 } }, 0x000000FF );")]
		[LuaMethod(
			"drawBezier",
			"Draws a Bezier curve using the table of coordinates provided in the given color")]
		public void DrawBezier(TTable points, Color color)
		{
			try
			{
				luaPictureBox.DrawBezier(points, color);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample(
			"LuaCanvas.drawBox( 16, 32, 162, 322, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawBox",
			"Draws a rectangle on screen from x1/y1 to x2/y2. Same as drawRectangle except it receives two points intead of a point and width/height")]
		public void DrawBox(int x, int y, int x2, int y2, Color? line = null, Color? background = null)
		{
			try
			{
				luaPictureBox.DrawBox(x, y, x2, y2, line, background);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample(
			"LuaCanvas.drawEllipse( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawEllipse",
			"Draws an ellipse at the given coordinates and the given width and height. Line is the color of the ellipse. Background is the optional fill color")]
		public void DrawEllipse(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			try
			{
				luaPictureBox.DrawEllipse(x, y, width, height, line, background);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample(
			"LuaCanvas.drawIcon( \"C:\\icon.ico\", 16, 32, 18, 24 );")]
		[LuaMethod(
			"drawIcon",
			"draws an Icon (.ico) file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawIcon(string path, int x, int y, int? width = null, int? height = null)
		{
			try
			{
				luaPictureBox.DrawIcon(path, x, y, width, height);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample(
			"LuaCanvas.drawImage( \"C:\\image.bmp\", 16, 32, 18, 24, false );")]
		[LuaMethod(
			"drawImage",
			"draws an image file from the given path at the given coordinate. width and height are optional. If specified, it will resize the image accordingly")]
		public void DrawImage(string path, int x, int y, int? width = null, int? height = null, bool cache = true)
		{
			if (!File.Exists(path))
			{
				LogOutputCallback($"File not found: {path}\nScript Terminated");
				return;
			}

			luaPictureBox.DrawImage(path, x, y, width, height, cache);
		}

		[LuaMethodExample(
			"LuaCanvas.clearImageCache( );")]
		[LuaMethod(
			"clearImageCache",
			"clears the image cache that is built up by using gui.drawImage, also releases the file handle for cached images")]
		public void ClearImageCache()
		{
			luaPictureBox.ClearImageCache();
		}

		[LuaMethodExample(
			"LuaCanvas.drawImageRegion( \"C:\\image.png\", 11, 22, 33, 44, 21, 43, 34, 45 );")]
		[LuaMethod(
			"drawImageRegion",
			"draws a given region of an image file from the given path at the given coordinate, and optionally with the given size")]
		public void DrawImageRegion(string path, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int destX, int destY, int? destWidth = null, int? destHeight = null)
		{
			if (!File.Exists(path))
			{
				LogOutputCallback($"File not found: {path}\nScript Terminated");
				return;
			}

			luaPictureBox.DrawImageRegion(path, sourceX, sourceY, sourceWidth, sourceHeight, destX, destY, destWidth, destHeight);
		}

		[LuaMethodExample(
			"LuaCanvas.drawLine( 161, 321, 162, 322, 0xFFFFFFFF );")]
		[LuaMethod(
			"drawLine",
			"Draws a line from the first coordinate pair to the 2nd. Color is optional (if not specified it will be drawn black)")]
		public void DrawLine(int x1, int y1, int x2, int y2, Color? color = null)
		{
			luaPictureBox.DrawLine(x1, y1, x2, y2, color);
		}

		[LuaMethodExample(
			"LuaCanvas.drawAxis( 16, 32, int size, 0xFFFFFFFF );")]
		[LuaMethod(
			"drawAxis",
			"Draws an axis of the specified size at the coordinate pair.)")]
		public void DrawAxis(int x, int y, int size, Color? color = null)
		{
			luaPictureBox.DrawAxis(x, y, size, color);
		}

		[LuaMethodExample(
			"LuaCanvas.drawArc( 16, 32, 77, 99, 180, 90, 0x007F00FF );")]
		[LuaMethod(
			"drawArc",
			"draws a Arc shape at the given coordinates and the given width and height"
		)]
		public void DrawArc(int x, int y, int width, int height, int startAngle, int sweepAngle, Color? line = null)
		{
			luaPictureBox.DrawArc(x, y, width, height, startAngle, sweepAngle, line);
		}

		[LuaMethodExample(
			"LuaCanvas.drawPie( 16, 32, 77, 99, 180, 90, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawPie",
			"draws a Pie shape at the given coordinates and the given width and height")]
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
			luaPictureBox.DrawPie(x, y, width, height, startAngle, sweepAngle, line, background);
		}

		[LuaMethodExample(
			"LuaCanvas.drawPixel( 16, 32, 0xFFFFFFFF );")]
		[LuaMethod(
			"drawPixel",
			"Draws a single pixel at the given coordinates in the given color. Color is optional (if not specified it will be drawn black)")]
		public void DrawPixel(int x, int y, Color? color = null)
		{
			try
			{
				luaPictureBox.DrawPixel(x, y, color);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}

		[LuaMethodExample(
			"LuaCanvas.drawPolygon( { 10, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawPolygon",
			"Draws a polygon using the table of coordinates specified in points. This should be a table of tables(each of size 2). Line is the color of the polygon. Background is the optional fill color")]
		public void DrawPolygon(TTable points, int? x = null, int? y = null, Color? line = null, Color? background = null)
		{
			try
			{
				luaPictureBox.DrawPolygon(points, x, y, line, background);
			}
			catch (Exception ex)
			{
				LogOutputCallback(ex.Message);
			}
		}


		[LuaMethodExample(
			"LuaCanvas.drawRectangle( 16, 32, 77, 99, 0x007F00FF, 0x7F7F7FFF );")]
		[LuaMethod(
			"drawRectangle",
			"Draws a rectangle at the given coordinate and the given width and height. Line is the color of the box. Background is the optional fill color")]
		public void DrawRectangle(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			luaPictureBox.DrawRectangle(x, y, width, height, line, background);
		}

		[LuaMethodExample(
			"LuaCanvas.drawString( 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod(
			"drawString",
			"Alias of DrawText()")]
		public void DrawString(
			int x,
			int y,
			string message,
			Color? foreColor = null,
			Color? backColor = null,
			int? fontSize = null,
			string fontFamily = null,
			string fontStyle = null,
			string horizontalAlign = null,
			string verticalAlign = null)
		{
			luaPictureBox.DrawText(x, y, message, foreColor, backColor, fontSize, fontFamily, fontStyle, horizontalAlign, verticalAlign);
		}

		[LuaMethodExample(
			"LuaCanvas.drawText( 16, 32, \"Some message\", 0x7F0000FF, 0x00007FFF, 8, \"Arial Narrow\", \"bold\", \"center\", \"middle\" );")]
		[LuaMethod(
			"drawText",
			"Draws the given message at the given x,y coordinates and the given color. The default color is white. A fontfamily can be specified and is monospace generic if none is specified (font family options are the same as the .NET FontFamily class). The fontsize default is 12. The default font style is regular. Font style options are regular, bold, italic, strikethrough, underline. Horizontal alignment options are left (default), center, or right. Vertical alignment options are bottom (default), middle, or top. Alignment options specify which ends of the text will be drawn at the x and y coordinates.")]
		public void DrawText(
			int x,
			int y,
			string message,
			Color? foreColor = null,
			Color? backColor = null,
			int? fontSize = null,
			string fontFamily = null,
			string fontStyle = null,
			string horizontalAlign = null,
			string verticalAlign = null)
		{
			luaPictureBox.DrawText(x, y, message, foreColor, backColor, fontSize, fontFamily, fontStyle, horizontalAlign, verticalAlign);
		}


		// It'd be great if these were simplified into 1 function, but I cannot figure out how to return a LuaTable from this class
		[LuaMethodExample(
			"local inLuaget = LuaCanvas.getMouseX( );")]
		[LuaMethod(
			"getMouseX",
			"Returns an integer representation of the mouse X coordinate relative to the canvas window.")]
		public int GetMouseX()
		{
			var position = luaPictureBox.GetMouse();
			return position.X;
		}

		[LuaMethodExample(
			"local inLuaget = LuaCanvas.getMouseY( );")]
		[LuaMethod(
			"getMouseY",
			"Returns an integer representation of the mouse Y coordinate relative to the canvas window.")]
		public int GetMouseY()
		{
			var position = luaPictureBox.GetMouse();
			return position.Y;
		}
	}
}
