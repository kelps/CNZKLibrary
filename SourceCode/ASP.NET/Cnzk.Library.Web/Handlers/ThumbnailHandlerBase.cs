using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Web;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing.Drawing2D;
using Cnzk.Library.Web.Handlers.ImageQuantizers;
using System.Configuration;

namespace Cnzk.Library.Web.Handlers {
    public abstract class ThumbnailHandlerBase : IHttpHandler {

        /// <summary>
        /// Gets the url regular expression pattern used to validate and parse the requested url.
        /// </summary>
        protected abstract string UrlValidationPattern { get; }

        /// <summary>
        /// Gets the original image. If not found, return null.
        /// </summary>
        /// <param name="context">HttpContext of the current request.</param>
        /// <returns>Image object containg the original image or null, if no image found.</returns>
        protected abstract Image GetOriginalImage(HttpContext context);

        /// <summary>
        /// Gets the requested size for the output image.
        /// </summary>
        /// <param name="context">HttpContext of the current request.</param>
        /// <returns>Requested size for the output image.</returns>
        protected abstract Size GetRequestedSize(HttpContext context);

        /// <summary>
        /// Gets the content type for the output file.
        /// </summary>
        /// <param name="context">HttpContext of the current request.</param>
        /// <returns>Content type for the output file.</returns>
        protected abstract ImageFormat GetOriginalImageFormat(HttpContext context);

        #region IHttpHandler Members

        /// <summary>
        /// Implementation of IHttpHandler "IsReusable" property.
        /// </summary>
        /// <value>The return value is "true" because this handler is stateless.</value>
        public bool IsReusable {
            get { return true; }
        }

        /// <summary>
        /// Implemetation of IHttpHandler "ProcessRequest" method.
        /// </summary>
        /// <param name="context">HttpContext of the current request.</param>
        public void ProcessRequest(HttpContext context) {
            Image img;
            Image finalImage;
            bool originalImageExists = false;

            if (ValidateUrl(context)) {
                try {
                    img = GetOriginalImage(context);
                    originalImageExists = true;
                    if (img == null) {
                        img = GetNotFoundImage(context);
                    }
                } catch {
                    img = GetNotFoundImage(context);
                }

                if (img != null) {
                    using (img) {
                        Size requestedSize = GetRequestedSize(context);
                        finalImage = GetFinalImage(context, img, requestedSize);
                        using (finalImage) {
                            SetFileDependency(context);
                            SetCache(context, originalImageExists);
                            SetOutput(context, finalImage);
                        }
                    }
                }
            }
        }

        #endregion


        #region Method ValidateUrl(HttpContext)
        /// <summary>
        /// Validate if the requested url is correct.
        /// </summary>
        /// <param name="context">Context of the current request.</param>
        /// <returns>Value indicating if the request url is valid.</returns>
        protected virtual bool ValidateUrl(HttpContext context) {
            bool result = false;
            result = ExecuteRegEx(UrlValidationPattern, context.Request.CurrentExecutionFilePath).Success;
            return result;
        }
        #endregion

        #region Method SetOutput(HttpContext, Image)
        protected virtual void SetOutput(HttpContext context, Image image) {
            ImageFormat originalImageFormat = GetOriginalImageFormat(context);
            ImageFormat outputImageFormat = GetOutputImageFormat(originalImageFormat);
            string outputContentType = GetOutputContentType(outputImageFormat);
            context.Response.ContentType = outputContentType;

            MemoryStream img = new MemoryStream();
            image.Save(img, outputImageFormat);
            img.WriteTo(context.Response.OutputStream);
        }
        #endregion

        #region Method GetOutputContentType(ImageFormat)
        protected virtual string GetOutputContentType(ImageFormat imageFormat) {
            string result = string.Empty;

            if (imageFormat == ImageFormat.Gif) {
                result = "image/gif";
            } else if (imageFormat == ImageFormat.Png) {
                result = "image/png";
            } else {
                result = "image/jpeg";
            }

            return result;
        }
        #endregion

        #region Method GetOutputImageFormat(ImageFormat)
        private ImageFormat GetOutputImageFormat(ImageFormat imageFormat) {
            ImageFormat result = imageFormat;
            if (result != ImageFormat.Gif && result != ImageFormat.Png) result = ImageFormat.Jpeg;
            return result;
        }
        #endregion

