﻿using System;
using System.Collections.Generic;
using System.Linq;
using LrControl.Api.Common;
using LrControl.Core.Configurations;
using LrControl.Core.Devices.Enums;
using LrControl.Core.Midi;
using RtMidi.Core.Devices;
using RtMidi.Core.Messages;

namespace LrControl.Core.Devices
{   
    internal delegate void ControllerAddedHandler(Controller controller);

    public interface IDeviceManager
    {
        /// <summary>
        /// Get input device (if any)
        /// </summary>
        InputDeviceInfo InputDevice { get; }
        
        /// <summary>
        /// Get output device (if any)
        /// </summary>
        OutputDeviceInfo OutputDevice { get; }        
        
        /// <summary>
        /// Set the input device being used
        /// </summary>
        /// <param name="inputDeviceInfo">Input device info to create device from</param>
        void SetInputDevice(InputDeviceInfo inputDeviceInfo);
        
        /// <summary>
        /// Set the output device being used
        /// </summary>
        /// <param name="outputDeviceInfo">Output device info to create device from</param>
        void SetOutputDevice(OutputDeviceInfo outputDeviceInfo);
    }

    internal class DeviceManager : IDeviceManager
    {
        private readonly ISettings _settings;
        private readonly Dictionary<ControllerKey, Controller> _controllers;
        private IMidiInputDevice _inputDevice;
        private IMidiOutputDevice _outputDevice;

        public DeviceManager(ISettings settings)
        {
            _controllers = new Dictionary<ControllerKey, Controller>();
            _settings = settings;
        }

        internal IReadOnlyCollection<Controller> Controllers => _controllers.Values;

        public InputDeviceInfo InputDevice => _inputDevice != null
            ? new InputDeviceInfo(_inputDevice)
            : null;

        public OutputDeviceInfo OutputDevice => _outputDevice != null
            ? new OutputDeviceInfo(_outputDevice)
            : null;

        public event ControllerAddedHandler ControllerAdded;

        public void SetInputDevice(InputDeviceInfo inputDeviceInfo)
        {
            if (_inputDevice != null)
            {
                if (_inputDevice.IsOpen)
                    _inputDevice.Close();
                
                _inputDevice.ControlChange -= InputDeviceOnControlChange;
                _inputDevice.Nrpn -= InputDeviceOnNrpn;
                _inputDevice.Dispose();
            }

            if (inputDeviceInfo != null)
            {
                var inputDevice = inputDeviceInfo.CreateDevice();
                _inputDevice = new InputDeviceDecorator(inputDevice, _settings);
                
                _inputDevice.ControlChange += InputDeviceOnControlChange;
                _inputDevice.Nrpn += InputDeviceOnNrpn;

                if (!_inputDevice.IsOpen)
                    _inputDevice.Open();
            }
            else
            {
                _inputDevice = null;
            }
        }

        public void SetOutputDevice(OutputDeviceInfo outputDeviceInfo)
        {
            if (_outputDevice != null)
            {
                if (_outputDevice.IsOpen)
                    _outputDevice.Close();

                _outputDevice.Dispose();
            }

            if (outputDeviceInfo != null)
            {
                _outputDevice = outputDeviceInfo.CreateDevice();
                
                if (!_outputDevice.IsOpen)
                    _outputDevice.Open();
            }
            else
            {
                _outputDevice = null;
            }
        }
        
        public void Clear()
        {
            _controllers.Clear();
        }

        public void SetConfiguration(IEnumerable<ControllerConfiguration> controllerConfiguration)
        {
            // Clear existing controllers
            Clear();

            // Load configuration controllers from configuration
            foreach (var conf in controllerConfiguration)
            {
                var controller = new Controller(this, conf.MessageType, conf.ControllerType, conf.Channel,
                    conf.ControlNumber, new Range(conf.RangeMin, conf.RangeMax));

                _controllers[new ControllerKey(controller)] = controller;
            }
            
            // Reset all controllers new controllers
            foreach (var controller in _controllers.Values)
            {
                controller.Reset();
            }
        }
        
        public List<ControllerConfiguration> GetConfiguration()
        {
            return _controllers.Values.Select(x => new ControllerConfiguration(x)).ToList();
        }

        // TODO Figure out way to not have this as a public method, but injected into Controller somehow
        public void OnDeviceOutput(Controller controller, int controllerValue)
        {
            if (OutputDevice == null) return;

            switch (controller.MessageType)
            {
                case ControllerMessageType.ControlChange:
                    _outputDevice.Send(new ControlChangeMessage(controller.MidiChannel, controller.ControlNumber,
                        controllerValue));
                    break;
                case ControllerMessageType.Nrpn:
                    _outputDevice.Send(new NrpnMessage(controller.MidiChannel, controller.ControlNumber, controllerValue));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void InputDeviceOnControlChange(IMidiInputDevice sender, in ControlChangeMessage message)
        {
            var key = new ControllerKey(ControllerMessageType.ControlChange, message.Channel, message.Control);
            UpdateControllerValue(key, message.Value);
        }

        private void InputDeviceOnNrpn(IMidiInputDevice sender, in NrpnMessage message)
        {
            var key = new ControllerKey(ControllerMessageType.Nrpn, message.Channel, message.Parameter);
            UpdateControllerValue(key, message.Value);
        }

        private void UpdateControllerValue(ControllerKey key, int value)
        {
            // Get controller or create new if not previously seen
            if (!_controllers.TryGetValue(key, out var controller))
            {
                _controllers[key] = controller = new Controller(this, key.ControllerMessageType, ControllerType.Encoder,
                    key.Channel, key.ControlNumber, new Range(value, value));

                OnControllerAdded(controller);
            }

            controller.OnDeviceInput(value);
        }

        private void OnControllerAdded(Controller controller)
        {
            ControllerAdded?.Invoke(controller);
        }
    }
}