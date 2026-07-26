using System;
using System.Runtime.InteropServices;

internal static class FileDialogHelper {
    private const int MaxFileLength = 1024;
    private const int OFN_PATHMUSTEXIST = 0x800;
    private const int OFN_FILEMUSTEXIST = 0x1000;
    private const int OFN_OVERWRITEPROMPT = 0x00000002;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class OpenFileName {
        public int structSize = Marshal.SizeOf(typeof(OpenFileName));
        public IntPtr hwnd = IntPtr.Zero;
        public IntPtr hinst = IntPtr.Zero;
        public string? filter = null;
        public string? custFilter = null;
        public int custFilterMax = 0;
        public int filterIndex = 0;
        public string? file = null;
        public int maxFile = 0;
        public string? fileTitle = null;
        public int maxFileTitle = 0;
        public string? initialDir = null;
        public string? title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtMax = 0;
        public string? defExt = null;
        public int custData = 0;
        public IntPtr pHook = IntPtr.Zero;
        public string? template = null;
    }

    [DllImport("Comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

    public static string? ShowOpenDialog(string title, string filter, string initialDir) {
        var ofn = new OpenFileName() {
            file = new string(new char[MaxFileLength]),
            maxFile = MaxFileLength,
            title = title,
            initialDir = initialDir,
            filter = filter + "\0",
            flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST
        };
        return GetOpenFileName(ofn) ? ofn.file : null;
    }
    public static string? ShowSaveDialog(string title, string filter, string initialDir, string defaultExt) {
        var ofn = new OpenFileName() {
            file = new string(new char[MaxFileLength]),
            maxFile = MaxFileLength,
            title = title,
            initialDir = initialDir,
            filter = filter + "\0",
            flags = OFN_PATHMUSTEXIST | OFN_OVERWRITEPROMPT,
            defExt = defaultExt
        };
        return GetSaveFileName(ofn) ? ofn.file : null;
    }
}

