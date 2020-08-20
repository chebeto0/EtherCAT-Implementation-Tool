using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;

namespace EtherCAT_Master.Core.Controls
{
    public class PythonOutput
    {

        private readonly RichTextBox outputBox;
        private readonly MainWindow MW;

        public PythonOutput(MainWindow main_window, RichTextBox out_box)
        {
            MW = main_window;
            outputBox = out_box;
        }

        public void println(object text)
        {
            MW.Dispatcher.Invoke(() =>
            {
                outputBox.AppendText(text.ToString());
                outputBox.AppendText("\u2028"); /* Linebreak, not paragraph break */
                outputBox.ScrollToEnd();
            });
        }

        public void saveToText()
        {
            string richText = new TextRange(outputBox.Document.ContentStart, outputBox.Document.ContentEnd).Text;

            richText = richText.Replace("\u2028", "\n");

            Directory.CreateDirectory(Path.Combine(MW.PyScripting.PythonFolderName, "Saves"));
            File.WriteAllText(Path.Combine(MW.PyScripting.PythonFolderName, "Saves", string.Format("{0}_output_text.txt", DateTime.Now.ToString("yyyyMMddHHmmss"))), richText);

        }

        public void Clear()
        {
            outputBox.SelectAll();

            outputBox.Selection.Text = "";
        }

    }
}
