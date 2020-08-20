using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.IO;
using System.ComponentModel;

namespace EtherCAT_Master.Core
{
    public class PythonScripting : INotifyPropertyChanged
    {

        private string _currentFileName;
        public string CurrentFileName
        {
            get { return _currentFileName; }
            set
            {
                _currentFileName = value;
                OnPropertyChanged("CurrentFileName");
            }
        }
        public string CurrentScriptText { get; set; }
        public string PyScriptDirectory { get; set; }

        public readonly string PythonFolderName = "PythonScripts";

        public ScriptEngine PyEngine;
        public ScriptScope PyScope;

        public PythonScripting(string exe_path, string file_name)
        {

            if (!File.Exists(PythonFolderName + "\\temp.py"))
            {
                File.Create(PythonFolderName + "\\temp.py");
            }

            PyEngine = Python.CreateEngine();

            PyScriptDirectory = Path.Combine(exe_path, PythonFolderName);

            PyEngine.SetSearchPaths(new List<string> { PyScriptDirectory });

            PyScope = PyEngine.CreateScope();

            CurrentFileName = file_name;

            if ( File.Exists(Path.Combine( PyScriptDirectory, CurrentFileName)) )
            {
                CurrentScriptText = File.ReadAllText(Path.Combine(PyScriptDirectory, CurrentFileName));
            }
            else
            {
                CurrentScriptText = File.ReadAllText(Path.Combine(PyScriptDirectory, "temp.py"));
            }

        }

        public void SaveScript()
        {
            File.WriteAllText(Path.Combine(PyScriptDirectory, CurrentFileName), CurrentScriptText);
        }

        public void OpenScript(string file_name)
        {
            CurrentFileName = file_name;

            CurrentScriptText = File.ReadAllText(Path.Combine(PyScriptDirectory, CurrentFileName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
