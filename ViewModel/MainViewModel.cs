using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
//using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using WinForms = System.Windows.Forms;

namespace XmlEditor.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private const string ProcessedMessage = "Идёт обработка...";
        private const string ErrorFileMessage = "Выберите XML-файл.";

        private string _filePath;
        private string _info;
        private string _selectedBadParameterId;

        public string SelectedBadParameterId
        {
            get => _selectedBadParameterId;
            set
            {
                _selectedBadParameterId = value;
                RaisePropertyChanged(nameof(SelectedBadParameterId));
            }
        }

        public string Info
        {
            get => _info;
            set
            {
                _info = value;
                RaisePropertyChanged(nameof(Info));
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                RaisePropertyChanged(nameof(FilePath));
                Info = GetCheckingPathResult(value);
                if (Info == ProcessedMessage)
                    RunCalculation(value);
            }
        }

        public ObservableCollection<string> BadParameterIds { get; set; } = new ObservableCollection<string>();

        public RelayCommand ChooseFileCommand { get; private set; }
        public RelayCommand DeleteSelectedIdCommand { get; private set; }
        public RelayCommand DeleteAllIdsCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}

            ChooseFileCommand = new RelayCommand(ChooseFile);
            DeleteSelectedIdCommand = new RelayCommand(
                () => BadParameterIds.Remove(SelectedBadParameterId), 
                () => !(SelectedBadParameterId is null));
            DeleteAllIdsCommand = new RelayCommand(
                () => BadParameterIds.Clear(),
                () => BadParameterIds.Count != 0);
        }

        private void RunSurvey()
        {
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(ExecuteSurvey);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void ExecuteSurvey(object sender, EventArgs e)
        {
            //CommandManager.InvalidateRequerySuggested();
        }

        private void RunCalculation(string filePath)
        {
            if (BadParameterIds.Count != 0)
                BadParameterIds.Clear();
            XmlDocument document = new XmlDocument();
            document.Load(filePath);
            XmlElement rootElement = document.DocumentElement;
            var parameterIdHashSet = GetParameterIdHashSet(rootElement);
            int parametersCount = parameterIdHashSet.Count;
            FillBadParameterIdsCollection(rootElement, parameterIdHashSet);
            Info = $"Обработка окончена. {BadParameterIds.Count} параметров без описания. Всего - {parametersCount}.";
        }

        private void FillBadParameterIdsCollection(XmlElement rootElement, HashSet<string> parameterIdHashSet)
        {            
            foreach (XmlNode node in rootElement.ChildNodes)
            {
                string parameterId = node.FirstChild.InnerText;
                if (node.Name == "ParameterDiscreteSet" && !parameterIdHashSet.Contains(parameterId))
                    BadParameterIds.Add(parameterId);
            }            
        }

        private HashSet<string> GetParameterIdHashSet(XmlElement rootElement)
        {
            var parameterIdHashSet = new HashSet<string>();
            foreach (XmlNode node in rootElement.ChildNodes)
            {
                string parameterId = node.FirstChild.InnerText;
                if (node.Name == "Parameters" && !parameterIdHashSet.Contains(parameterId))
                    parameterIdHashSet.Add(parameterId);
            }
            return parameterIdHashSet;
        }

        private string GetCheckingPathResult(string path)
        {
            return Path.GetExtension(path) == ".xml" ? ProcessedMessage : ErrorFileMessage;            
        }

        private void ChooseFile()
        {
            using (var dialog = new WinForms.OpenFileDialog())
            {
                var result = dialog.ShowDialog();
                if (result == WinForms.DialogResult.OK)
                    FilePath = dialog.FileName;                
            }
        }
    }
}