        #region Method GetNotFoundImage(HttpContext)
        protected virtual Image GetNotFoundImage(HttpContext context) {
            Color bgcolor = GetBackgroundColor(context);
            Color fgcolor = GetForegroundColor(context);
            string txt = GetNotFoundText(context);
            Image img = null;

            //Verifica se existe imagem padrão configurada
            if (DefaultNotFoundImage != null) {
                try {
                    //tenta carregar a imagem padrão
                    img = Image.FromFile(DefaultNotFoundImage);
                } catch { }
            }

            if (img == null) {
                img = new Bitmap(300, 300);
                Graphics g = Graphics.FromImage(img);

                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                StringFormat f = new StringFormat();
                f.Alignment = StringAlignment.Center;

                g.Clear(bgcolor); //pinta o fundo da imagem
                g.DrawString(txt, new Font("Verdana", 24, FontStyle.Bold), new SolidBrush(fgcolor), new Rectangle(10, 100, 280, 280), f);
            }

            return img;
        }
        #endregion

        #region Method GetBackgroundColor(HttpContext)
        protected virtual Color GetBackgroundColor(HttpContext context) {
            Color result = DefaultBackgroundColor;
            string colorParam = context.Request["bg"];
            TranslateColor(colorParam, ref result);
            return result;
        }
        #endregion

        #region Method GetForegroundColor(HttpContext)
        protected virtual Color GetForegroundColor(HttpContext context) {
            Color result = DefaultForegroundColor;
            string colorParam = context.Request["fg"];
            TranslateColor(colorParam, ref result);
            return result;
        }
        #endregion

        #region Method GetNotFoundText(HttpContext)
        protected virtual string GetNotFoundText(HttpContext context) {
            string txt = context.Request["txt"];
            if (txt == null) txt = DefaultNotFoundText;
            return txt;
        }
        #endregion

        #region Method GetFitInsideMode
        /// <summary>
        /// Obtém o valor da opção FitInside para a imagem. Se for passada a propriedade "inside",
        /// ela será utilizada. Caso contrário, será utilizado o valor padrão.
        /// </summary>
        protected virtual bool GetFitInsideMode(HttpContext context) {
            string param = context.Request["inside"];
            bool result = DefaultFitInsideMode;
            if (!string.IsNullOrEmpty(param)) {
                bool.TryParse(param, out result);
            }
            return result;
        }
        #endregion

        #region Method GetMaskColor(HttpContext)
        protected virtual Color GetMaskColor(HttpContext context) {
            Color result = Color.Transparent;
            string colorParam = context.Request["mask"];
            if (colorParam != null && colorParam.Length == 8) {//só aceita cores com a informação de alpha.
                TranslateColor(colorParam, ref result);
            }
            return result;
        }
        #endregion

        #region Method GetFinalImage(HttpContext, Image, Size)
        protected virtual Image GetFinalImage(HttpContext context, Image original, Size requestedSize) {
            Image result = null;

            if (original.Size != requestedSize) {
                bool fitInsideMode = GetFitInsideMode(context);
                Size proportionalSize = GetProportionalSize(original.Size, requestedSize, fitInsideMode);
                result = new Bitmap(requestedSize.Width, requestedSize.Height, PixelFormat.Format32bppArgb);

                Point p = GetStartPosition(requestedSize, proportionalSize);
                Graphics g = Graphics.FromImage(result);

                g.PageUnit = GraphicsUnit.Pixel;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;

                Color bgcolor = GetBackgroundColor(context);
                g.Clear(bgcolor);
                g.DrawImage(original, p.X, p.Y, proportionalSize.Width, proportionalSize.Height);

                PaintMask(context, result, proportionalSize, p);

                //Se for gif, converte para 8 bits
                if (original.PixelFormat == PixelFormat.Format8bppIndexed ||
                    original.PixelFormat == PixelFormat.Format4bppIndexed ||
                    original.PixelFormat == PixelFormat.Format1bppIndexed) {
                    result = ConvertPixelFormatAndPalette(original as Bitmap, result);
                }

                g.Dispose();
            } else {
                result = original;
            }

            return result;
        }
        #endregion

        #region Method CopyPalette(Bitmap, Bitmap)
        /// <summary>
        /// Creates a new image using the same PixelFormat and Palette of the original image and copy the
        /// generated image into this new image.
        /// </summary>
        protected virtual Image ConvertPixelFormatAndPalette(Bitmap original, Image generated) {
            OctreeQuantizer quantizer = new OctreeQuantizer(255, 8);
            Image result = quantizer.Quantize(generated);
            return result;
        }
        #endregion

        #region Method PaintMask(HttpContext, Image, Size, Point)
        protected virtual void PaintMask(HttpContext context, Image img, Size maskSize, Point startPosition) {
            Graphics g = Graphics.FromImage(img);
            Color maskColor = GetMaskColor(context);
            Brush b = new SolidBrush(maskColor);
            g.FillRectangle(b, startPosition.X, startPosition.Y, maskSize.Width, maskSize.Height);
        }
        #endregion

