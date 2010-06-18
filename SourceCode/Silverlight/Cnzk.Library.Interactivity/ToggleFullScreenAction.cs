using System;
using System.Windows.Interactivity;
using System.Windows;

namespace Cnzk.Library.Interactivity {
    public class ToggleFullScreenAction : TriggerAction<DependencyObject> {

        public ToggleFullScreenAction() {
            Application.Current.Host.Content.FullScreenChanged += new EventHandler(Content_FullScreenChanged);
        }

        protected override void Invoke(object parameter) {
            Application.Current.Host.Content.IsFullScreen = !Application.Current.Host.Content.IsFullScreen;
        }

        void Content_FullScreenChanged(object sender, EventArgs e) {
            IsFullScreen = Application.Current.Host.Content.IsFullScreen;
        }

        #region DependencyProperty IsFullScreen
        public bool IsFullScreen {
            get { return (bool)GetValue(IsFullScreenProperty); }
            set { SetValue(IsFullScreenProperty, value); }
        }

        public static readonly DependencyProperty IsFullScreenProperty =
            DependencyProperty.Register("IsFullScreen", typeof(bool), typeof(ToggleFullScreenAction), new PropertyMetadata(OnIsFullScreenChanged));

        private static void OnIsFullScreenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var nv = (bool)e.NewValue;
            if (nv != Application.Current.Host.Content.IsFullScreen) {
                Application.Current.Host.Content.IsFullScreen = nv;
            }
        }
        #endregion

    }
}
