using System;
using System.Windows.Interactivity;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Windows.Media;

namespace Cnzk.Library.Interactivity {
    public class DeepZoomTagFilterBehavior : Behavior<MultiScaleImage> {

        protected override void OnAttached() {
            base.OnAttached();
            var a = this.AssociatedObject;
            if (a != null) {
                a.ImageOpenSucceeded += AssociatedObject_ImageOpenSucceeded;
            }
            LoadCollection();
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            var a = this.AssociatedObject;
            if (a != null) {
                a.ImageOpenSucceeded -= AssociatedObject_ImageOpenSucceeded;
            }
        }

        XElement Metadata;

        void AssociatedObject_ImageOpenSucceeded(object sender, System.Windows.RoutedEventArgs e) {
            SetImagesTags();
        }

        private void LoadCollection() {
            var msi = AssociatedObject;
            var uri = Source;
            if (msi != null && uri != null) {
                msi.Source = new DeepZoomImageTileSource(uri);
                SetImagesTags();
            }
        }

        private void SetImagesTags() {
            var msi = AssociatedObject;
            if (msi != null && msi.SubImages != null && Metadata != null) {
                var images = AssociatedObject.SubImages;
                var availableTags = new List<string>();
                var info = from t in Metadata.Descendants("Image")
                           select new {
                               Tag = t.Element("Tag").Value,
                               ZOrder = int.Parse(t.Element("ZOrder").Value) - 1
                           };
                foreach (var i in info) {
                    if (!string.IsNullOrWhiteSpace(i.Tag)) {
                        var tags = new List<string>(i.Tag.ToLowerInvariant().Split(','));
                        foreach (var t in tags) {
                            if (!availableTags.Contains(t)) availableTags.Add(t);
                        }
                        SetTags(images[i.ZOrder], tags);
                    }
                }
                AvailableTags = availableTags.ToArray();
                FilterTiles();
            }
        }

        private void FilterTiles() {
            var o = AssociatedObject;
            if (o != null && o.SubImages != null) {
                var filter = Filter;
                var images = o.SubImages;
                var visibleImages = new List<MultiScaleSubImage>();
                var anim = new Storyboard() { Duration = AnimationDuration };
                foreach (var i in images) {
                    var tags = GetTags(i);
                    var show = (string.IsNullOrWhiteSpace(filter) || ContainsAny(tags, filter));

                    CreateOpacityAnimation(anim, i, show ? 1 : 0);

                    if (show) visibleImages.Add(i);
                }
                ArrangeImages(visibleImages, anim);
            }
        }

        private bool ContainsAny(IList<string> list, string filter) {
            bool result = false;
            if (list != null && !string.IsNullOrWhiteSpace(filter)) {
                var filters = filter.ToLowerInvariant().Split(',');
                foreach (var f in filters) {
                    if (result) break;
                    result = list.Contains(f);
                }
            }
            return result;
        }

        /// <summary>
        /// Distribui as imagens na tela da melhor maneira possível para que ocupem o controle ao máximo.
        /// </summary>
        /// <param name="images">Imagens a serem arranjadas no controle</param>
        private void ArrangeImages(IList<MultiScaleSubImage> images, Storyboard anim) {
            var msi = AssociatedObject;
            if (msi != null && images != null && images.Count > 0) {
                var wmsi = msi.ActualWidth;
                var hmsi = msi.ActualHeight;
                if (images.Count > 1) {//se houver mais de 1 imagem
                    var amsi = Math.Sqrt(wmsi * wmsi + hmsi * hmsi); //calcula hipotenusa do deepzoom
                    var sw = wmsi / amsi;
                    var sh = hmsi / amsi;
                    var areamsi = wmsi * hmsi;
                    var shw = amsi * amsi * sw * sh / areamsi;

                    double totalImg = 0;
                    foreach (var i in images) {
                        totalImg += i.AspectRatio;
                        var h = 1 / i.AspectRatio;
                        i.ViewportWidth = h;
                    }

                    var aview = Math.Sqrt(shw * totalImg / (sw * sh));
                    var wview = sw * aview; //essa é a largura máxima de cada linha

                    double totalLinha = 0;
                    double linha = 0;
                    foreach (var i in images) {
                        if (totalLinha + i.ViewportWidth > wview) {
                            totalLinha = 0;
                            linha++;
                        }
                        CreatePointAnimation(anim, i, new Point(-totalLinha * i.ViewportWidth, -linha * i.ViewportWidth));
                        totalLinha += i.AspectRatio;
                    }

                    msi.ViewportWidth = wview * 1.1;
                } else {// se houver apenas 1 imagem
                    var i = images[0];

                    i.ViewportWidth = Math.Max(i.AspectRatio, 2/i.AspectRatio);
                    CreatePointAnimation(anim, i, new Point());

                    msi.ViewportWidth = 1;
                }
            }
            CreatePointAnimation(anim, msi, new Point());
            anim.Begin();
        }

