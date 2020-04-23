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

namespace EtherCAT_Implementation_Tool.Core
{
    public class PythonScripting
    {

        public string CurrentFileName { get; set; }
        public string CurrentScriptText { get; set; }

        private readonly string pythonFolder = "PythonScripts";

        public ScriptEngine PyEngine;
        public ScriptScope PyScope;

        public PythonScripting(string file_name)
        {

            PyEngine = Python.CreateEngine();
            PyScope = PyEngine.CreateScope();

            //ScriptRuntime runtime = PyEngine.Runtime;
            //runtime.LoadAssembly(typeof(string).Assembly);
            //runtime.LoadAssembly(typeof(Uri).Assembly);

            CurrentFileName = file_name;

            CurrentScriptText = System.IO.File.ReadAllText( pythonFolder + "\\" + CurrentFileName);

        }

        public void SaveScript()
        {
            System.IO.File.WriteAllText(pythonFolder + "\\" + CurrentFileName, CurrentScriptText);
        }

    }
}
