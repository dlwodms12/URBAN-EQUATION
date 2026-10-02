using System;
using System.IO;
using System.Security;
using System.Text;

public sealed class ProgressFileStore
{
    public string FilePath { get; }
    public ProgressFileStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Save path is required.");
        FilePath = Path.GetFullPath(filePath);
    }

    public bool TryRead(out string json, out bool exists, out string error)
    {
        json = null;
        exists = false;
        error = null;
        try
        {
            json = File.ReadAllText(FilePath, new UTF8Encoding(false, true));
            exists = true;
            return true;
        }
        catch (FileNotFoundException) { return true; }
        catch (DirectoryNotFoundException) { return true; }
        catch (Exception exception) when (IsStorageException(exception))
        { error = "Could not read progress: " + exception.Message; return false; }
    }

    public bool TryWrite(string json, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(json)) { error = "Save content is missing."; return false; }
        string temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
                {
                    writer.Write(json);
                    writer.Flush();
                }
                stream.Flush(true);
            }
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
            else File.Move(temporary, FilePath);
            return true;
        }
        catch (Exception exception) when (IsStorageException(exception))
        { error = "Could not write progress: " + exception.Message; return false; }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (Exception exception) when (IsStorageException(exception)) { }
        }
    }

    internal static bool IsStorageException(Exception exception) =>
        exception is IOException || exception is UnauthorizedAccessException
        || exception is ArgumentException || exception is NotSupportedException
        || exception is SecurityException;
}
