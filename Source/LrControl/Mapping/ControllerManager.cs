﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using micdah.LrControl.Annotations;
using Midi.Devices;

namespace micdah.LrControl.Mapping
{
    public class ControllerManager : INotifyPropertyChanged
    {
        private ObservableCollection<Controller> _controllers;

        public ControllerManager()
        {
            Controllers = new ObservableCollection<Controller>();
        }

        public ControllerManager(IEnumerable<Controller> controllers)
        {
            Controllers = new ObservableCollection<Controller>(controllers);
        }

        public ObservableCollection<Controller> Controllers
        {
            get { return _controllers; }
            private set
            {
                if (Equals(value, _controllers)) return;
                _controllers = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetInputDevice(IInputDevice inputDevice)
        {
            foreach (var controller in Controllers)
            {
                controller.SetInputDevice(inputDevice);
            }
        }

        public void SetOutputDevice(IOutputDevice outputDevice)
        {
            foreach (var controller in Controllers)
            {
                controller.SetOutputDevice(outputDevice);
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}