using System.Collections.Generic;

namespace DuplicateSearcher
{
	public class Duplet
	{
		public string Name { get; set; } 
		public List<Track> Tracks { get; set; }
		public Duplet(Track track)
		{
			Name = track.Name;
			Tracks = new List<Track> { track };
		}
	}
}