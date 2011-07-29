using System.Windows.Interactivity;
using System.Windows;
using System;
using System.Windows.Input;

namespace Cnzk.Library.Interactivity {
    public class ClickCountTrigger : TriggerBase<UIElement> {
        DateTime lastClick;
        int count = 0;
        int ClickInterval = 400;

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(AssociatedObject_MouseLeftButtonUp), true);
        }

        void AssociatedObject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            var now = DateTime.Now;
            if (now.Subtract(lastClick).TotalMilliseconds < ClickInterval) {
                count++;
                lastClick = now;
                if (count >= ClickCount) {
                    count = 0;
                    InvokeActions(e);
                }
            } else {
                count = 1;
                lastClick = now;
            }
        }

        protected override void OnDetaching() {
            AssociatedObject.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            base.OnDetaching();
        }

        #region DependencyProperty ClickCount

        public int ClickCount {
            get { return (int)GetValue(ClickCountProperty); }
            set { SetValue(ClickCountProperty, value); }
        }

        public static readonly DependencyProperty ClickCountProperty =
            DependencyProperty.Register("ClickCount", typeof(int), typeof(ClickCountTrigger), new PropertyMetadata(2));

        #endregion

    }
}
