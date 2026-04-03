using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DuplicateSearcher
{
	public class MsgWindow : Window
	{
		readonly DispatcherTimer timer;
		readonly TextBlock box;
		bool stay;

		public MsgWindow(string msg, int delay)
		{
			var (w, h) = Kit.FormatMsg(ref msg);
			Width = (int)(w * 8.2);
			Height = (int)(h * 18.5);
			WindowStartupLocation = WindowStartupLocation.Manual;
			Left = 200; Top = 200;
			WindowStyle = WindowStyle.None;
			AllowsTransparency = true;
			//Topmost = true;
			stay = false;

			var p = new StackPanel();
			Content = p;
			box = new TextBlock
			{
				Padding = new Thickness(8),
				Background = new SolidColorBrush(Color.FromArgb(255, 0, 80, 90)),
				Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)),
				FontSize = 16,
				FontFamily = new FontFamily("Trebuchet MS"),
				Height = Height,
				Text = msg
			};
			p.Children.Add(box); //box.CaptureMouse();

			box.MouseDown += (s, e) =>
			{
				if (e.RightButton == MouseButtonState.Pressed)
				{
					Clipboard.Clear();
					Clipboard.SetText(box.Text);
				}
				stay = !stay;
				if (!stay)
					Close();
			};
			box.Focus();
			timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(delay) };
			timer.Start();
			timer.Tick += (s, e) =>
			{
				if (!stay)
				{
					timer.Stop();
					Close();
				}
			};
		}
	}
}
