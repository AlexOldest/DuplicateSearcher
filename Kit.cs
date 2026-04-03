using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DuplicateSearcher
{
	public class Kit
	{
		public static string FindFolder(string name)
		{
			string
				dir = AppContext.BaseDirectory,
				bd = Path.GetPathRoot(dir);
			for (char slash = '\\'; dir != null; dir = Path.GetDirectoryName(dir))
			{
				string res = dir.TrimEnd(slash) + slash + name;
				if (Directory.Exists(res))
					return res + slash;
			}
			throw new Exception($"Could not find folder named '{name}' in {bd}\r\nApplication must have a 'Data' directory");
		}

		public static string FindOrPointMusicDir(string dataDir)
		{
			var dlg = new FolderBrowserDialog 
			{ 
				SelectedPath = Path.GetFullPath(dataDir), 
				ShowNewFolderButton = false,
				RootFolder = Environment.SpecialFolder.MyComputer
			};
			if (FolderBrowserLauncher.ShowFolderBrowser(dlg) == DialogResult.OK)
				return dlg.SelectedPath + "\\";
			throw new Exception("You must point a directory\nwhere duplicats should be searched for");
		}

		public static (int,int) FormatMsg(ref string msg)
		{
			var sb = new StringBuilder();
			int w = 2, wmax = 2, h = 2;
			for (int i = 0, j = 0; i < msg.Length; i++, j++)
			{
				var c = msg[i];
				if (c == '\n' || c == '\r')
				{
					j = 0;
					w = 2;
					h++;
				}
				sb.Append(c);
				w++;
				if (j > 45)
				{
					while (++i < msg.Length - 1 && (c = msg[i]) != ' ' && c != '.' && c != ',' && c != '\n')
					{
						sb.Append(c);
						w++;
					}
					sb.Append(c); w++;
					if (c != '\n')
						sb.Append(c = '\n');
					if (c == '\n')
						w = 2;
					h++;
					for (int k = i + 1; k < msg.Length - 1 && msg[k] == ' ' && msg[k] == '\n'; k++)
						i = k;
					j = 0;
				}
				if (w > wmax)
					wmax = w;
			}
			msg = sb.ToString();
			return (wmax, h);
		}
		public static string Trim(string s)
		{
			return new Regex(@"\s{2,}").Replace(s.Trim(), " ");
		}
		public static string Strip(string s)
		{
			s = Trim(s.Replace('-', ' ').Replace('_', ' '));
			int i = 0;
			for (; i < s.Length; i++)
			{
				char c = s[i];
				if (char.IsWhiteSpace(c) || char.IsLetter(s[i]))
					break;
			}
			return s.Substring(i).Trim();
		}
		public static string ToTitleCase(string s)
		{
			var sb = new StringBuilder();
			s = Strip(s);
			if (s.Length > 0)
			{
				var cap = true;
				bool wasTick = false;
				for (int i = 0; i < s.Length; i++)
				{
					var c = s[i];
					if (wasTick)
						sb.Append(c);
					else
						sb.Append(cap ? char.ToUpper(c) : char.ToLower(c));
					wasTick = c == '\'';
					cap = c == ' ' || char.IsPunctuation(c);
				}
			}
			return sb.ToString().TrimEnd();
		}
	}
}