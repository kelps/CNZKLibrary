using System;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows;

namespace Cnzk.Library.Interactivity {
    public class MouseWheelScale : Behavior<UIElement> {

        private CompositeTransform Transform { get; set; }

        protected override void OnAttached() {
            base.OnAttached();

            Transform = new CompositeTransform();
            this.AssociatedObject.RenderTransform = Transform;
            this.AssociatedObject.MouseWheel += new System.Windows.Input.MouseWheelEventHandler(AssociatedObject_MouseWheel);
        }

        protected override void OnDetaching() {
            base.OnDetaching();
        }

        void AssociatedObject_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
            double x = e.Delta / 100.0;

        }

    }
}