        private void CreatePointAnimation(Storyboard anim, DependencyObject target, Point newPoint) {
            var panim = new PointAnimation() {
                To = newPoint,
                EasingFunction = EasingFunction
            };
            Storyboard.SetTarget(panim, target);
            Storyboard.SetTargetProperty(panim, new PropertyPath("ViewportOrigin"));
            anim.Children.Add(panim);
        }

        private void CreateOpacityAnimation(Storyboard anim, DependencyObject target, double opacity) {
            var oanim = new DoubleAnimation() {
                To = opacity,
                EasingFunction = EasingFunction
            };
            Storyboard.SetTarget(oanim, target);
            Storyboard.SetTargetProperty(oanim, new PropertyPath("Opacity"));
            anim.Children.Add(oanim);
        }

        #region DependencyProperty Source
        public Uri Source {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(DeepZoomTagFilterBehavior), new PropertyMetadata(OnSourceChanged));

        private static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var o = sender as DeepZoomTagFilterBehavior;
            o.LoadCollection();
        }
        #endregion

        #region DependencyProperty MetadataUri
        public Uri MetadataUri {
            get { return (Uri)GetValue(MetadataUriProperty); }
            set { SetValue(MetadataUriProperty, value); }
        }

        public static readonly DependencyProperty MetadataUriProperty =
            DependencyProperty.Register("MetadataUri", typeof(Uri), typeof(DeepZoomTagFilterBehavior), new PropertyMetadata(OnMetadataUriChanged));

        private static void OnMetadataUriChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var o = sender as DeepZoomTagFilterBehavior;
            var uri = e.NewValue as Uri;
            if (o != null && uri != null) {
                var client = new WebClient();
                client.DownloadStringCompleted += (sender2, e2) => {
                    if (!e2.Cancelled && e2.Error == null) {
                        try {
                            var xml = XElement.Parse(e2.Result);
                            o.Metadata = xml;
                            o.SetImagesTags();
                        } catch {
                            o.Metadata = null;
                        }
                    }
                };
                client.DownloadStringAsync(uri);
            }
        }
        #endregion

        #region DependencyProperty Filter
        public string Filter {
            get { return (string)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Filter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(string), typeof(DeepZoomTagFilterBehavior), new PropertyMetadata(OnFilterChanged));

        private static void OnFilterChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var o = sender as DeepZoomTagFilterBehavior;
            o.FilterTiles();
        }
        #endregion

        #region DependencyProperty AvailableTags
        /// <summary>
        /// List of all available Tags for this deepzoom collection.
        /// </summary>
        public string[] AvailableTags {
            get { return (string[])GetValue(AvailableTagsProperty); }
            set { SetValue(AvailableTagsProperty, value); }
        }

        public static readonly DependencyProperty AvailableTagsProperty =
            DependencyProperty.Register("AvailableTags", typeof(string[]), typeof(DeepZoomTagFilterBehavior), null);
        #endregion

        #region DependencyProperty AnimationDuration
        public TimeSpan AnimationDuration {
            get { return (TimeSpan)GetValue(AnimationDurationProperty); }
            set { SetValue(AnimationDurationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AnimationDuration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register("AnimationDuration", typeof(TimeSpan), typeof(DeepZoomTagFilterBehavior), new PropertyMetadata(TimeSpan.FromSeconds(1)));
        #endregion

        #region DependencyProperty EasingFuncion
        public IEasingFunction EasingFunction {
            get { return (IEasingFunction)GetValue(EasingFunctionProperty); }
            set { SetValue(EasingFunctionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EasingFunction.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EasingFunctionProperty =
            DependencyProperty.Register("EasingFunction", typeof(IEasingFunction), typeof(DeepZoomTagFilterBehavior), new PropertyMetadata(new CircleEase() { EasingMode = EasingMode.EaseInOut }));
        #endregion


        #region AttachedProperty Tags
        public static IList<string> GetTags(DependencyObject obj) {
            return obj.GetValue(TagsProperty) as IList<string>;
        }

        public static void SetTags(DependencyObject obj, IList<string> value) {
            obj.SetValue(TagsProperty, value);
        }

        // Using a DependencyProperty as the backing store for Tags.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TagsProperty =
            DependencyProperty.RegisterAttached("Tags", typeof(IList<string>), typeof(DeepZoomTagFilterBehavior), null);
        #endregion

    }
}