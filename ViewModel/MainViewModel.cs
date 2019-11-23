using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
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

        public ObservableCollection<string> BadParameterIds { get; set; }

        public ICommand ChooseFileCommand { get; private set; }
        
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
        }

        private void RunCalculation(string filePath)
        {
            XmlDocument document = new XmlDocument();
            document.Load(filePath);
            XmlElement rootElement = document.DocumentElement;
            var parameterIdHashSet = GetParameterIdHashSet(rootElement);
            int parametersCount = parameterIdHashSet.Count;
            BadParameterIds = new ObservableCollection<string>(GetBadParameterIds(rootElement, parameterIdHashSet));
            Info = $"Обработка окончена. {BadParameterIds.Count} параметров без описания. Всего - {parametersCount}.";
        }

        private static List<string> GetBadParameterIds(XmlElement rootElement, HashSet<string> parameterIdHashSet)
        {            
            foreach (XmlNode node in rootElement.ChildNodes)
            {
                string parameterId = node.FirstChild.InnerText;
                if (node.Name == "Parameters" && parameterIdHashSet.Contains(parameterId))
                    parameterIdHashSet.Remove(parameterId);
            }
            return parameterIdHashSet.ToList();
        }

        private static HashSet<string> GetParameterIdHashSet(XmlElement rootElement)
        {
            var parameterIdHashSet = new HashSet<string>();
            foreach (XmlNode node in rootElement.ChildNodes)
            {
                string parameterId = node.FirstChild.InnerText;
                if (node.Name == "ParameterDiscreteSet" && !parameterIdHashSet.Contains(parameterId))
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