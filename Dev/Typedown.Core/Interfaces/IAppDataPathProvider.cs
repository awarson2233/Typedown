namespace Typedown.Core.Interfaces
{
    public interface IAppDataPathProvider
    {
        string GetLocalFolderPath();

        string GetSettingsFilePath();

        string GetDatabaseFilePath();

        string GetBackupFolderPath();
    }
}
