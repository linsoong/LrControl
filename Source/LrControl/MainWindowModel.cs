﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using micdah.LrControl.Annotations;
using micdah.LrControl.Configurations;
using micdah.LrControl.Core;
using micdah.LrControl.Core.Midi;
using micdah.LrControl.Gui.Tools;
using micdah.LrControl.Mapping;
using micdah.LrControl.Mapping.Catalog;
using micdah.LrControlApi;
using micdah.LrControlApi.Modules.LrApplicationView;
using MahApps.Metro.Controls.Dialogs;
using Midi.Devices;
using Prism.Commands;

namespace micdah.LrControl
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        private ControllerManager _controllerManager;
        private IMainWindowDialogProvider _dialogProvider;
        private FunctionCatalog _functionCatalog;
        private FunctionGroupManager _functionGroupManager;
        private InputDeviceDecorator _inputDevice;
        private IOutputDevice _outputDevice;
        private bool _showSettingsDialog;
        private ObservableCollection<IInputDevice> _inputDevices;
        private ObservableCollection<IOutputDevice> _outputDevices;
        private string _inputDeviceName;
        private string _outputDeviceName;

        public MainWindowModel(LrApi api)
        {
            Api           = api;
            InputDevices  = new ObservableCollection<IInputDevice>();
            OutputDevices = new ObservableCollection<IOutputDevice>();

            // Commands
            OpenSettingsCommand            = new DelegateCommand(OpenSettings);
            SaveCommand                    = new DelegateCommand(() => SaveConfiguration());
            LoadCommand                    = new DelegateCommand(() => LoadConfiguration());
            ExportCommand                  = new DelegateCommand(ExportConfiguration);
            ImportCommand                  = new DelegateCommand(ImportConfiguration);
            ResetCommand                   = new DelegateCommand(Reset);
            RefreshAvailableDevicesCommand = new DelegateCommand(RefreshAvailableDevices);
            SetupControllerCommand         = new DelegateCommand(SetupController);

            // Initialize catalogs and controllers
            FunctionCatalog      = FunctionCatalog.DefaultCatalog(api);
            ControllerManager    = new ControllerManager();
            FunctionGroupManager = FunctionGroupManager.DefaultGroups(api, FunctionCatalog, ControllerManager);

            // Hookup module listener
            api.LrApplicationView.ModuleChanged += FunctionGroupManager.EnableModule;
        }

        public LrApi Api { get; }

        public IMainWindowDialogProvider DialogProvider
        {
            get { return _dialogProvider; }
            set
            {
                if (Equals(value, _dialogProvider)) return;
                _dialogProvider = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenSettingsCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand RefreshAvailableDevicesCommand { get; }

        public ICommand SetupControllerCommand { get; }

        public IInputDevice InputDevice
        {
            get { return _inputDevice; }
            set
            {
                if (Equals(value, _inputDevice)) return;

                if (_inputDevice != null)
                {
                    if (_inputDevice.IsReceiving) _inputDevice.StopReceiving();
                    if (_inputDevice.IsOpen) _inputDevice.Close();
                    _inputDevice.Dispose();
                }

                _inputDevice = new InputDeviceDecorator(value);
                ControllerManager.InputDevice = _inputDevice;

                if (_inputDevice != null)
                {
                    if (!_inputDevice.IsOpen) _inputDevice.Open();
                    if (!_inputDevice.IsReceiving) _inputDevice.StartReceiving(null);
                }
                
                OnPropertyChanged();

                InputDeviceName = _inputDevice?.Name;
            }
        }

        public string InputDeviceName
        {
            get { return _inputDeviceName; }
            set
            {
                if (value == _inputDeviceName) return;
                _inputDeviceName = value;
                OnPropertyChanged();

                InputDevice = InputDevices.FirstOrDefault(x => x.Name == value);
            }
        }

        public ObservableCollection<IInputDevice> InputDevices
        {
            get { return _inputDevices; }
            private set
            {
                if (Equals(value, _inputDevices)) return;
                _inputDevices = value;
                OnPropertyChanged();
            }
        }

        public IOutputDevice OutputDevice
        {
            get { return _outputDevice; }
            set
            {
                if (Equals(value, _outputDevice)) return;

                if (_outputDevice != null)
                {
                    if (_outputDevice.IsOpen) _outputDevice.Close();
                }

                _outputDevice = value;
                ControllerManager.OutputDevice = value;

                if (_outputDevice != null)
                {
                    if (!_outputDevice.IsOpen) _outputDevice.Open();
                }
                
                OnPropertyChanged();

                OutputDeviceName = _outputDevice?.Name;
            }
        }

        public string OutputDeviceName
        {
            get { return _outputDeviceName; }
            set
            {
                if (value == _outputDeviceName) return;
                _outputDeviceName = value;
                OnPropertyChanged();

                OutputDevice = OutputDevices.FirstOrDefault(x => x.Name == value);
            }
        }

        public ObservableCollection<IOutputDevice> OutputDevices
        {
            get { return _outputDevices; }
            private set
            {
                if (Equals(value, _outputDevices)) return;
                _outputDevices = value;
                OnPropertyChanged();
            }
        }

        private ControllerManager ControllerManager
        {
            get { return _controllerManager; }
            set
            {
                if (Equals(value, _controllerManager)) return;
                _controllerManager = value;
                OnPropertyChanged();
            }
        }

        public FunctionCatalog FunctionCatalog
        {
            get { return _functionCatalog; }
            private set
            {
                if (Equals(value, _functionCatalog)) return;
                _functionCatalog = value;
                OnPropertyChanged();
            }
        }

        public FunctionGroupManager FunctionGroupManager
        {
            get { return _functionGroupManager; }
            private set
            {
                if (Equals(value, _functionGroupManager)) return;
                _functionGroupManager = value;
                OnPropertyChanged();
            }
        }

        public bool ShowSettingsDialog
        {
            get { return _showSettingsDialog; }
            set
            {
                if (value == _showSettingsDialog) return;
                _showSettingsDialog = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OpenSettings()
        {
            ShowSettingsDialog = !ShowSettingsDialog;
        }

        public void SaveConfiguration(string file = MappingConfiguration.ConfigurationsFile)
        {
            var conf = new MappingConfiguration
            {
                Controllers = ControllerManager.GetConfiguration(),
                Modules = FunctionGroupManager.GetConfiguration()
            };

            MappingConfiguration.Save(conf, file);
        }

        public void LoadConfiguration(string file = MappingConfiguration.ConfigurationsFile)
        {
            var conf = MappingConfiguration.Load(file);
            if (conf == null) return;

            ControllerManager.Load(conf.Controllers);
            ControllerManager.ResetAllControls();

            FunctionGroupManager.Load(conf.Modules);

            // Enable current module group
            Module currentModule;
            if (Api.LrApplicationView.GetCurrentModuleName(out currentModule))
            {
                FunctionGroupManager.EnableModule(currentModule);
            }
        }

        public void ExportConfiguration()
        {
            var file = _dialogProvider.ShowSaveDialog(GetSettingsFolder());
            if (!string.IsNullOrEmpty(file))
            {
                SaveConfiguration(file);
            }
        }

        public void ImportConfiguration()
        {
            var file = _dialogProvider.ShowOpenDialog(GetSettingsFolder());
            if (!string.IsNullOrEmpty(file))
            {
                LoadConfiguration(file);
            }
        }

        public async void Reset()
        {
            var result = await _dialogProvider.ShowMessage("Are you sure, you want to clear the current configuration?",
                "Confirm clear configuration", DialogButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                ControllerManager?.Clear();
                FunctionGroupManager?.Reset();
            }
        }

        public void RefreshAvailableDevices()
        {
            InputDevices.Clear();
            foreach (var inputDevice in DeviceManager.InputDevices)
            {
                InputDevices.Add(inputDevice);
            }

            OutputDevices.Clear();
            foreach (var outputDevice in DeviceManager.OutputDevices)
            {
                OutputDevices.Add(outputDevice);
            }
        }

        public void SetupController()
        {
            var viewModel = new SetupControllerModel
            {
                InputDevice = InputDevice
            };

            var dialog = new SetupController(viewModel);
            dialog.ShowDialog();
        }

        private static string GetSettingsFolder()
        {
            var settingsFolder =
                Path.GetDirectoryName(Serializer.ResolveRelativeFilename(MappingConfiguration.ConfigurationsFile));
            return settingsFolder;
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}