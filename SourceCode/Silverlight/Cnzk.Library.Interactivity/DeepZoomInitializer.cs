using System;
using System.Windows.Browser;
using System.Windows.Interactivity;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;

namespace Cnzk.Library.Interactivity {
    public class DeepZoomInitializer : Behavior<MultiScaleImage> {
        double zoom = 1;
        bool duringDrag = false;
        bool mouseDown = false;
        Point lastMouseDownPos = new Point();
        Point lastMousePos = new Point();
        Point lastMouseViewPort = new Point();

        MultiScaleImage msi;

        public double ZoomFactor {
            get { return zoom; }
            set { zoom = value; }
        }

        protected override void OnAttached() {
            msi = this.AssociatedObject;

            //
            // Firing an event when the MultiScaleImage is Loaded
            //
            msi.Loaded += new RoutedEventHandler(msi_Loaded);

            //
            // Firing an event when all of the images have been Loaded
            //
            msi.ImageOpenSucceeded += new RoutedEventHandler(msi_ImageOpenSucceeded);

            //
            // Handling all of the mouse and keyboard functionality
            //
            msi.MouseMove += delegate(object sender, MouseEventArgs e) {
                lastMousePos = e.GetPosition(msi);

                if (duringDrag) {
                    Point newPoint = lastMouseViewPort;
                    newPoint.X += (lastMouseDownPos.X - lastMousePos.X) / msi.ActualWidth * msi.ViewportWidth;
                    newPoint.Y += (lastMouseDownPos.Y - lastMousePos.Y) / msi.ActualWidth * msi.ViewportWidth;
                    msi.ViewportOrigin = newPoint;
                }
            };

            msi.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) {
                lastMouseDownPos = e.GetPosition(msi);
                lastMouseViewPort = msi.ViewportOrigin;

                mouseDown = true;

                msi.CaptureMouse();
            };

            msi.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e) {
                if (!duringDrag) {
                    bool shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                    double newzoom = zoom;

                    if (shiftDown) {
                        newzoom /= 2;
                    } else {
                        newzoom *= 2;
                    }

                    Zoom(newzoom, msi.ElementToLogicalPoint(this.lastMousePos));
                }
                duringDrag = false;
                mouseDown = false;

                msi.ReleaseMouseCapture();
            };

            msi.MouseMove += delegate(object sender, MouseEventArgs e) {
                lastMousePos = e.GetPosition(msi);
                if (mouseDown && !duringDrag) {
                    duringDrag = true;
                    double w = msi.ViewportWidth;
                    Point o = new Point(msi.ViewportOrigin.X, msi.ViewportOrigin.Y);
                    msi.UseSprings = false;
                    msi.ViewportOrigin = new Point(o.X, o.Y);
                    msi.ViewportWidth = w;
                    zoom = 1 / w;
                    msi.UseSprings = true;
                }

                if (duringDrag) {
                    Point newPoint = lastMouseViewPort;
                    newPoint.X += (lastMouseDownPos.X - lastMousePos.X) / msi.ActualWidth * msi.ViewportWidth;
                    newPoint.Y += (lastMouseDownPos.Y - lastMousePos.Y) / msi.ActualWidth * msi.ViewportWidth;
                    msi.ViewportOrigin = newPoint;
                }
            };

