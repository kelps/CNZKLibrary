using System;
using System.Windows.Interactivity;
using System.Windows;
using System.Windows.Input;
using System.Reflection;

namespace Cnzk.Library.Interactivity {
    public class RelayCommandAction : TriggerAction<DependencyObject> {
        // Fields
        private string commandName;
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter", typeof(object), typeof(RelayCommandAction), null);
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(RelayCommandAction), null);
        public static readonly DependencyProperty UseEventArgsAsParameterProperty = DependencyProperty.Register("UseEventArgsAsParameter", typeof(bool), typeof(RelayCommandAction), new PropertyMetadata(true));

        // Methods
        protected override void Invoke(object parameter) {
            if (base.AssociatedObject != null) {
                ICommand command = this.ResolveCommand();
                object param = UseEventArgsAsParameter ? parameter : this.CommandParameter;
                if (command != null && command.CanExecute(param)) {
                    command.Execute(param);
                }
            }
        }

        private ICommand ResolveCommand() {
            ICommand command = null;
            if (this.Command != null) {
                return this.Command;
            }
            if (base.AssociatedObject != null) {
                foreach (PropertyInfo info in base.AssociatedObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                    if (typeof(ICommand).IsAssignableFrom(info.PropertyType) && string.Equals(info.Name, this.CommandName, StringComparison.Ordinal)) {
                        command = (ICommand)info.GetValue(base.AssociatedObject, null);
                    }
                }
            }
            return command;
        }

        // Properties
        public ICommand Command {
            get {
                return (ICommand)base.GetValue(CommandProperty);
            }
            set {
                base.SetValue(CommandProperty, value);
            }
        }

        public string CommandName {
            get {
                return this.commandName;
            }
            set {
                if (this.CommandName != value) {
                    this.commandName = value;
                }
            }
        }

        public object CommandParameter {
            get {
                return base.GetValue(CommandParameterProperty);
            }
            set {
                base.SetValue(CommandParameterProperty, value);
            }
        }

        public bool UseEventArgsAsParameter {
            get {
                return (bool)base.GetValue(UseEventArgsAsParameterProperty);
            }
            set {
                base.SetValue(UseEventArgsAsParameterProperty, value);
            }
        }
    }
}