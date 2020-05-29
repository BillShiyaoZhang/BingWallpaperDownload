namespace UWPLibrary
{
    /// <summary>
    /// State of function RunFunctionAsync may return.
    /// </summary>
    public enum DownloadAndSetWallpaperCode
    {
        SUCCESSFUL, FAILED, NO_INTERNET, UNEXPECTED_EXCEPTION, FOLDER_NOT_SET
    }
}