        #region Method GetStartPosition(Size, Size)
        protected virtual Point GetStartPosition(Size requested, Size proportional) {
            Point result = new Point();

            result.X = (requested.Width == proportional.Width) ? 0 : (int)((requested.Width - proportional.Width) / 2);
            result.Y = (requested.Height == proportional.Height) ? 0 : (int)((requested.Height - proportional.Height) / 2);

            return result;
        }
        #endregion

        #region Method GetProportionalSize(Size, Size, bool)
        protected virtual Size GetProportionalSize(Size original, Size requested, bool fitInsideMode) {
            Size result = requested;
            double propW = (double)requested.Width / original.Width,   //proporção da largura
                   propH = (double)requested.Height / original.Height; //proporção da altura

            double newProp;

            // se a proporção da altura for diferente da proporção da largura significa
            // que a imagem não é proporcional ao tamanho solicitado.
            if (propH != propW) {
                if (fitInsideMode) { //redimencionamento por dentro
                    newProp = (propW < propH) ? propW : propH; //escolhe a menor das proporções
                } else { //redimensionamento por fora
                    newProp = (propW > propH) ? propW : propH; //escolhe a maior das proporções
                }

                //calculo das novas dimensões. mantém a nova imagem proporcional à original.
                result.Width = (int)(original.Width * newProp);   //multiplica a largura original pela proporção correta
                result.Height = (int)(original.Height * newProp); //multiplica a altura original pela proporção correta
            }

            return result;
        }
        #endregion

        #region Method SetCache(HttpContext, bool)
        protected virtual void SetCache(HttpContext context, bool originalFileExists) {
            context.Response.Cache.SetCacheability(HttpCacheability.ServerAndNoCache);
            if (originalFileExists) {
                context.Response.Cache.SetExpires(DateTime.Now.AddMinutes(30));
            } else {
                context.Response.Cache.SetExpires(DateTime.Now.AddMinutes(2));
            }
        }
        #endregion

        #region Method SetFileDependency(HttpContext)
        protected virtual void SetFileDependency(HttpContext context) { }
        #endregion


        #region Property DefaultNotFoundText
        private string defaultNotFoundText;
        /// <summary>
        /// Texto padrão para ser exibido para imagem não encontrada. Esse texto só será utilizado
        /// caso não haja uma imagem padrão. Se esse texto não for definido, será utilizado o texto padrão:
        /// "Imagem Não Disponível".
        /// </summary>
        protected virtual string DefaultNotFoundText {
            get {
                if (defaultNotFoundText == null) {
                    defaultNotFoundText = ConfigurationManager.AppSettings["ThumbnailHandler.DefaultNotFoundText"];
                    if (defaultNotFoundText == null) {
                        defaultNotFoundText = "Imagem Não Disponível";
                    }
                }
                return defaultNotFoundText;
            }
        }
        #endregion

        #region Property DefaultBackgroundColor
        private Color? defaultBackgroundColor = null;
        /// <summary>
        /// Cor de fundo padrão da imagem. Essa é a cor que aparecerá quando a imagem dimensionada
        /// não tiver as mesmas proporções ou no fundo da imagem não disponível. Se não for definida,
        /// será utilizada a cor padrão (Transparente para gif ou png e Branco para jpg). Esse valor 
        /// pode ser sobrescrito pela propriedade "bg" que pode ser passada na url da imagem.
        /// </summary>
        protected virtual Color DefaultBackgroundColor {
            get {
                if (defaultBackgroundColor == null) {
                    ImageFormat format = GetOriginalImageFormat(HttpContext.Current);
                    Color aux = (format == ImageFormat.Png || format == ImageFormat.Gif) ? Color.Transparent : Color.White;
                    string colorParam = ConfigurationManager.AppSettings["ThumbnailHandler.DefaultBackgroundColor"];
                    TranslateColor(colorParam, ref aux);
                    defaultBackgroundColor = aux;
                }
                return defaultBackgroundColor.Value;
            }
        }
        #endregion

