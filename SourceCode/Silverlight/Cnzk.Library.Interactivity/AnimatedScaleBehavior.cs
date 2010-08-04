using System;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace Cnzk.Library.Interactivity {
    public class AnimatedScaleBehavior : Behavior<UIElement> {

        private Storyboard anim { get; set; }
        private DoubleAnimation animX { get; set; }
        private DoubleAnimation animY { get; set; }
        
        protected override void OnAttached() {
            base.OnAttached();

            var o = AssociatedObject;
            if(o != null){
                anim = new Storyboard(){
                    Duration = Duration
                };
                animX = new DoubleAnimation() {
                    EasingFunction = Easing
                };
                animY = new DoubleAnimation() {
                    EasingFunction = Easing
                };
                anim.Children.Add(animX);
                anim.Children.Add(animY);

                var transform = o.RenderTransform;
                if (transform == null || (!(transform is CompositeTransform) && !(transform is ScaleTransform))) {
                    transform = new CompositeTransform();
                    o.RenderTransform = transform;
                }

                if (transform is CompositeTransform) {
                    Storyboard.SetTargetProperty(animX, new PropertyPath(CompositeTransform.ScaleXProperty));
                    Storyboard.SetTargetProperty(animY, new PropertyPath(CompositeTransform.ScaleYProperty));
                } else if (transform is ScaleTransform) {
                    Storyboard.SetTargetProperty(animX, new PropertyPath(ScaleTransform.ScaleXProperty));
                    Storyboard.SetTargetProperty(animY, new PropertyPath(ScaleTransform.ScaleYProperty));
                }

                Storyboard.SetTarget(anim, transform);
            }
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            anim = null;
            animX = null;
            animY = null;
        }

        #region DependencyProperty ScaleValue
        public double ScaleValue {
            get { return (double)GetValue(ScaleValueProperty); }
            set { SetValue(ScaleValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScaleValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScaleValueProperty =
            DependencyProperty.Register("ScaleValue", typeof(double), typeof(AnimatedScaleBehavior), new PropertyMetadata(1.0, OnScaleValueChanged));

        private static void OnScaleValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var o = sender as AnimatedScaleBehavior;
            var to = (double)e.NewValue;
            if (o != null && o.anim != null) {
                if (o.animX != null) o.animX.To = to;
                if (o.animY != null) o.animY.To = to;
                o.anim.Begin();
            }
        }
        #endregion

        #region DependencyProperty Easing
        public IEasingFunction Easing {
            get { return (IEasingFunction)GetValue(EasingProperty); }
            set { SetValue(EasingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Easing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EasingProperty =
            DependencyProperty.Register("Easing", typeof(IEasingFunction), typeof(AnimatedScaleBehavior), new PropertyMetadata(OnEasingChanged));

        private static void OnEasingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var o = sender as AnimatedScaleBehavior;
            var easing = e.NewValue as IEasingFunction;
            if (o != null) {
                if (o.animX != null) o.animX.EasingFunction = easing;
                if (o.animY != null) o.animY.EasingFunction = easing;
            }
        }
        #endregion

        #region DependencyProperty Duration
        public TimeSpan Duration {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Duration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(AnimatedScaleBehavior), new PropertyMetadata(TimeSpan.FromSeconds(.5), OnDurationChanged));

        private static void OnDurationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var o = sender as AnimatedScaleBehavior;
            var duration = (TimeSpan)e.NewValue;
            if (o != null && o.anim != null) {
                o.anim.Duration = duration;
            }
        }
        #endregion

    }
}