            new MouseWheelHelper(msi).Moved += delegate(object sender, MouseWheelEventArgs e) {
                e.Handled = true;

                double newzoom = zoom;

                if (e.Delta < 0)
                    newzoom /= 1.3;
                else
                    newzoom *= 1.3;

                Zoom(newzoom, msi.ElementToLogicalPoint(this.lastMousePos));
                msi.CaptureMouse();
            };
        }

        void msi_ImageOpenSucceeded(object sender, RoutedEventArgs e) {
            //If collection, this gets you a list of all of the MultiScaleSubImages
            //
            //foreach (MultiScaleSubImage subImage in msi.SubImages)
            //{
            //    // Do something
            //}

            msi.ViewportWidth = 1;
        }

        void msi_Loaded(object sender, RoutedEventArgs e) {
            // Hook up any events you want when the image has successfully been opened
        }

        private void Zoom(double newzoom, Point p) {
            if (newzoom < 0.5) {
                newzoom = 0.5;
            }

            msi.ZoomAboutLogicalPoint(newzoom / zoom, p.X, p.Y);
            zoom = newzoom;
        }

        private void ZoomInClick(object sender, System.Windows.RoutedEventArgs e) {
            Zoom(zoom * 1.3, msi.ElementToLogicalPoint(new Point(.5 * msi.ActualWidth, .5 * msi.ActualHeight)));
        }

        private void ZoomOutClick(object sender, System.Windows.RoutedEventArgs e) {
            Zoom(zoom / 1.3, msi.ElementToLogicalPoint(new Point(.5 * msi.ActualWidth, .5 * msi.ActualHeight)));
        }

        private void GoHomeClick(object sender, System.Windows.RoutedEventArgs e) {
            msi.ViewportWidth = 1;
            msi.ViewportOrigin = new Point(0, 0);
            ZoomFactor = 1;
        }

        private void GoFullScreenClick(object sender, System.Windows.RoutedEventArgs e) {
            if (!Application.Current.Host.Content.IsFullScreen) {
                Application.Current.Host.Content.IsFullScreen = true;
            } else {
                Application.Current.Host.Content.IsFullScreen = false;
            }
        }

        // unused functions that show the inner math of Deep Zoom
        public Rect getImageRect() {
            return new Rect(-msi.ViewportOrigin.X / msi.ViewportWidth, -msi.ViewportOrigin.Y / msi.ViewportWidth, 1 / msi.ViewportWidth, 1 / msi.ViewportWidth * msi.AspectRatio);
        }

        public Rect ZoomAboutPoint(Rect img, double zAmount, Point pt) {
            return new Rect(pt.X + (img.X - pt.X) / zAmount, pt.Y + (img.Y - pt.Y) / zAmount, img.Width / zAmount, img.Height / zAmount);
        }

        public void LayoutDZI(Rect rect) {
            double ar = msi.AspectRatio;
            msi.ViewportWidth = 1 / rect.Width;
            msi.ViewportOrigin = new Point(-rect.Left / rect.Width, -rect.Top / rect.Width);
        }

        public class MouseWheelEventArgs : EventArgs {
            private double delta;
            private bool handled = false;

            public MouseWheelEventArgs(double delta) {
                this.delta = delta;
            }

            public double Delta {
                get { return this.delta; }
            }

            // Use handled to prevent the default browser behavior!
            public bool Handled {
                get { return this.handled; }
                set { this.handled = value; }
            }
        }

        public class MouseWheelHelper {
            public event EventHandler<MouseWheelEventArgs> Moved;
            private static Worker worker;
            private bool isMouseOver = false;

            public MouseWheelHelper(FrameworkElement element) {

                if (MouseWheelHelper.worker == null)
                    MouseWheelHelper.worker = new Worker();

                MouseWheelHelper.worker.Moved += this.HandleMouseWheel;

                element.MouseEnter += this.HandleMouseEnter;
                element.MouseLeave += this.HandleMouseLeave;
                element.MouseMove += this.HandleMouseMove;
            }

            private void HandleMouseWheel(object sender, MouseWheelEventArgs args) {
                if (this.isMouseOver)
                    this.Moved(this, args);
            }

            private void HandleMouseEnter(object sender, EventArgs e) {
                this.isMouseOver = true;
            }

            private void HandleMouseLeave(object sender, EventArgs e) {
                this.isMouseOver = false;
            }

            private void HandleMouseMove(object sender, EventArgs e) {
                this.isMouseOver = true;
            }

            private class Worker {

                public event EventHandler<MouseWheelEventArgs> Moved;

                public Worker() {

                    if (HtmlPage.IsEnabled) {
                        HtmlPage.Window.AttachEvent("DOMMouseScroll", this.HandleMouseWheel);
                        HtmlPage.Window.AttachEvent("onmousewheel", this.HandleMouseWheel);
                        HtmlPage.Document.AttachEvent("onmousewheel", this.HandleMouseWheel);
                    }

                }

                private void HandleMouseWheel(object sender, HtmlEventArgs args) {
                    double delta = 0;

                    ScriptObject eventObj = args.EventObject;

                    if (eventObj.GetProperty("wheelDelta") != null) {
                        delta = ((double)eventObj.GetProperty("wheelDelta")) / 120;


                        if (HtmlPage.Window.GetProperty("opera") != null)
                            delta = -delta;
                    } else if (eventObj.GetProperty("detail") != null) {
                        delta = -((double)eventObj.GetProperty("detail")) / 3;

                        if (HtmlPage.BrowserInformation.UserAgent.IndexOf("Macintosh") != -1)
                            delta = delta * 3;
                    }

                    if (delta != 0 && this.Moved != null) {
                        MouseWheelEventArgs wheelArgs = new MouseWheelEventArgs(delta);
                        this.Moved(this, wheelArgs);

                        if (wheelArgs.Handled)
                            args.PreventDefault();
                    }
                }
            }
        }
    }
}
