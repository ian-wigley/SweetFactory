using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SweetFactory
{
    public partial class Form1 : Form
    {
        private List<string> contentUpdate = new List<string>();
        private List<string> fileContents = new List<string>();
        private List<string> variableName = new List<string>();
        private List<string> removeVariable = new List<string>();
        private RuntimeTextTemplate1 page;

        public Form1()
        {
            InitializeComponent();
            page = new RuntimeTextTemplate1();
            page.WriteLine("//----------------------------------------------------------------------");
            page.WriteLine("//                Applet Converted using the Sweet Factory              ");
            page.WriteLine("//----------------------------------------------------------------------");
            page.WriteLine("");
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "EA App";
            openFileDialog.InitialDirectory = @"*.*";
            openFileDialog.Filter = "All files (*.*)|*.*|All files (*.java)|*.java";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var reader = new StreamReader(File.OpenRead(openFileDialog.FileName));

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    fileContents.Add(line);
                }

                MakeItSweet();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save Updated File";
            saveFileDialog.InitialDirectory = @"*.*";
            saveFileDialog.Filter = "All files (*.*)|*.*|All files (*.java)|*.java";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                String pageContent = page.TransformText();
                File.WriteAllText(saveFileDialog.FileName, pageContent);
            }
        }

        private void MakeItSweet()
        {
            bool voidHit = false;
            foreach (string line in fileContents.ToList())
            {
                if (line.Contains("package"))
                {
                    page.WriteLine(line + "\n");
                }
                else if (line.Contains("extends Applet implements Runnable"))
                {
                    string temp = line.Replace("extends Applet implements Runnable", "");
                    contentUpdate.Add(temp);
                }
                else if (line.Contains("Image ") || line.Contains("Image["))
                {
                    string temp = line.Replace("Image", "static HTMLImageElement");
                    contentUpdate.Add(temp);
                }
                else if (line.Contains("Dimension"))
                {
                    variableName.Add(line);
                    removeVariable.Add(line.Replace("Dimension", ""));
                }
                else if (line.Contains("Font"))
                {
                    variableName.Add(line);
                    removeVariable.Add(line.Replace("Font", ""));
                }
                else if (line.Contains("FontMetrics"))
                {
                    variableName.Add(line);
                    removeVariable.Add(line.Replace("FontMetrics", ""));
                }
                else if (line.Contains("Graphics"))
                {
                    variableName.Add(line);
                    removeVariable.Add(line.Replace("Graphics", ""));
                }
                else if (line.Contains("Thread"))
                {
                    variableName.Add(line);
                    removeVariable.Add(line.Replace("Thread", ""));
                }
                else if (line.Contains("AudioClip"))
                {
                    RecordOld("AudioClip", line);
                }
                else if (line.Contains("Color"))
                {
                    variableName.Add(line);
                    removeVariable.Add(line.Replace("Color", ""));
                }
                else if (line != string.Empty)
                {
                    contentUpdate.Add(line);
                }

                if (!voidHit)
                {
                    if (line.Contains("int"))
                    {
                        string temp = line.Replace("int", "static int");
                        contentUpdate.Remove(line);
                        contentUpdate.Add(temp);
                    }

                    if (line.Contains("boolean"))
                    {
                        string temp = line.Replace("boolean", "static boolean");
                        contentUpdate.Remove(line);
                        contentUpdate.Add(temp);
                    }

                    if (line.Contains("long"))
                    {
                        string temp = line.Replace("long", "static long");
                        contentUpdate.Remove(line);
                        contentUpdate.Add(temp);
                    }
                }

                else
                {
                    if (line.Contains("drawImage"))
                    {
                        int pos = line.IndexOf("drawImage");
                        string temp = line.Substring(pos);
                        temp = temp.Replace("drawImage", "ctx.drawImage");
                        temp = temp.Replace(", this", "");
                        contentUpdate.Remove(line);
                        contentUpdate.Add(temp);
                    }

                    if (line.Contains("public void"))
                    {
                        string temp = line.Replace("public void", "public static void");
                        contentUpdate.Remove(line);
                        contentUpdate.Add(temp);
                    }

                    if (line.Contains("public boolean"))
                    {
                        string temp = line.Replace("public boolean", "public static boolean");
                        contentUpdate.Remove(line);
                        contentUpdate.Add(temp);
                    }

                    if (line.Contains("getImage(getCodeBase(),"))
                    {
                        string temp = line.Replace("getImage(getCodeBase(),", @"(HTMLImageElement) document.getElementById(""YOUR GRAPHICS"");//");
                        contentUpdate.Remove(line);
                        contentUpdate.Add(temp);
                    }
                }
                if (line.Contains("void"))
                {
                    voidHit = true;
                }
            }

            AddImports();

            int index = 0;
            foreach (string line in contentUpdate.ToList())
            {
                foreach (string varName in removeVariable)
                {
                    string temp = varName.Replace(";", "");
                    temp = temp.Replace(" ", "");
                    temp = temp.Replace("[]", "");

                    if (temp.Length > 1)
                    {
                        if (line.Contains(temp))
                        {
                            contentUpdate[index] = "//" + line;
                        }
                    }
                }
                if (line.Contains("Event."))
                {
                    contentUpdate[index] = "//" + line;
                }
                index++;
            }

            foreach (string line in contentUpdate.ToList())
            {
                if (line.ToLower().Contains("public class"))
                {

                    page.WriteLine(line);
                    page.WriteLine("");
                    page.WriteLine("private static HTMLCanvasElement canvas;");
                    page.WriteLine("private static CanvasRenderingContext2D ctx;");
                    page.WriteLine("");
                }
                else
                {
                    page.WriteLine(line);
                }

            }
            AddMethods();

            MessageBox.Show("Done");
        }

        private void RecordOld(string type, string line)
        {
            variableName.Add(line);
            string temp = line.Replace(";", "");
            temp = temp.Replace(type, "");
            var values = temp.Split(',');
            foreach (string str in values)
            {
                removeVariable.Add(str);
            }
        }

        private void AddImports()
        {
            page.WriteLine("import static jsweet.dom.Globals.console;");
            page.WriteLine("import static jsweet.dom.Globals.document;");
            page.WriteLine("import static jsweet.dom.Globals.window;");
            page.WriteLine("import static jsweet.util.StringTypes.mousedown;");
            page.WriteLine("import static jsweet.util.StringTypes.mousemove;");
            page.WriteLine("import static jsweet.util.StringTypes.mouseup;");
            page.WriteLine("import static jsweet.util.StringTypes.touchend;");
            page.WriteLine("import static jsweet.util.StringTypes.touchmove;");
            page.WriteLine("import static jsweet.util.StringTypes.touchstart;");
            page.WriteLine("import static jsweet.util.StringTypes.keypress;");
            page.WriteLine("import static jsweet.util.StringTypes.keydown;");
            page.WriteLine("");
            page.WriteLine("import jsweet.dom.CanvasRenderingContext2D;");
            page.WriteLine("import jsweet.dom.Element;");
            page.WriteLine("import jsweet.dom.Event;");
            page.WriteLine("import jsweet.dom.HTMLCanvasElement;");
            page.WriteLine("import jsweet.dom.HTMLElement;");
            page.WriteLine("import jsweet.dom.HTMLImageElement;");
            page.WriteLine("import jsweet.dom.Image;");
            page.WriteLine("import jsweet.dom.MouseEvent;");
            page.WriteLine("import jsweet.dom.Touch;");
            page.WriteLine("import jsweet.dom.TouchEvent;");
            page.WriteLine("import jsweet.dom.KeyboardEvent;");
            page.WriteLine("");
            page.WriteLine("import jsweet.lang.Math;");
            page.WriteLine("import jsweet.util.StringTypes;");
            page.WriteLine("");
        }

        private void AddMethods()
        {
            page.WriteLine("public static void main(String[] args) {");
            page.WriteLine("    Sweetinit();");
            page.WriteLine("    InstallListeners();");
            page.WriteLine("    AddHitListener(canvas);");
            page.WriteLine("    LifeCycle();");
            page.WriteLine("}");
            page.WriteLine("");
            page.WriteLine(" // Do some initailaistaion");
            page.WriteLine("public static void Sweetinit() {");
            page.WriteLine(@"     canvas = (HTMLCanvasElement) document.getElementById(""canvas"");");
            page.WriteLine(" ");
            page.WriteLine(@"      Element body = document.querySelector(""body"");");
            page.WriteLine("      double size = Math.min(body.clientHeight, body.clientWidth);");
            page.WriteLine("      canvas.width = size - 20;");
            page.WriteLine("      canvas.height = size - 20;");
            page.WriteLine(@"      canvas.style.top = (body.clientHeight / 2 - size / 2 + 10) + ""px"";");
            page.WriteLine(@"      canvas.style.left = (body.clientWidth / 2 - size / 2 + 10) + ""px"";");
            page.WriteLine("      ctx = canvas.getContext(StringTypes._2d);");
            page.WriteLine("}");
            page.WriteLine("");
            page.WriteLine("// Main loop that replaces the Applet lifecycle");
            page.WriteLine("public static void LifeCycle() {");
            page.WriteLine("       // Add update/paint methods as required");
            page.WriteLine("       window.requestAnimationFrame((time) -> {");
            page.WriteLine("         LifeCycle();");
            page.WriteLine("});");
            page.WriteLine("");
            page.WriteLine("public static void InstallListeners() {");
            page.WriteLine("       canvas.addEventListener(mousedown, event -> {");
            page.WriteLine("               onMouseDown(event);");
            page.WriteLine("               return null;");
            page.WriteLine("       }, true); ");
            page.WriteLine("      //canvas.addEventListener(mousemove, event -> {");
            page.WriteLine("      //         onMouseMove(event);");
            page.WriteLine("      //         return null;");
            page.WriteLine("      // }, true); ");
            page.WriteLine("      //canvas.addEventListener(mouseup, event -> {");
            page.WriteLine("      //         onMouseUp(event);");
            page.WriteLine("      //         return null;");
            page.WriteLine("      // }, true); ");
            page.WriteLine("}");
            page.WriteLine("");
            page.WriteLine("private static void AddHitListener(HTMLElement element) {");
            page.WriteLine("       window.addEventListener(keydown, event -> {");
            page.WriteLine("          onKeyPress(event);");
            page.WriteLine("          return null;");
            page.WriteLine("       });");
            page.WriteLine("}");
            page.WriteLine("");
            page.WriteLine("public static void onMouseDown(MouseEvent event) {");
            page.WriteLine("       event.preventDefault();");
            page.WriteLine("       // this.area.onInputDeviceDown(event, false);");
            page.WriteLine("       ingame = true;");
            page.WriteLine("       GameStart();");
            page.WriteLine("}");
            page.WriteLine("");
            page.WriteLine("public static void onMouseUp(MouseEvent event) {");
            page.WriteLine("       event.preventDefault();");
            page.WriteLine("       // if (this.area.finished) {");
            page.WriteLine("       // this.initGame();");
            page.WriteLine("       // this.startGame();");
            page.WriteLine("       // }");
            page.WriteLine("       // this.area.onInputDeviceUp(event, false);");
            page.WriteLine("}");
            page.WriteLine("");
            page.WriteLine("public static void onMouseMove(MouseEvent event) {");
            page.WriteLine("       event.preventDefault();");
            page.WriteLine("       onInputDeviceMove(event, false);");
            page.WriteLine("} ");
            page.WriteLine("");
            page.WriteLine("public static void onKeyPress(KeyboardEvent event) {");
            page.WriteLine("       event.preventDefault();");
            page.WriteLine("       onKeyboardPress(event, false);");
            page.WriteLine("}");
            page.WriteLine("");
            page.WriteLine("public static void onKeyboardPress(Event event, boolean touchDevice) {");
            page.WriteLine("      switch ((int) ((KeyboardEvent) event).keyCode) {");
            page.WriteLine("          case 17:");
            page.WriteLine("          break;");
            page.WriteLine("      }");
            page.WriteLine("}");
            page.WriteLine("}");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
