using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DuplicateSearcher
{
	public static class FolderBrowserLauncher
  {
    const string searchString = "Browse For Folder";
    const int dlgItemBrowseControl = 0;
    const int dlgItemTreeView = 100;

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

    const int
      TV_FIRST = 0x1100,
      TVM_SELECTITEM = (TV_FIRST + 11),
      TVM_GETNEXTITEM = (TV_FIRST + 10),
      TVM_GETITEM = (TV_FIRST + 12),
      TVM_ENSUREVISIBLE = (TV_FIRST + 20),
      TVGN_ROOT = 0x0,
      TVGN_NEXT = 0x1,
      TVGN_CHILD = 0x4,
      TVGN_FIRSTVISIBLE = 0x5,
      TVGN_NEXTVISIBLE = 0x6,
      TVGN_CARET = 0x9;
    
    public static DialogResult ShowFolderBrowser(FolderBrowserDialog dlg, IWin32Window parent = null)
    {
      var result = DialogResult.Cancel;
      int retries = 10;
      using (Timer t = new Timer { Interval = 150 })
      {
        t.Tick += (s, a) =>
        {
          if (retries > 0)
          {
            --retries;
            IntPtr hwndDlg = FindWindow((string)null, searchString);
            if (hwndDlg != IntPtr.Zero)
            {
              IntPtr hwndFolderCtrl = GetDlgItem(hwndDlg, dlgItemBrowseControl);
              if (hwndFolderCtrl != IntPtr.Zero)
              {
                IntPtr hwndTV = GetDlgItem(hwndFolderCtrl, dlgItemTreeView);

                if (hwndTV != IntPtr.Zero)
                {
                  IntPtr item = SendMessage(hwndTV, (uint)TVM_GETNEXTITEM, new IntPtr(TVGN_CARET), IntPtr.Zero);
                  if (item != IntPtr.Zero)
                  {
                    SendMessage(hwndTV, TVM_ENSUREVISIBLE, IntPtr.Zero, item);
                    retries = 0;
                    t.Stop();
                  }
                }
              }
            }
          }
          else
          {
            // We failed to find the Tree View control.
            // As a fall back (and this is an UberUgly hack), we will send some fake keystrokes 
            // to the application in an attempt to force the Tree View to scroll to the selected item.
            t.Stop();
            SendKeys.Send("{TAB}{TAB}{DOWN}{DOWN}{UP}{UP}");
          }
        };
        t.Start();
        result = dlg.ShowDialog(parent);
      }
      return result;
    }
  }
}