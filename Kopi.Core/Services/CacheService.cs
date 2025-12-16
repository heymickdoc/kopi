
using System.Runtime.InteropServices;
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services;

public static class CacheService
{
	private static readonly string KopiCacheWin =
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kopi", "Cache");
	
	//Linux path is ~/.kopi/cache
	private static readonly string KopiCacheLinux =
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kopi", "cache");

	private static string GetCacheLocation()
	{
		return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? KopiCacheWin : KopiCacheLinux;
	}
	
    /// <summary>
    /// Checks to see if the file is already cached
    /// </summary>
    /// <param name="hashedString">The name of the .json file</param>
    /// <returns>True if cached and valid</returns>
    public static bool IsCached(string hashedString)
    {
        //Check the cache folder for a file with the name of the input string.
        var cacheFilePath = Path.Combine(GetCacheLocation(), hashedString + ".json");
        
        return File.Exists(cacheFilePath);
    }

	/// <summary>
	/// Loads the cached file and returns the SourceDbModel
	/// </summary>
	/// <param name="hashedString">The name of the .json file</param>
	/// <returns></returns>
	/// <exception cref="FileNotFoundException">Can't find the cached file</exception>
	public static async Task<SourceDbModel> LoadFromCache(string hashedString)
	{
		Msg.Write(MessageType.Info, "Loading source database model from cache...");
		var cacheFilePath = Path.Combine(GetCacheLocation(), hashedString + ".json");
		if (!File.Exists(cacheFilePath))
        {
            throw new FileNotFoundException("Cache file not found", cacheFilePath);
		}

        var json = await File.ReadAllTextAsync(cacheFilePath);
        var sourceDbModel = System.Text.Json.JsonSerializer.Deserialize<SourceDbModel>(json);
        if (sourceDbModel == null)
        {
            throw new InvalidOperationException("Failed to deserialize cache file to SourceDbModel");
		}
        
        Msg.Write(MessageType.Success, "Successfully loaded source database model from cache.");
        Console.WriteLine("");
        return sourceDbModel;
	}

	/// <summary>
	/// Writes the SourceDbModel to a cached file
	/// </summary>
	/// <param name="hashedString"></param>
	/// <param name="sourceDbData"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public static async Task WriteToCache(string hashedString, SourceDbModel sourceDbData)
	{
		try
		{
			if (!Directory.Exists(GetCacheLocation())) Directory.CreateDirectory(GetCacheLocation());

			var cacheFilePath = Path.Combine(GetCacheLocation(), hashedString + ".json");
			
			//Delete if it already exists
			if (File.Exists(cacheFilePath)) File.Delete(cacheFilePath);

			//Write the new file
			var json = System.Text.Json.JsonSerializer.Serialize(sourceDbData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
			await File.WriteAllTextAsync(cacheFilePath, json);
		}
		catch (UnauthorizedAccessException ex)
		{
			Msg.Write(MessageType.Error, $"Access denied when trying to write cache file: {ex.Message}");
			Environment.Exit(1);
		}
		catch (Exception ex)
		{
			Msg.Write(MessageType.Error, $"Error when trying to write cache file: {ex.Message}");
			Environment.Exit(1);
		}

	}
}
