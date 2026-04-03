using Shell32;
using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DuplicateSearcher
{
	public partial class MainWindow : Window
	{
		const string 
			playChar = "\u25b6", stopChar = "\u23f8", delChar = "\u2612",
			prompt = "Click 'Open' button to select a folder with musical files";
		readonly SolidColorBrush
			bkp = new SolidColorBrush(Color.FromArgb(255, 240, 255, 240)),
			bkg = new SolidColorBrush(Color.FromArgb(255, 245, 245, 245)),
			bkf = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)),
			bkt = new SolidColorBrush(Color.FromArgb(255, 50, 180, 220)),
			fg = new SolidColorBrush(Color.FromArgb(255, 255, 20, 20)),
			fgt = new SolidColorBrush(Colors.Red),
			brush = new SolidColorBrush(Color.FromArgb(255, 245, 245, 245));

		readonly List<Track> tracks;
		public readonly List<Duplet> duplets;
		Duplet duplet;

		readonly string pathData;
		readonly string[] musicExt = { "*.mp3", "*.flac", "*.ogg" };
		public static string PathRoot = "";
		readonly Shell shell;
		readonly DispatcherTimer timer;
		Run run, runTrack;
		int boxId, playerState;

		StatusBarItem info;
		DockPanel dock;
		ToolBar tool;
		StackPanel panel;
		TextBlock tbMsg;
		ListBox box;
		StatusBar status;
		MediaElement player;
		FlowDocumentScrollViewer viewer;
		FlowDocument doc;
		Table table;
		Slider slider;

		public MainWindow()
		{
			Title = "Select a directory, then search and handle its duplicate files";
			Width = 750;
			Left = 120;
			Top = 10;
			try
			{
				pathData = Kit.FindFolder("Data");
				PathRoot = File.ReadAllText($"{pathData}Path.txt");
				if (string.IsNullOrEmpty(PathRoot))
					PathRoot = pathData;
			}
			catch (Exception ex)
			{
				Content = new TextBlock
				{
					Margin = new Thickness(20),
					Foreground = fgt,
					FontSize = 20,
					Text = ex.Message
				};
				return;
			}
			FillPanel();
			
			Loaded += (s, e) => AddHandlers();
			tracks = new List<Track>();
			duplets = new List<Duplet>();
			shell = new ShellClass();
			timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			timer.Tick += (s, e) => OnTimerTick();
			playerState = -1;
		}
		void AddHandlers()
		{
			
			var p = tool.Items[0] as ToolBarPanel;
			(p.Children[0] as Button).Click += (s, e) => TrySearchFolder();
			(p.Children[1] as Button).Click += (s, e) => ProcessFolder();
			box.SelectionChanged += OnDupletSelected;
			Closing += (s, e) => File.WriteAllText($"{pathData}Path.txt", PathRoot);
			slider.ValueChanged += OnPositionChanged;
			player.MediaOpened += Player_MediaOpened;
		}

		void Player_MediaOpened(object sender, RoutedEventArgs e)
		{
			// Just checking that f.GetDetailsOf(fi, 27) and player.NaturalDuration
			// coincide. Both are wrong for files with 36kbps bitrate. Damn MS.
			if (player.HasAudio) 
			{
				var d = player.NaturalDuration.ToString();
				var dd = d;
			}
		}

		void TrySearchFolder()
		{
			try
			{
				StopPlayer();
				Clear();
				PathRoot = Kit.FindOrPointMusicDir(PathRoot);
				ProcessFolder();
			}
			catch (Exception ex) { new MsgWindow(ex.Message, 5).Show(); }
		}
		public void StopPlayer()
		{
			player.Stop();
			player.Source = null;
			playerState = -1;
			slider.Value = 0;
		}
		void ProcessFolder()
		{
			FindTracks();
			var msgWnd = new MsgWindow("Wait, searching duplicates...", 20000);
			msgWnd.Show();
			Clear();
			SearchDuplicates();
			msgWnd.Close();
			duplets.Sort((a, b) => { return string.Compare(a.Name, b.Name); });
			foreach (var d in duplets)
				box.Items.Add(d.Name);
			UpdateInfo();
		}
		void UpdateInfo()
		{
			var s = $"Folder: {PathRoot};  Tracks: {tracks.Count};  Duplicate Titles: {duplets.Count}";
			info.Content = s;
			tbMsg.Text = s;
		}
		void SearchDuplicates()
		{
			for (int i = 0; i < tracks.Count; i++)
			{
				var t = tracks[i];
				string name = t.Name;
				for (int j = i+1; j < tracks.Count; j++)
				{
					var u = tracks[j];
					if (name.Equals(u.Name, StringComparison.InvariantCultureIgnoreCase))
						AddDupletOrTrack(t, u); 
				}
			}
		}
		void AddDupletOrTrack(Track t, Track u)
		{
			Duplet duplet = null;
			foreach (var d in duplets)
			{
				if (d.Name == t.Name)
				{
					duplet = d;
					break;
				}
			}
			if (duplet == null)
			{
				duplets.Add(duplet = new Duplet(t));
				duplet.Tracks.Add(u);
				return;
			}
			foreach (var d in duplet.Tracks)
			{
				if (d.Equals(u))
					return;
			}
			duplet.Tracks.Add(u);
		}
		void OnDupletSelected(object sender, SelectionChangedEventArgs e)
		{
			player.Stop();
			slider.Value = 0;
			boxId = box.SelectedIndex;
			if (boxId == -1)
				return;
			duplet = duplets[boxId];
			runTrack.Text = $"{duplet.Name}";
			
			var group = table.RowGroups[1];
			group.Rows.Clear();
			string sz = "", ln = "", br = ""; // Size, Length, Bitrate
			for (int i = 0; i < duplet.Tracks.Count; i++)
			{
				var t = duplet.Tracks[i];
				if (t.Ext == ".mp3") // FIXME: Get audio tags from (.flac, .ogg)
				{
					Folder f = shell.NameSpace(t.AlbumPath);
					FolderItem fi = f.ParseName($"{t.Title}.mp3");
					sz = f.GetDetailsOf(fi, 1).ToString().Trim();  // file size
					ln = f.GetDetailsOf(fi, 27).ToString().Trim(); // Length
					br = f.GetDetailsOf(fi, 28).ToString().Trim(); // Bitrate
				}

				group.Rows.Add(new TableRow { Background = bkp, Foreground = bkf });
				var row = group.Rows[group.Rows.Count - 1];
				row.Cells.Add(new TableCell(new Paragraph(new Run(t.Album)) { Margin = new Thickness(25, 0, 0, 4) }) { ColumnSpan = 6 });
				group.Rows.Add(new TableRow { Tag = i });
				row = group.Rows[group.Rows.Count - 1];
				row.Cells.Add(new TableCell(new Paragraph(new Run(t.TrackNo))));
				row.Cells.Add(new TableCell(new Paragraph(new Run(sz))));
				row.Cells.Add(new TableCell(new Paragraph(new Run(ln))));
				row.Cells.Add(new TableCell(new Paragraph(new Run(br))));
				var cp = new TableCell(new Paragraph(new Run(playChar))) { FontSize = 20 };
				row.Cells.Add(cp); // Cell 'play'
				var cd = new TableCell(new Paragraph(new Run(delChar))) { FontSize = 20 };
				row.Cells.Add(cd); // Cell 'delete'
				cp.MouseDown += (s, a) => Play(a);
				cd.MouseDown += (s, a) => Del(a);
			}
			panel.Visibility = Visibility.Visible;
		}
		TableRow GetTableRow(MouseButtonEventArgs e)
		{
			var o = e.OriginalSource as FrameworkContentElement;
			while (!(o is TableRow))
				o = (FrameworkContentElement)o.Parent;
			var row = o as TableRow;
			return row;
		}
		Run GetRun(TableRow row)
		{
			var o = row.Cells[4].Blocks;
			var en = o.GetEnumerator();
			en.MoveNext();
			return (en.Current as Paragraph).Inlines.FirstInline as Run;
		}
		void Play(MouseButtonEventArgs e)
		{
			TableRow row = GetTableRow(e);
			int n = (int)row.Tag;
			Run r = GetRun(row);
			if (playerState == -1)
			{
				r.Text = stopChar;
				player.Source = new Uri(duplet.Tracks[n].FilePath);
				player.Play();
				timer.Start();
				playerState = n;
			}
			else
			{
				player.Pause();
				timer.Stop();
				if (run == r)
				{
					r.Text = playChar;
					playerState = -1;
				}
				else
				{
					run.Text = playChar;
					r.Text = stopChar;
					player.Source = new Uri(duplet.Tracks[n].FilePath);
					player.Play();
					timer.Start();
					playerState = n;
				}
			}
			run = r;
		}
		void Del(MouseButtonEventArgs e)
		{
			TableRow row = GetTableRow(e);
			int n = (int)row.Tag;
			Track t = duplet.Tracks[n];
			var path = t.FilePath;
			if (MessageBox.Show($"Do you really want to delete the file:\r\n '{path}'",
				"Ready to delete the file", MessageBoxButton.YesNo,
				MessageBoxImage.Error, MessageBoxResult.No) == MessageBoxResult.Yes)
			{
				StopPlayer();
				try { File.Delete(path); }
				catch { new MsgWindow("Uncheck 'ReadOnly' flag in your file's Properties dialog", 6000).Show(); }
				duplet.Tracks.RemoveAt(n);
				tracks.Remove(t);
				RepairTable(n);
				if (duplet.Tracks.Count == 1)
					RemoveDuplet();
			}
		}
		void RemoveDuplet()
		{
			duplets.RemoveAt(boxId);
			UpdateInfo();
			box.SelectionChanged -= OnDupletSelected;
			box.Items.RemoveAt(boxId);
			box.SelectionChanged += OnDupletSelected;
			panel.Visibility = Visibility.Collapsed;
			int count = box.Items.Count;
			if (count == 0)
			{
				tbMsg.Text = prompt;
				return;
			}
			if (boxId < count)
				box.SelectedIndex = boxId;
			else
				box.SelectedIndex = boxId = count - 1;
		}
		void RepairTable(int n)
		{
			var rows = table.RowGroups[1].Rows;
			int k = (n << 1) + 1;
			rows.RemoveAt(k);
			rows.RemoveAt(k - 1);
			for (int i = 1; i < rows.Count; i += 2)
			{
				var row = rows[i];
				row.Tag = (i - 1) / 2;
				GetRun(row).Text = playChar;
			}
			UpdateInfo();
		}
		public void FindTracks()
		{
			tracks.Clear();
			foreach (string ext in musicExt)
				AddRecursive(PathRoot, ext);
		}
		void AddRecursive(string dir, string ext)
		{
			var files = Directory.GetFiles(dir, ext);
			if (files.Length > 0)
			{
				foreach (string f in files)
					tracks.Add(new Track(f));
			}
			foreach (string d in Directory.GetDirectories(dir))
				AddRecursive(d, ext);
		}
		void Clear()
		{
			box.SelectionChanged -= OnDupletSelected;
			box.Items.Clear();
			box.SelectionChanged += OnDupletSelected;
			duplets.Clear();
			panel.Visibility = Visibility.Collapsed;
		}
		void FillPanel()
		{
			dock = new DockPanel();
			tool = SetToolBar();
			DockPanel.SetDock(tool, Dock.Top);
			dock.Children.Add(tool);
			Content = dock;

			status = new StatusBar();
			info = new StatusBarItem { Content = "" };
			status.Items.Add(info);
			DockPanel.SetDock(status, Dock.Bottom);
			dock.Children.Add(status);

			box = new ListBox { Padding = new Thickness(3, 1, 10, 1), Width = 220 };
			DockPanel.SetDock(box, Dock.Left);
			dock.Children.Add(box);

			var sp = new StackPanel();
			dock.Children.Add(sp);
			tbMsg = new TextBlock { Text = prompt, FontFamily = new FontFamily("Consolas"), FontSize = 12, Margin = new Thickness(4,2,2,12)};
			sp.Children.Add(tbMsg);
			panel = new StackPanel { Visibility = Visibility.Collapsed };
			sp.Children.Add(panel);
			slider = new Slider { Width = 400, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(15, -2, 0, 4), TickFrequency = 0.5, TickPlacement = TickPlacement.BottomRight, AutoToolTipPlacement = AutoToolTipPlacement.BottomRight };
			panel.Children.Add(slider);
			viewer = new FlowDocumentScrollViewer{ VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Height=400, };
			panel.Children.Add(viewer);
			viewer.Document = doc = new FlowDocument { Background = Brushes.White, FontSize = 12, FontFamily = new FontFamily("Trebuchet MS") };
			table = new Table { };
			doc.Blocks.Add(table);
			string[] names = { "Nr", "Size", "Length", "Bitrate", "Play", "Del" };
			for (int i = 0; i < names.Length; i++)
				table.Columns.Add(new TableColumn { Name = names[i] });
			table.RowGroups.Add(new TableRowGroup());
			var group = table.RowGroups[0];
			group.Rows.Add(new TableRow { FontWeight = FontWeights.Bold, Foreground = fgt, FontSize = 14, Background = bkp });
			var row = group.Rows[0];
			runTrack = new Run();
			row.Cells.Add(new TableCell(new Paragraph(runTrack) { TextAlignment = TextAlignment.Center }) { ColumnSpan = 6 });
			group.Rows.Add(new TableRow { FontWeight = FontWeights.Bold, Background = bkt, Foreground = bkg });
			row = group.Rows[1];
			for (int i = 0; i < names.Length; i++)
				row.Cells.Add(new TableCell(new Paragraph(new Run(names[i]))));
			table.RowGroups.Add(new TableRowGroup());
			player = new MediaElement
			{
				LoadedBehavior = MediaState.Manual,
				Volume = 2,
			};
			panel.Children.Add(player);
			//player.
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
		void OnPositionChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (playerState > -1)
				player.Position = TimeSpan.FromSeconds((sender as Slider).Value);
		}
		ToolBar SetToolBar()
		{
			var tool = new ToolBar { Background = brush };
			var tbPanel = new ToolBarPanel { Orientation = Orientation.Horizontal, Background = brush };
			tool.Items.Add(tbPanel);
			var img = new Image { Source = new BitmapImage(new Uri(pathData + "OpenDir.png")) };
			tbPanel.Children.Add(new Button { Height = 25, Width = 28, Content = img, Background = brush, ToolTip = "Open Directory" });
			img = new Image { Source = new BitmapImage(new Uri(pathData + "Reset.png")) };
			tbPanel.Children.Add(new Button { Height = 25, Width = 28, Content = img, Background = brush, ToolTip = "Read Current Folder once more" });
			return tool;
		}
		[STAThread]
		public static void Main() { new Application().Run(new MainWindow()); }
	}
}

//box.SetBinding(ListBox.ItemsSourceProperty, new Binding("Name") { Source = duplets, Mode = BindingMode.TwoWay } );
//void AddItem(Duplet d)
//{
//	if (box.Dispatcher.CheckAccess())
//		box.Items.Add(d.Name);
//	else
//		box.Dispatcher.Invoke(new Action<Duplet>(AddItem), new object[] { d });
//}
