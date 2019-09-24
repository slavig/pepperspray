using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

using Serilog;
using pepperspray.SharedServices;
using pepperspray.Utils;

namespace pepperspray.RestAPIServer.Storage
{
  internal class PhotoStorage: IDIService
  {
    internal class OversizeException : Exception { }

    internal static int FullsizeWidth = 1024, FullsizeHeight = 1024;
    internal static int ThumbnailWidth = 200, ThumbnailHeight = 200;

    private Configuration config;
    private Random random;

    public void Inject()
    {
      this.config = DI.Get<Configuration>();
      this.random = new Random();
    }

    internal uint SizeLimit
    {
      get
      {
        return this.config.PhotoSizeLimit;
      }
    }

    internal string Save(Stream dataStream)
    {
      if (dataStream.Length > this.config.PhotoSizeLimit)
      {
        throw new OversizeException();
      }

      var hash = this.randomHash();

      var originalImage = Image.FromStream(dataStream);

      var image = this.resizeImage(originalImage.Clone() as Image, PhotoStorage.FullsizeWidth, PhotoStorage.FullsizeWidth);
      image.Save(this.filePath(hash), ImageFormat.Jpeg);

      var thumbnail = this.resizeImage(originalImage, PhotoStorage.ThumbnailWidth, PhotoStorage.ThumbnailHeight);
      thumbnail.Save(this.filePath(hash, true), ImageFormat.Jpeg);

      originalImage.Dispose();
      image.Dispose();
      thumbnail.Dispose();

      return hash;
    }

    internal void Delete(string hash)
    {
      try
      {
        File.Delete(this.filePath(hash));
      } 
      catch (Exception) { }

      try
      {
        File.Delete(this.filePath(hash, true));
      } 
      catch (Exception) { }
    }

    private string randomHash()
    {
      return Hashing.Md5(this.random.Next().ToString());
    }

    private string filePath(string hash, bool isThumbnail = false)
    {
      return Path.Combine("peppersprayData", "photos", (isThumbnail ? "thumb_" + hash : hash) + ".jpg");
    }

    public Image resizeImage(Image imgPhoto, int newWidth, int newHeight)
    {
      int sourceWidth = imgPhoto.Width;
      int sourceHeight = imgPhoto.Height;
      int destWidth = 0;
      int destHeight = 0;

      // Calculate new dimentions

      if (sourceWidth <= newWidth && sourceHeight <= newHeight) // Keep the source dimentions if they does not exceed the requested ones
      {
            destHeight = sourceHeight;
            destWidth = sourceWidth;
      }
      else if (sourceWidth < sourceHeight) // Vertical image case
      {
            destHeight = newHeight;
            destWidth = (int)((float)sourceWidth / (float)sourceHeight * (float)destHeight);
      }
      else // Horizontal or square image case
      {
           destWidth = newWidth;
           destHeight = (int)((float)sourceHeight / (float)sourceWidth * (float)destWidth);
      }

      Bitmap bmPhoto = new Bitmap(destWidth, destHeight, PixelFormat.Format24bppRgb);
      bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);
      Graphics grPhoto = Graphics.FromImage(bmPhoto);
      grPhoto.Clear(Color.White);
      grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
      grPhoto.DrawImage(imgPhoto, -1, -1, destWidth + 1, destHeight + 1);
      grPhoto.Dispose();
      imgPhoto.Dispose();
      return bmPhoto;
    }
  }
}
