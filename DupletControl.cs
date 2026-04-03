using Shell32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DuplicateSearcher
{
	public partial class DupletControl : UserControl
	{
		readonly SolidColorBrush
			bkg = new SolidColorBrush(Color.FromArgb(255, 245, 245, 245)),
			bkt = new SolidColorBrush(Color.FromArgb(255, 50, 180, 220));
		readonly string sep = new string('\u22ef', 36);
		readonly TextBlock tbTrack;
		readonly StackPanel[] spv, sph;
		readonly TextBlock[] tb, th;
		readonly Button[] bt, bd;
		readonly Image[] imgPlay, imgStop, imgDel;
		readonly Slider slider;
		readonly Shell shell;
		readonly MediaElement player;
		DispatcherTimer timer;
		Duplet duplet;
		
		public int PlayerState { get; set; }
		public event Action FileDeleting;

		public DupletControl(string path, StackPanel panel, MediaElement player)
		{
			this.player = player;
			PlayerState = -1;
			shell = new ShellClass();
			var ff = new FontFamily("Consolas");
			var fg = new SolidColorBrush(Color.FromArgb(255, 255, 20, 20));
			tbTrack = new TextBlock { FontFamily = ff, FontSize = 16, FontWeight = FontWeights.UltraBold, Foreground = fg, Margin = new Thickness(4, 2, 2, 2) };
			panel.Children.Add(tbTrack);
			imgPlay = new Image[]
			{
				new Image { Source = new BitmapImage(new Uri(path + "Play.png")) },
				new Image { Source = new BitmapImage(new Uri(path + "Play.png")) }
			};
			imgStop = new Image[]
			{
				new Image { Source = new BitmapImage(new Uri(path + "Stop.png")) },
				new Image { Source = new BitmapImage(new Uri(path + "Stop.png")) }
			};
			imgDel = new Image[]
			{
				new Image { Source = new BitmapImage(new Uri(path + "Del.png")) },
				new Image { Source = new BitmapImage(new Uri(path + "Del.png")) }
			};
			spv = new StackPanel[]
			{
				new StackPanel { Visibility = Visibility.Collapsed },
				new StackPanel { Visibility = Visibility.Collapsed },
			};
			sph = new StackPanel[]
			{
				new StackPanel { Orientation = Orientation.Horizontal, },
				new StackPanel { Orientation = Orientation.Horizontal, },
			};
			th = new TextBlock[]
			{
				new TextBlock { FontFamily = ff, FontSize = 16, FontWeight = FontWeights.UltraBold, Margin=new Thickness(4), TextAlignment = TextAlignment.Center, Foreground = bkg, Background = bkt },
				new TextBlock { FontFamily = ff, FontSize = 16, FontWeight = FontWeights.UltraBold, Margin=new Thickness(4), TextAlignment = TextAlignment.Center, Foreground = bkg, Background = bkt }
			};
			tb = new TextBlock[]
			{
				new TextBlock { FontFamily = ff, FontSize = 12, Margin=new Thickness(4) },
				new TextBlock { FontFamily = ff, FontSize = 12, Margin=new Thickness(4) }
			};
			bt = new Button[]
			{
				new Button { Content = imgPlay[0], Margin = new Thickness(10, 2, 2, 2), Background = bkg, Width = 70 },
				new Button { Content = imgPlay[1], Margin = new Thickness(10, 2, 2, 2), Background = bkg, Width = 70 }
			};
			bd = new Button[]
			{
				new Button { Content = imgDel[0], Margin = new Thickness(10, 2, 2, 2), Background = bkg, Width = 70 },
				new Button { Content = imgDel[1], Margin = new Thickness(10, 2, 2, 2), Background = bkg, Width = 70 }
			};
			for (int i = 0; i < 2; i++)
			{
				panel.Children.Add(spv[i]);
				spv[i].Children.Add(th[i]);
				spv[i].Children.Add(sph[i]);
				sph[i].Children.Add(tb[i]);
				sph[i].Children.Add(bt[i]);
				sph[i].Children.Add(bd[i]);				
			}
			bt[0].Click += (s, e) => Play(0);
			bt[1].Click += (s, e) => Play(1);
			bd[0].Click += (s, e) => Del(0);
			bd[1].Click += (s, e) => Del(1);
			slider = new Slider { Width = 400, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(15,8,0,0), TickFrequency = 0.5, TickPlacement = TickPlacement.BottomRight, AutoToolTipPlacement = AutoToolTipPlacement.BottomRight };
			spv[1].Children.Add(slider);
			slider.ValueChanged += OnPositionChanged;
			timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			timer.Tick += (s, e) => OnTimerTick();
		}
		void OnPositionChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (PlayerState > -1)
				player.Position = TimeSpan.FromSeconds((sender as Slider).Value);
		}
		void OnTimerTick()
		{
			slider.ValueChanged -= OnPositionChanged;
			if (player.Source != null && player.NaturalDuration.HasTimeSpan)
			{
				slider.Maximum = player.NaturalDuration.TimeSpan.TotalSeconds;
				slider.Value = player.Position.TotalSeconds;
			}
			slider.ValueChanged += OnPositionChanged;
		}
		public void UpdateDuplet(Duplet d, string pathRoot)
		{
			duplet = d;
			for (int i = 0; i < 2; i++)
			{
				bt[i].Content = imgPlay[i];
				UpdateTags(i, pathRoot);
				spv[i].Visibility = Visibility.Visible;
			}
			slider.Value = 0;
		}
		public void Collapse()
		{
			for (int i = 0; i < 2; i++)
			{
				bt[i].Content = imgPlay[i];
				spv[i].Visibility = Visibility.Collapsed;
			}
			tbTrack.Text = "";
		}

		//public Duplet CheckTracks(Track t, Track u)
		//{
		//	Folder ft = shell.NameSpace(t.AlbumPath);
		//	FolderItem fit = ft.ParseName($"{t.Title}.mp3");
		//	t.Len = ft.GetDetailsOf(fit, 27).ToString().Trim();
		//	int st = ToSeconds(t.Len);

		//	Folder fu = shell.NameSpace(u.AlbumPath);
		//	FolderItem fiu = fu.ParseName($"{u.Title}.mp3");
		//	u.Len = fu.GetDetailsOf(fiu, 27).ToString().Trim();
		//	int su = ToSeconds(u.Len);
		//	if (Math.Abs(st - su) < 10)
		//	{
		//		t.Size = ft.GetDetailsOf(fit, 1).ToString().Trim();
		//		t.Bitrate = ft.GetDetailsOf(fit, 28).ToString().Trim();
		//		u.Size = fu.GetDetailsOf(fiu, 1).ToString().Trim();
		//		u.Bitrate = fu.GetDetailsOf(fiu, 28).ToString().Trim();
		//		return new Duplet(t, u);
		//	}
		//	return null;
		//}

		int ToSeconds(string time)
		{
			string 
				h = time.Substring(0, 2), 
				m = time.Substring(3, 2),
				s = time.Substring(6, 2);
			return int.Parse(h) * 3600 + int.Parse(m) * 60 + int.Parse(s);
		}
		void UpdateTags(int n, string pathRoot)
		{
			Track t = duplet.Tracks[n];
			th[n].Text = Path.GetDirectoryName(t.FilePath).Replace(pathRoot, "");
			tbTrack.Text = $"\n{sep}\nDuplicate Track:  {duplet.Name}\n{sep}";
			var tn = t.Title.Substring(0, 2);
			//tb[n].Text = $"  Size:       {t.Size}\n  Length:     {t.Len}\n  Bit Rate:   {t.Bitrate}\n  Track #:    {tn}";
		}

		void Play(int n)
		{
			if (PlayerState == -1)
			{
				bt[n].Content = imgStop[n];
				player.Source = new Uri(duplet.Tracks[n].FilePath);
				player.Play();
				timer.Start();
				PlayerState = n;
			}
			else
			{
				player.Pause();
				timer.Stop();
				if (n == PlayerState)
				{
					bt[n].Content = imgPlay[n];
					PlayerState = -1;
				}
				else
				{
					bt[n].Content = imgStop[n];
					bt[1 - n].Content = imgPlay[1 - n];
					player.Source = new Uri(duplet.Tracks[n].FilePath);
					player.Play();
					timer.Start();
					PlayerState = n;
				}
			}
		}
		void Del(int n)
		{
			var path = duplet.Tracks[n].FilePath;
			if (MessageBox.Show($"Do you really want to delete the file:\r\n '{path}'",
				"Ready to delete the file", MessageBoxButton.YesNo, 
				MessageBoxImage.Error, MessageBoxResult.No) == MessageBoxResult.Yes)
			{
				StopPlayer();
				try
				{
					File.Delete(path);
					FileDeleting?.Invoke();
				}
				catch
				{
					new MsgWindow("Uncheck 'ReadOnly' flag in your file's Properties dialog", 6000).Show();
				}
			}
		}

		public void StopPlayer()
		{
			player.Stop();
			player.Source = null;
			PlayerState = -1;
			Collapse();
		}
	}
}

