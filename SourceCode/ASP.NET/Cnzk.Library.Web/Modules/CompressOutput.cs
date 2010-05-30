using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using System.IO.Compression;
using System.Configuration;
using System.Globalization;

namespace Cnzk.Library.Web.Modules {
    public class CompressOutput : IHttpModule {

        #region IHttpModule Members

        public void Dispose() {

        }

        public void Init(HttpApplication context) {
            context.BeginRequest += new EventHandler(context_BeginRequest);
        }

        private static bool Enabled {
            get {
                bool result = true;
                string aux = ConfigurationManager.AppSettings["CrompressOutput.Enabled"];

                if (!string.IsNullOrEmpty(aux)) {
                    bool.TryParse(aux, out result);
                }

                return result;
            }
        }

        void context_BeginRequest(object sender, EventArgs e) {
            HttpApplication app = sender as HttpApplication;
            string ext = Path.GetExtension(app.Request.CurrentExecutionFilePath);

            if (Enabled && !string.IsNullOrEmpty(ext) && app.Response.ContentType.Contains("text") && (
                ext.Equals(".aspx", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".asmx", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".ashx", StringComparison.OrdinalIgnoreCase))) {

                string acceptEncoding = app.Request.Headers["Accept-Encoding"];
                Stream prevUncompressedStream = app.Response.Filter;

                if (acceptEncoding == null || acceptEncoding.Length == 0)
                    return;

                acceptEncoding = acceptEncoding.ToUpperInvariant();

                if (acceptEncoding.Contains("GZIP")) {
                    // gzip
                    app.Response.Filter = new GZipStream(prevUncompressedStream, CompressionMode.Compress);
                    app.Response.AppendHeader("Content-Encoding", "gzip");
                } else if (acceptEncoding.Contains("DEFLATE")) {
                    // defalte
                    app.Response.Filter = new DeflateStream(prevUncompressedStream, CompressionMode.Compress);
                    app.Response.AppendHeader("Content-Encoding", "deflate");
                }
            }
        }

        #endregion
    }
}
