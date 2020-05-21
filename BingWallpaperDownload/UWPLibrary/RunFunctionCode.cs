namespace UWPLibrary
{
    /// <summary>
    /// State of function RunFunctionAsync may return.
    /// </summary>
    public enum RunFunctionCode
    {
        SUCCESSFUL, FAILED, NO_INTERNET, UNEXPECTED_EXCEPTION, FOLDER_NOT_SET
    }
}