using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Serilog;

namespace pepperspray.ExternalServer.Storage
{
  internal class WorldStorage
  {
    private static string defaultWorldUid = "2";

    private uint worldCacheCapacity;
    private Dictionary<string, byte[]> worldCache = new Dictionary<string, byte[]>();
    private List<string> worldCacheKeys = new List<string>();

    internal WorldStorage(uint cacheCapacity)
    {
      Log.Debug("Created WorldStorage with capacity {capacity}", cacheCapacity);
      this.worldCacheCapacity = cacheCapacity;
    }

    internal byte[] GetWorldData(string uid)
    {
      var path = this.worldPath(uid);
      Log.Debug("WorldStorage request world with {uid}/{path}", uid, path);
      if (this.worldCache.ContainsKey(path))
      {
        this.touchWorldCacheKey(path);
        Log.Verbose("Returning world data for {path} from cache", path);
        return this.worldCache[path];
      }

      if (!File.Exists(path))
      {
        return this.GetWorldData(WorldStorage.defaultWorldUid);
      }
      else
      {
        var bytes = File.ReadAllBytes(path);
        this.worldCache[path] = bytes;
        this.cleanupWorldCache();
        Log.Verbose("Returning world data for {path} from disk", path);
        return bytes;
      }
    }

    internal void SaveWorldData(string uid, Stream dataStream)
    {
      var path = this.worldPath(uid);
      Log.Debug("WorldStorage saving world with {uid}/{path}", uid, path);
      
      using (var memoryStream = new MemoryStream())
      {
        dataStream.CopyTo(memoryStream);
        this.worldCache[path] = memoryStream.ToArray();
        Log.Verbose("Cached world for {path}", path);
        this.touchWorldCacheKey(path);
        this.cleanupWorldCache();
      }

      if (File.Exists(path))
      {
        File.Delete(path);
      }

      using (var fileStream = File.OpenWrite(path))
      {
        dataStream.Seek(0, SeekOrigin.Begin);
        dataStream.CopyTo(fileStream);

        Log.Verbose("Saved world for {path} to disk", path);
      }
    }

    private void touchWorldCacheKey(string key)
    {
      if (this.worldCacheKeys.Contains(key))
      {
        this.worldCacheKeys.Remove(key);
      }

      this.worldCacheKeys.Insert(0, key);
      Log.Verbose("Touching world at {path}, new order is {keys}", key, this.worldCacheKeys);
    }

    private void cleanupWorldCache()
    {
      for (int i = this.worldCacheKeys.Count() - 1; i >= this.worldCacheCapacity; i--)
      {
        var key = this.worldCacheKeys[i];
        this.worldCache.Remove(key);
        this.worldCacheKeys.Remove(key);
        Log.Verbose("Removed world {path} from cache due to exceeding capacity", key);
      }
    }

    private string worldPath(string uid)
    {
      string identifier = null;
      if (uid.Equals("2"))
      {
        identifier = "default";
      }
      else
      {
        identifier = Utils.Hashing.Md5(uid);
      }
      return ExternalServer.worldDirectoryPath + "\\" + identifier + ".world";
    }
  }
}
