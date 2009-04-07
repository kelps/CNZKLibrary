using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Web;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Cnzk.Library.Web.Handlers {
    /// <summary>HttpHandler for generating in-memory thumbnail images.</summary>
    /// <remarks>
    /// <para>This class is responsible for generating image thumbnails by resizing imagens to a desired size. This is accomplished by
    /// hooking-up the .thumb.axd file extension to this class.</para>
    /// <para>To resize a image, all that is needed is to append the suffix {width}x{height}.thumb.axd (where {width} and {height} must
    /// be replaced by the desired dimensions. </para>
    /// <para>The new image is generated only in memory and stays in cache for 30 minutes. The cache is vinculated to the original image, so
    /// if that image changes, the cache also expires.</para>
    /// <para>If the desired proportions are diferent from the original proportions, the image will be resized using the original proportions,
    /// so it will not be distorted. It will be resized to fit the desired size.</para>
    /// <para>It is possible to change some aspects of the generated image by passing some parameters to the handler. This parameters are described below.</para>
    /// <para>
    ///     <list type="bullet">
    ///         <item><b>bg</b> : Background color for the new image, represented in a 6 position hexadecimal format. This will be the color of the background when the resized image is of a diferent proportion of the original image.</item>
    ///         <item><b>fg</b> : Foreground color for the new image, represented in a 6 position hexadecimal format. This will be the color of the text in a "file not found" scenario.</item>
    ///         <item><b>txt</b> : Text for the "File not found" scenario.</item>
    ///         <item><b>inside</b> : Resizing method. This will change the behavior of the resizing algorithm. The default value is true. When inside = "false", the resized image will fill the entire new canvas and if it is of a diferent proportion, some parts are going to be cut off.</item>
    ///         <item><b>mask</b> : Overlay colored mask. ARGB represented in a 8 characters hexadecimal text. The first 2 characters represents the opacity and the other 6 are a tradicional HTML color.</item>
    ///     </list></para>
    /// </remarks>
    /// <example>
    ///     <code lang="xml" title="Configuring the handler">
    /// 	    <system.web>
    ///             <httpHandlers>
    ///                 <add verb="*" path="*.thumb.axd" type="Cnzk.Web.Handlers.ThumbnailHandler, Cnzk.Web"/>
    ///             </httpHandlers>
    /// 	    </system.web>
    ///     </code>
    ///     <code lang="xml" title="Example of thumbnail"><img src="img/photo.jpg.200x150.thumb.axd" /></code>
    ///     <code lang="xml" title="Example of thumbnail with diferent background color"><img src="img/photo.jpg.200x150.thumb.axd?bg=000000" /></code>
    ///     <code lang="xml" title="Configuration of default global values (all optional)">
    /// 	    <appSettings>
    ///             <add key="ThumbnailHandler.DefaultNotFoundImage" value="~/img/nao-disponivel.jpg"/>
    ///             <add key="ThumbnailHandler.DefaultNotFoundText" value="imagem não disponivel"/>
    ///             <add key="ThumbnailHandler.DefaultBackgroundColor" value="000000"/>
    ///             <add key="ThumbnailHandler.DefaultForegroundColor" value="00FF00"/>
    ///             <add key="ThumbnailHandler.DefaultFitInsideMode" value="true"/>
    /// 	    </appSettings>
    ///     </code>
    /// </example>
    public class ThumbnailHandler : ThumbnailHandlerBase {

        #region Property UrlPattern
        private const string urlPattern = @"(?<url>.*\.((jpg)|(jpeg)|(gif)|(bmp)|(png)|(tif)|(tiff)))\.(?<w>[0-9]+)x(?<h>[0-9]+)\.thumb\.axd";

        /// <summary>
        /// Gets the url regular expression pattern used to validate and parse the requested url.
        /// </summary>
        protected override string UrlValidationPattern {
            get { return urlPattern; }
        }
        #endregion

        #region Method GetOriginalImage(HttpContext)
        /// <summary>
        /// Gets the original image. If not found, return null.
        /// </summary>
        /// <param name="context">HttpContext of the current request.</param>
        /// <returns>Image object containg the original image or null, if no image found.</returns>
        protected override Image GetOriginalImage(HttpContext context) {
            Image result = null;
            result = Image.FromFile(GetOriginalFileName(context));
            return result;
        }
        #endregion

        #region Method GetRequestedSize(HttpContext)
        protected override Size GetRequestedSize(HttpContext context) {
            Size result = new Size();
            string requestedFileName = Path.GetFileName(context.Request.CurrentExecutionFilePath);
            Match m = ExecuteRegEx(UrlValidationPattern, requestedFileName);
            if (m.Success) {
                result.Width = Int32.Parse(m.Groups["w"].Value, CultureInfo.InvariantCulture);
                result.Height = Int32.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture);
            }
            return result;
        }
        #endregion

        #region Method GetOriginalImageFormat(HttpContext)
        protected override ImageFormat GetOriginalImageFormat(HttpContext context) {
            string fileExtension = Path.GetExtension(GetOriginalFileName(context)).ToUpperInvariant();
            ImageFormat result;
            switch (fileExtension) {
                case ".GIF":
                    result = ImageFormat.Gif;
                    break;
                case ".PNG":
                    result = ImageFormat.Png;
                    break;
                default:
                    result = ImageFormat.Jpeg;
                    break;
            }
            return result;
        }
        #endregion


        #region Method SetFileDependency(HttpContext)
        protected override void SetFileDependency(HttpContext context) {
            string fileName = GetOriginalFileName(context);
            if (File.Exists(fileName)) {
                context.Response.AddFileDependency(fileName);
            }
        }
        #endregion


        #region Method GetOriginalFileName(HttpContext)
        /// <summary>
        /// Gets the real path of the original requested file.
        /// </summary>
        /// <param name="context">HttpContext of the current request.</param>
        /// <returns>Real path of the original requested file.</returns>
        protected virtual string GetOriginalFileName(HttpContext context) {
            string result = null;
            string filePath = Path.GetDirectoryName(context.Server.MapPath(context.Request.CurrentExecutionFilePath));
            string requestedFileName = Path.GetFileName(context.Request.CurrentExecutionFilePath);
            string fileName;
            Match m = ExecuteRegEx(UrlValidationPattern, requestedFileName);
            if (m.Success) {
                fileName = m.Groups["url"].Value;
                result = Path.Combine(filePath, fileName);
            }
            return result;
        }
        #endregion
    }
}