        #region Property DefaultForegroundColor
        private Color? defaultForegroundColor = null;
        /// <summary>
        /// Cor de padrão de primeiro plano da imagem. Essa é a cor do texto quando a imagem não for
        /// encontrada e não houver imagem padrão para esse caso. Se esse parâmetro não for definido,
        /// será utilizada a cor padrão (Vermelho). Esse valor pode ser sobrescrito pela propriedade
        /// "fg" que pode ser passada na url da imagem.
        /// </summary>
        protected virtual Color DefaultForegroundColor {
            get {
                if (defaultForegroundColor == null) {
                    Color aux = Color.Red;
                    string colorParam = ConfigurationManager.AppSettings["ThumbnailHandler.DefaultForegroundColor"];
                    TranslateColor(colorParam, ref aux);
                    defaultForegroundColor = aux;
                }
                return defaultForegroundColor.Value;
            }
        }
        #endregion

        #region Property DefaultNotFoundImage
        /// <summary>
        /// Imagem padrão para ser exibida para imagens não encontradas.
        /// </summary>
        private static string DefaultNotFoundImage {
            get {
                string fileName = ConfigurationManager.AppSettings["ThumbnailHandler.DefaultNotFoundImage"];
                fileName = HttpContext.Current.Server.MapPath(fileName);
                if (!File.Exists(fileName)) {
                    fileName = null;
                }
                return fileName;
            }
        }
        #endregion

        #region Property DefaultFitInsideMode
        private bool? defaultFitInsideMode;
        /// <summary>
        /// Propriedade que define o método de redimensionamento da imagem. Define se uma imagem de proporção 
        /// diferente da solicitada deve ser redimensionada para caber dentro das dimensões solicitadas (exibindo
        /// tarjas nas laterais ou na parte superior e inferior) ou se deve ser cortada a parte desproporcional.
        /// O valor padrão é "true", que significa que a imagem será redimensionada para caber nas dimensões 
        /// solicitadas. Esse valor pode ser sobrescrito pela propriedade "inside" que pode ser passada
        /// na url da image.
        /// </summary>
        protected virtual bool DefaultFitInsideMode {
            get {
                if (defaultFitInsideMode == null) {
                    bool result = true;
                    string param = ConfigurationManager.AppSettings["ThumbnailHandler.DefaultFitInsideMode"];
                    if (!string.IsNullOrEmpty(param)) {
                        bool.TryParse(param, out result);
                    }
                    defaultFitInsideMode = result;
                }
                return defaultFitInsideMode.Value;
            }
        }
        #endregion


        #region Helper Method ExecuteRegEx
        /// <summary>
        /// Helper method for executing regular expressions.
        /// </summary>
        /// <param name="re">String containing the regular expression.</param>
        /// <param name="text">String containing the text to execute the regular expression on.</param>
        /// <returns>Result from the regular expression match.</returns>
        protected virtual Match ExecuteRegEx(string re, string text) {
            RegexOptions opt = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline;
            Regex r = new Regex(re, opt);
            Match m = r.Match(text);
            return m;
        }
        #endregion

        #region Helper Method TranslateColor
        /// <summary>
        /// Translates a RGB Hexadecimal color to System.Drawing.Color. The color can have
        /// 6 or 8 characters. If it has 8 characters, the first 2 are the transparence (Alpha)
        /// or the color.
        /// </summary>
        /// <param name="hexColor">Hexadecimal color in [FF]FFFFFF format.</param>
        /// <param name="output">Color generated from the hexadecimal.</param>
        /// <returns>Informs if the requested color translation succeded.</returns>
        protected virtual bool TranslateColor(string hexColor, ref Color output) {
            int a = 255, r, g, b; //integer representation of each color and alpha
            string xA, xR, xG, xB; //string representation of each color and alpha
            int len = 0;

            if (hexColor == null || (hexColor.Length != 6 && hexColor.Length != 8)) {
                return false;
            } else {
                len = hexColor.Length;
                if (len == 8) {
                    xA = hexColor.Substring(0, 2); //string for the Alpha or the color
                    a = int.Parse(xA, NumberStyles.HexNumber, CultureInfo.InvariantCulture); //integer for the Alpha of the color
                }
                xR = hexColor.Substring(len - 6, 2); //string for the Red of the color
                xG = hexColor.Substring(len - 4, 2); //string for the Green of the color
                xB = hexColor.Substring(len - 2, 2); //string for the Blue of the color

                r = int.Parse(xR, NumberStyles.HexNumber, CultureInfo.InvariantCulture); //integer for the Red of the color
                g = int.Parse(xG, NumberStyles.HexNumber, CultureInfo.InvariantCulture); //integer for the Green of the color
                b = int.Parse(xB, NumberStyles.HexNumber, CultureInfo.InvariantCulture); //integer for the Blue of the color

                output = Color.FromArgb(a, r, g, b); //resulting color.
                return true;
            }
        }
        #endregion
    }
}
