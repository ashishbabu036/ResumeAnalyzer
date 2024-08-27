namespace IHire.API.Helper
{
    public class FileHandler
    {
        public static async System.Threading.Tasks.Task CopyStream(Stream stream, string downloadPath)
        {

            if (File.Exists(downloadPath))
            {
                File.Delete(downloadPath);
            }

            using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream);
            }
        }
    }
}
