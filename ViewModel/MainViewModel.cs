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
                    RunCalculation();
                int parametersCount = _nodesDictionary["Parameters"].Count;
                Info = $"Обработка окончена. {BadDiscreteSets.Count} параметров без описания, с описанием - {parametersCount}.";
            }
        }

        public ObservableCollection<DiscreteSet> BadDiscreteSets { get; set; } = new ObservableCollection<DiscreteSet>();        

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
                () => DeleteSelectedId(), 
                () => !(SelectedBadParameterId is null));
            DeleteAllIdsCommand = new RelayCommand(
                () => DeleteAllBadIds(),
                () => BadDiscreteSets.Count != 0);
            OpenFileCommand = new RelayCommand(
                () => Process.Start(FilePath),
                () => File.Exists(FilePath));
        }

        private void DeleteAllBadIds()
        {
            while (BadDiscreteSets.Count > 0)
            {                
                while (BadDiscreteSets[0].ParameterIds.Count > 0)
                {
                    var badParameterDiscreteSet = _nodesDictionary["ParameterDiscreteSet"]
                                                    .Single(x => x["ParameterId"].InnerText == BadDiscreteSets[0].ParameterIds[0]);
                    _checkingDocument.DocumentElement.RemoveChild(badParameterDiscreteSet);
                    BadDiscreteSets[0].ParameterIds.RemoveAt(0);
                }
                BadDiscreteSets.Remove(BadDiscreteSets[0]);
            }
            _checkingDocument.Save(FilePath);
            Info = $"Все параметры без описания удалены.";
        }

        private void DeleteSelectedId()
        {
            string deletingId = SelectedBadParameterId;
            var badParameterDiscreteSet = _nodesDictionary["ParameterDiscreteSet"]
                                                    .Single(x => x["ParameterId"].InnerText == SelectedBadParameterId);
            _checkingDocument.DocumentElement.RemoveChild(badParameterDiscreteSet);
            _checkingDocument.Save(FilePath);
            var selectedBadDiscreteSet = BadDiscreteSets.First(x => x.ParameterIds.Contains(deletingId));
            if (selectedBadDiscreteSet.ParameterIds.Count == 1)
                BadDiscreteSets.Remove(selectedBadDiscreteSet);
            else
                BadDiscreteSets.First(x => x == selectedBadDiscreteSet).ParameterIds.Remove(SelectedBadParameterId);

            SelectedBadParameterId = null;
            string idBegining = new string(deletingId.Take(8).ToArray());
            Info = $"Параметр с Id \"{idBegining}...\" удалён.";
        }
        
        private void RunCalculation()
        {
            _nodesDictionary = GetNodesDictionary(FilePath);            
            FillBadDiscreteSets();            
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

        private void FillBadDiscreteSets()
        {
            BadDiscreteSets.Clear();
            // Идентификаторы всех параметров.
            var parameterIds = _nodesDictionary["Parameters"].Select(x => x["Id"].InnerText);

            // ParameterDiscreteSets, идентификаторов параметров которых нет в идентификаторах всех параметрах.
            var badParameterDiscreteSets = _nodesDictionary["ParameterDiscreteSet"]
                                                .Where(x => !parameterIds.Contains(x["ParameterId"].InnerText));

            // Идентификаторы параметров badDiscreteSets.
            var badParameterIds = badParameterDiscreteSets                
                                    .Select(p => p["ParameterId"].InnerText)
                                    .ToList();

            // Идентификаторы значений дискретных наборов badParameterDiscreteSets.
            var badDiscreteSetValueIds = badParameterDiscreteSets                                            
                                    .Select(x => x["DiscreteSetValueId"].InnerText)
                                    .ToList();

            // Идентификаторы дискретных наборов, соответствующие badDiscreteSetValueIds.
            var badDiscreteSetIds = new List<string>();
            badDiscreteSetValueIds.ForEach(d =>
            {
                badDiscreteSetIds.Add
                    (_nodesDictionary["DiscreteSetValue"]
                        .FirstOrDefault(x => x["Id"].InnerText == d)["DiscreteSetId"].InnerText);
            });

            // Здесь формируется соответствие "ParameterId" - "DiscreteSetId и DiscreteSetName". ParameterId собираются в список для
            // каждой пары "DiscreteSetId и DiscreteSetName".
            for (int i = 0; i < badDiscreteSetIds.Count; i++)
            {
                var badDiscreteSetNode = _nodesDictionary["DiscreteSet"]
                                            .FirstOrDefault(x => x["Id"].InnerText == badDiscreteSetIds[i]);
                if (!BadDiscreteSets.Select(x => x.Id).ToList().Contains(badDiscreteSetNode["Id"].InnerText))
                    BadDiscreteSets.Add(new DiscreteSet() 
                    { 
                        Id = badDiscreteSetNode["Id"].InnerText,
                        Name = badDiscreteSetNode["Name"].InnerText,
                        ParameterIds = new ObservableCollection<string>() { badParameterIds[i] }
                    });
                else
                {
                    var existingBadDiscreteSet = BadDiscreteSets.First(x => x.Id == badDiscreteSetNode["Id"].InnerText);
                    existingBadDiscreteSet.ParameterIds.Add(badParameterIds[i]);
                }
            }
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