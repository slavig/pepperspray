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

      var image = this.resizeImage(originalImage.Clone() as Image, 1024, 1024);
      image.Save(this.filePath(hash), ImageFormat.Jpeg);

      var thumbnail = this.resizeImage(originalImage, 250, 250);
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

      if (sourceWidth <= newWidth)
      {
        newWidth = sourceWidth;
      }

      if (sourceHeight <= newHeight)
      {
        newHeight = sourceHeight;
      }

      //Consider vertical pics
      if (sourceWidth < sourceHeight)
      {
        int buff = newWidth;

        newWidth = newHeight;
        newHeight = buff;
      }

      int sourceX = 0, sourceY = 0, destX = 0, destY = 0;
      float nPercent = 0, nPercentW = 0, nPercentH = 0;

      nPercentW = ((float)newWidth / (float)sourceWidth);
      nPercentH = ((float)newHeight / (float)sourceHeight);
      if (nPercentH < nPercentW)
      {
        nPercent = nPercentH;
        destX = System.Convert.ToInt16((newWidth -
                  (sourceWidth * nPercent)) / 2);
      }
      else
      {
        nPercent = nPercentW;
        destY = System.Convert.ToInt16((newHeight -
                  (sourceHeight * nPercent)) / 2);
      }

      int destWidth = (int)(sourceWidth * nPercent);
      int destHeight = (int)(sourceHeight * nPercent);


      Bitmap bmPhoto = new Bitmap(newWidth, newHeight,
                    PixelFormat.Format24bppRgb);

      bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                   imgPhoto.VerticalResolution);

      Graphics grPhoto = Graphics.FromImage(bmPhoto);
      grPhoto.Clear(Color.Black);
      grPhoto.InterpolationMode =
          System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

      grPhoto.DrawImage(imgPhoto,
          new Rectangle(destX, destY, destWidth, destHeight),
          new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
          GraphicsUnit.Pixel);

      grPhoto.Dispose();
      imgPhoto.Dispose();
      return bmPhoto;
    }
  }
}
