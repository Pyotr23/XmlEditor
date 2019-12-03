using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
//using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using XmlEditor.Model;
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
        private Dictionary<string, List<XmlNode>> _nodesDictionary;
        private XmlDocument _checkingDocument = new XmlDocument();

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
        public RelayCommand OpenFileCommand { get; private set; }

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
                () => DeleteSelectedId(SelectedBadParameterId), 
                () => !(SelectedBadParameterId is null));
            DeleteAllIdsCommand = new RelayCommand(
                () => DeleteAllBadIds(),
                () => BadParameterIds.Count != 0);
            OpenFileCommand = new RelayCommand(
                () => Process.Start(FilePath),
                () => File.Exists(FilePath));
        }

        private void DeleteAllBadIds()
        {
            while (BadParameterIds.Count > 0)            
            {
                var badParameterDiscreteSet = _nodesDictionary["ParameterDiscreteSet"]
                                                  .First(x => x["ParameterId"].InnerText == BadParameterIds[0]);
                _checkingDocument.DocumentElement.RemoveChild(badParameterDiscreteSet);
                BadParameterIds.Remove(BadParameterIds[0]);
            }            
            _checkingDocument.Save(FilePath);            
            Info = $"Все параметры без описания удалены.";
        }

        private void DeleteSelectedId(string deletingId)
        {            
            foreach (XmlNode node in _nodesDictionary["ParameterDiscreteSet"])
            {
                string parameterId = node["ParameterId"].InnerText;
                if (parameterId == deletingId)
                {
                    _checkingDocument.DocumentElement.RemoveChild(node);
                    _checkingDocument.Save(FilePath);
                    BadParameterIds.Remove(deletingId);
                    break;
                }                    
            }
            string idBegining = new string(deletingId.Take(8).ToArray());
            Info = $"Параметр с Id \"{idBegining}...\" удалён.";
        }
        
        private void RunCalculation(string filePath)
        {
            if (BadParameterIds.Count != 0)
                BadParameterIds.Clear();

            _nodesDictionary = GetNodesDictionary(filePath);
            var parameterIdHashSet = GetParameterIdHashSet();
            int parametersCount = parameterIdHashSet.Count;
            FillBadParameterIdsCollection(parameterIdHashSet);
            Info = $"Обработка окончена. {BadParameterIds.Count} параметров без описания, с описанием - {parametersCount}.";
        }

        private Dictionary<string, List<XmlNode>> GetNodesDictionary(string filePath)
        {            
            _checkingDocument.Load(filePath);
            XmlElement rootElement = _checkingDocument.DocumentElement;
            var xmlNodesDictionary = new Dictionary<string, List<XmlNode>>();
            foreach (XmlNode node in rootElement.ChildNodes)
            {
                if (xmlNodesDictionary.ContainsKey(node.Name))
                    xmlNodesDictionary[node.Name].Add(node);
                else
                    xmlNodesDictionary.Add(node.Name, new List<XmlNode>() { node });
            }
            return xmlNodesDictionary;
        }

        private void FillBadParameterIdsCollection(HashSet<string> parameterIdHashSet)
        {
            //List<string> badDiscreteSetValueIds = new List<string>();
            //foreach (XmlNode node in _nodesDictionary["ParameterDiscreteSet"])
            //{
            //    string parameterId = node["ParameterId"].InnerText;
            //    if (!parameterIdHashSet.Contains(parameterId))
            //    {
            //        BadParameterIds.Add(parameterId);
            //        badDiscreteSetValueIds.Add(node["DiscreteSetValueId"].InnerText);
            //    }                    
            //}

            var parameterDescreteSets = _nodesDictionary["ParameterDiscreteSet"];
            var parameters = _nodesDictionary["Parameters"];
            var parameterIds = parameters.Select(x => x["Id"].InnerText);
            var badParameterIds = parameterDescreteSets
                .Where(x => !parameterIds.Contains(x["ParameterId"].InnerText))
                .Select(p => p["ParameterId"].InnerText)
                .ToList();
            var badDiscreteSetValueIds = parameterDescreteSets
                .Where(x => !parameterIds.Contains(x["ParameterId"].InnerText))                            
                .Select(x => x["DiscreteSetValueId"].InnerText)
                .ToList();
            var badDiscreteSetIds = new List<string>();
            badDiscreteSetValueIds.ForEach(d =>
            {
                badDiscreteSetIds.Add
                    (_nodesDictionary["DiscreteSetValue"]
                        .FirstOrDefault(x => x["Id"].InnerText == d)["DiscreteSetId"].InnerText);
            });
            //var badDiscrSetIds = _nodesDictionary["DiscreteSetValue"]
            //    .Where((x, i) => badDiscreteSetValueIds.Contains(x["Id"].InnerText))
            //    .Select(x => x["DiscreteSetId"].InnerText);

            var badSets = new ObservableCollection<DiscreteSet>();

            badDiscreteSetIds.ForEach(b =>
            {
                var badDiscreteSetNode = _nodesDictionary["DiscreteSet"]
                                            .FirstOrDefault(x => x["Id"].InnerText == b);
                badSets.Add(new DiscreteSet() 
                { 
                    ParameterId = badParameterIds[0], 
                    Id = badDiscreteSetNode["Id"].InnerText,
                    Name = badDiscreteSetNode["Name"].InnerText
                });
                badParameterIds.RemoveAt(0);
            });

            //var badSets2 = _nodesDictionary["DiscreteSet"]
            //    .Where(x => badDiscrSetIds.Contains(x["Id"].InnerText))
            //    .Select((x, i) => new { Identity = i, Id = x["Id"].InnerText, Name = x["Name"].InnerText })
            //    .Join(badParameterIds, x => x.Identity, y => y.Identity, (x, y) => new { y.ParameterId, x.Id, x.Name });

            //List<string> badDiscreteSetIds = new List<string>();
            //foreach (XmlNode node in _nodesDictionary["DiscreteSetValue"])
            //{
            //    if (badDiscreteSetValueIds.Contains(node["Id"].InnerText))
            //    {
            //        badDiscreteSetIds.Add(node["DiscreteSetId"].InnerText);                    
            //    }
            //}

            //List<string> badDiscreteSetName = new List<string>();
            //List<DiscreteSet> discreteSets = new List<DiscreteSet>();
            //foreach (XmlNode node in _nodesDictionary["DiscreteSet"])
            //{
            //    if (badDiscreteSetIds.Contains(node["Id"].InnerText))
            //        discreteSets.Add(new DiscreteSet() { Id = node["Id"].InnerText, Name = node["Name"].InnerText });
            //        //badDiscreteSetName.Add(node["Name"].InnerText);
            //}

            //MessageBox.Show($"DiscreteSetValueIds:\n{string.Join("\n", badDiscreteSetValueIds.Distinct())}\n\n" +
            //    $"DiscreteSetIds:\n{string.Join("\n", discreteSets.Select(x => x.Id).ToArray())}\n\n" +
            //    $"DiscreteSetNames:\n{string.Join("\n", discreteSets.Select(s => s.Name).ToArray())}");
        }

        private HashSet<string> GetParameterIdHashSet()
        {
            var parameterIdHashSet = new HashSet<string>();
            foreach (XmlNode node in _nodesDictionary["Parameters"])
            {
                string parameterId = node["Id"].InnerText;
                if (!parameterIdHashSet.Contains(parameterId))
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