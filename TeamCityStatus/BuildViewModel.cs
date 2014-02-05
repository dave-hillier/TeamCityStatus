using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Windows.Threading;

namespace TeamCityStatus
{
    class BuildStepViewModel : PropertyNotifyBase
    {
        private BuildStatus _status;
        private string _message;

        public BuildStepViewModel(IObservable<string> users, Dispatcher dispatcher)
        {
            ChangeMessage = "";
            users.ObserveOn(dispatcher).Subscribe(s =>
            {
                if (!ChangeMessage.Contains(s))
                {
                    ChangeMessage += " " + s;
                }
            });
        }

        public string BuildTypeId { get; set; }

        public string BuildName { get; set; }

        public string ChangeMessage
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                OnPropertyChanged("ChangeMessage");
            }
        }

        public BuildStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
                OnPropertyChanged("StatusColor");
            }
        }

        // TODO: main color?
        // TODO: use converter?
        public Brush StatusColor {
            get
            {
                switch (Status)
                {
                    case BuildStatus.Success:
                        return new SolidColorBrush(Colors.Green);
                    case BuildStatus.Error:
                    case BuildStatus.Failure:
                        return new SolidColorBrush(Colors.Red);
                    default:
                        return new SolidColorBrush(Colors.Gray);
                }
            }
        }

        public string Project { get; set; }
    }
}
