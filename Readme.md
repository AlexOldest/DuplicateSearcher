# DuplicateSearcher (WPF No XAML)
### Author: Alexander Chernosvitov

A utility for finding, listening to, and removing duplicate music files (MP3, FLAC, OGG).

#### Key Features:
* **"No XAML" Approach**: The entire WPF interface (DockPanel, ToolBar, Table, MediaElement) is created exclusively in C#. No XAML markup.
* **Win32 Interop**: Uses Shell32 to retrieve accurate metadata (bitrate, duration) directly from the Windows OS.
* **Smart Search**: The name normalization algorithm (TitleCase, Strip, Trim) finds duplicates even with different file name formats.
* **Built-in Player**: Allows you to instantly preview found duplicates before deleting them, preventing the loss of rare tracks or alternate duplicates.

*The project was originally published on CodeProject.*