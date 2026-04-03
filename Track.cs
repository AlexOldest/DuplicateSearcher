using System.IO;

namespace DuplicateSearcher
{
	public class Track
	{
		public string FilePath, Title, Name, Album, AlbumPath, TrackNo, Ext;
		public Track(string path) 
		{
			FilePath = path;
			Title = Path.GetFileNameWithoutExtension(path);
			Name = Kit.ToTitleCase(Title);
			AlbumPath = Path.GetDirectoryName(path);
			Album = AlbumPath.Replace(MainWindow.PathRoot, "");
			TrackNo = Title.Substring(0, 2);
			Ext = Path.GetExtension(path);
		}
		public override bool Equals(object x)
		{
			return x is Track t && FilePath.Equals(t.FilePath);
		}
		public override int GetHashCode() { return FilePath.GetHashCode(); }
	}
}