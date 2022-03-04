using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GH_IO.Serialization;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Robots;
using Robots.Commands;
using Robots.Grasshopper;

namespace RobotsExtended
{
    public class DefJoints : GH_Component, IGH_VariableParameterComponent
    {
        public DefJoints()
          : base("Define Joints", "DefJoints",
              "Define joint angle of each axis of the robot in degrees and outputs it as string of radians",
              "Robots", "Utility")
        { }
        public override Guid ComponentGuid => new Guid("cd62f0e9-b8bc-49d9-b423-5e181b71f22f");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Define_Joints;
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Axis 1", "A1", "Degree of rotation for Axis 1", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis 2", "A2", "Degree of rotation for Axis 2", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis 3", "A3", "Degree of rotation for Axis 3", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis 4", "A4", "Degree of rotation for Axis 4", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis 5", "A5", "Degree of rotation for Axis 5", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis 6", "A6", "Degree of rotation for Axis 6", GH_ParamAccess.item);
        }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "External Axis", ChangeInput, true, external);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Joints", "J", "Joint rotations in radians", GH_ParamAccess.item);
        }
        public override void AddedToDocument(GH_Document doc)
        {
            int i = 0;
            foreach (var p in Params.Input)
            {
                if (p.SourceCount == 0)
                {
                    GH_NumberSlider slider = new GH_NumberSlider();
                    slider.CreateAttributes();

                    slider.Attributes.Pivot = new System.Drawing.PointF(
                        this.Attributes.Pivot.X - slider.Attributes.Bounds.Width - this.Attributes.Bounds.Width / 2 - 30,
                        this.Attributes.Pivot.Y - this.Attributes.Bounds.Height / 2 + i * slider.Attributes.Bounds.Height);
                    slider.Slider.Maximum = limits[1, i];
                    slider.Slider.Minimum = limits[0, i];
                    slider.Slider.DecimalPlaces = 0;
                    slider.SetSliderValue(0);
                    OnPingDocument().AddObject(slider, false);
                    p.AddSource(slider);
                    p.CollectData();
                }
                i++;
            }
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string[] theta = new string[6];
            for (int i = 0; i < (external ? 2 : 6); i++)
            {
                DA.GetData(i, ref theta[i]);
                double deg;
                try { deg = Convert.ToDouble(theta[i]); }
                catch { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input not a number."); return; }
                theta[i] = (deg % 45 == 0 && deg != 0) ? (deg == 180 ? "Pi" : (deg / 180 + " * Pi")) : RhinoMath.ToRadians(deg).ToString();
            }
            StringBuilder str = new StringBuilder(theta[0].ToString() + ", " + theta[1].ToString());
            if (!external)
            {
                for (int i = 2; i < 6; i++)
                {
                    str.Append(", " + theta[i]);
                }
            }
            DA.SetData(0, str);
        }
        private void ChangeInput(object sender, EventArgs e)
        {
            external = !external;
            if (external)
            {
                for (int i = 5; i > 1; i--)
                {
                    Params.UnregisterInputParameter(Params.Input[i], true);
                }
                Params.Input[0].Name = "External 1";
                Params.Input[0].NickName = "E1";
                Params.Input[0].Description = "Degree of rotation for External 1";
                Params.Input[1].Name = "External 2";
                Params.Input[1].NickName = "E2";
                Params.Input[1].Description = "Degree of rotation for External 2";
            }
            else
            {
                Params.Input[0].Name = "Axis 1";
                Params.Input[0].NickName = "A1";
                Params.Input[0].Description = "Degree of rotation for Axis 1";
                Params.Input[1].Name = "Axis 2";
                Params.Input[1].NickName = "A2";
                Params.Input[1].Description = "Degree of rotation for Axis 2";
                foreach (var p in parameters)
                {
                    Params.RegisterInputParam(p);
                }
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("IsExt", external);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            external = reader.GetBoolean("IsExt");
            return base.Read(reader);
        }

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        bool external = false;
        readonly IGH_Param[] parameters = new IGH_Param[4]
        {
         new Param_String { Name = "Axis 3", NickName = "A3", Description = "Degree of rotation for Axis 3", Optional = false },
         new Param_String { Name = "Axis 4", NickName = "A4", Description = "Degree of rotation for Axis 4", Optional = false },
         new Param_String { Name = "Axis 5", NickName = "A5", Description = "Degree of rotation for Axis 5", Optional = false },
         new Param_String { Name = "Axis 6", NickName = "A6", Description = "Degree of rotation for Axis 6", Optional = false }
        };
        readonly int[,] limits = new int[2, 6]
        {
            {-185,-35,-120,-68,-119,-350},
            { 185, 135, 210, 185, 119, 350},
        };
    }
    public class RenderColour : GH_Component
    {
        public RenderColour()
          : base("Render Colour", "Colour",
              "Adds render colour to robot mesh",
              "Robots", "Utility")
        { }
        public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        { }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Archive archive = new GH_Archive();
            byte[] binaryRep = Convert.FromBase64String(snip);
            archive.Deserialize_Binary(binaryRep);
            string xmlSnippet = archive.Serialize_Xml();
            System.Windows.Forms.Clipboard.Clear();
            System.Windows.Forms.Clipboard.SetText(xmlSnippet);
            Paste();
            Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(10, DeleteThis);
        }

        readonly string snip = "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";

        void DeleteThis(GH_Document doc)
        {
            Grasshopper.Instances.ActiveCanvas.Document.RemoveObject(this, false);
        }
        void Paste()
        {
            GH_DocumentIO documentIO = new GH_DocumentIO();
            documentIO.Paste(GH_ClipboardType.System);
            var thispivot = this.Attributes.Pivot;

            int smallestX = Int32.MaxValue;
            int smallestY = Int32.MaxValue;
            foreach (IGH_DocumentObject obj in documentIO.Document.Objects)
            {
                var pivot = obj.Attributes.Pivot;
                if (pivot.X < smallestX) smallestX = (int)pivot.X;
                if (pivot.Y < smallestY) smallestY = (int)pivot.Y;
            }

            System.Drawing.Size offset = new System.Drawing.Size((int)thispivot.X - smallestX, (int)thispivot.Y - smallestY);

            documentIO.Document.TranslateObjects(offset, false);
            documentIO.Document.SelectAll();
            documentIO.Document.ExpireSolution();
            documentIO.Document.MutateAllIds();
            IEnumerable<IGH_DocumentObject> objs = documentIO.Document.Objects;
            Grasshopper.Instances.ActiveCanvas.Document.DeselectAll();
            Grasshopper.Instances.ActiveCanvas.Document.MergeDocument(documentIO.Document);
            Grasshopper.Instances.ActiveCanvas.Document.UndoUtil.RecordAddObjectEvent("Paste", objs);
            Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(10);
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Colourful;
        public override Guid ComponentGuid => new Guid("5AE1E121-11B3-499A-AB30-82B02FAD533A");
    }
 
    public class KukaMergeKRL : GH_Component, IGH_VariableParameterComponent
    {
        public KukaMergeKRL()
          : base("Merge KRL", "KRL",
              "Merges robots codes into a single KRL file",
              "Robots", "Components")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "C", "Code from create program", GH_ParamAccess.tree);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Save File", SaveInputs, true, save);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("FileName", "src", "Name of program file", GH_ParamAccess.item);
            pManager.AddTextParameter("KRL Code", "KRL", "Merged KRL Code", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool cvel = false;
            bool trigger = false;
            string prevApoCVEL = string.Empty;

            DA.GetDataTree(0, out GH_Structure<GH_String> code);

            List<string> header =
                code.Branches[0][0].Value
                .Split(new string[] { "\r\n", "\r", "\n", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Dictionary<string, string> declare = new Dictionary<string, string>();
            List<string> prog = new List<string>();
            for (int i = 1; i < code.Branches[1].Count - 1; i++)// Skip RVP+REL and ENDDAT
            {
                string[] de = code.Branches[1][i].Value.Split(new string[] { " = ", " =", "= ", "=" }, StringSplitOptions.RemoveEmptyEntries);
                de[0] = de[0].Replace("GLOBAL ", string.Empty);
                if (de[0].Contains("Zonev")) cvel = true;
                if (de[0].Contains("Zone000")) trigger = true;
                if (!declare.ContainsKey(de[0]))
                    declare.Add(de[0], de[1] ?? de[0]);
            }
            for (int i = 1; i < code.Branches[2].Count; i++)// Skip RVP+REL
            {
                prog.Add(code.Branches[2][i].Value);
            }

            List<string> main = new List<string>();
            main.AddRange(prog);
            main.Insert(2, "\r\n;START PROG");

            for (int i = 0; i < main.Count; i++)
            {
                if (cvel)
                {
                    if (main[i].StartsWith(@"\b C_VEL"))
                    {
                        string apoCVEL = main[i].Split(';')[1];
                        if (main[i - 1].Substring(main[i - 1].Length - 5, 5) == "C_DIS")
                            main[i - 1] = main[i - 1].Replace("C_DIS", "C_VEL");
                        main.RemoveAt(i);
                        if (prevApoCVEL != apoCVEL) main.Insert(i - 1, apoCVEL);
                        else i -= 1;
                        prevApoCVEL = apoCVEL;
                    }
                }
                if (trigger)
                {
                    if (main[i].StartsWith("CONTINUE\r\n"))
                    {
                        if (main[i].StartsWith("CONTINUE\r\nWAIT")) goto EndofProg;
                        if (i + 8 >= main.Count)
                        {
                            for (int j = 1; j < 8; j++)
                            {
                                if (main[i + j].StartsWith("END"))
                                {
                                    goto EndofProg;
                                }
                                else if (main[i + j].StartsWith("CONTINUE\r\n"))
                                {
                                    continue;
                                }
                                else break;
                            }
                        }
                        main[i] = main[i].Replace("CONTINUE\r\n", "TRIGGER WHEN DISTANCE=0 DELAY=0 DO ");
                        continue;
                    EndofProg:
                        main[i] = main[i].Replace("CONTINUE\r\n", "");
                        continue;
                    }
                }
            }

            List<string> all = header.GetRange(0, 3);
            string name = all[2].Substring(4).Split(new string[] { "_T_ROB" }, StringSplitOptions.RemoveEmptyEntries)[0];
            // Check implemented in new version of robots
            //StringBuilder nameFix = new StringBuilder();
            //foreach (char c in name)
            //{
            //    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
            //        nameFix.Append(c);
            //}
            //name = !char.IsLetter(nameFix[0]) ?
            //    "KUKA_" + nameFix.ToString().Substring(0, Math.Min(nameFix.Length, 19)) :
            //    nameFix.ToString().Substring(0, Math.Min(nameFix.Length, 24));

            all[2] = "DEF " + name + "()";
            all.Add("\r\n;DAT DECL");
            all.AddRange(declare.Keys);
            all.Add("\r\n;INI");
            foreach (var de in declare.Keys)
            {
                all.Add(de.Split(' ')[2] + " = " + declare[de]);
            }
            all.AddRange(header.GetRange(3, header.Count - 3));
            all.AddRange(main);

            bool S = false;
            if (save && DA.GetData(2, ref S))
            {
                string P = string.Empty;
                DA.GetData(1, ref P);

                if (string.IsNullOrEmpty(P))
                    P = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (S)
                {
                    try
                    {
                        string path = Path.Combine(P, name + ".src");
                        File.WriteAllLines(path, all);
                        error = false;
                        msg = DateTime.Now.ToString("HH:mm:ss") + " Saved as " + name + ".src\n@" + path;
                    }
                    catch (Exception e)
                    {
                        error = true;
                        msg = e.Message;
                    }
                }
                AddRuntimeMessage(error ? GH_RuntimeMessageLevel.Error : GH_RuntimeMessageLevel.Remark, msg);
            }

            DA.SetData(0, name + ".src");
            DA.SetDataList(1, all);
        }
        bool error = false;
        string msg = string.Empty;

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
        bool save = false;
        readonly Param_String param = new Param_String { Name = "Directory", NickName = "P", Description = "Specify Path where file will be saved\nIf not specified, will try to save to Desktop", Optional = true };
        readonly Param_Boolean param2 = new Param_Boolean { Name = "Save", NickName = "S", Description = "Button or toggle to specify saving", Optional = false };
        private void SaveInputs(object sender, EventArgs e)
        {
            save = !save;
            if (save)
            {
                Params.RegisterInputParam(param);
                Params.RegisterInputParam(param2);
            }
            else
            {
                for(int i =2;i>0; i--)
                    Params.UnregisterInputParameter(Params.Input[i], true);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("ShowSave", save);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            save = reader.GetBoolean("ShowSave");
            return base.Read(reader);
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.KRL;
        public override Guid ComponentGuid => new Guid("309454cf-ea5e-470f-80a8-fc19e3729dfc");
    }
    public class KukaCVEL : GH_Component
    {
        public KukaCVEL()
          : base("Speed Approximation", "CVEL",
              "Commands the robot to maintain defined speed percentage by zoning (Custom Command)",
              "Robots", "Commands")
        { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Speed Percentage", "%", "Speed % to maintain [0-100]", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Command", "C", "Command", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int percentage = 50;
            DA.GetData(0, ref percentage);
            percentage = Math.Max(percentage, 0);
            percentage = Math.Min(percentage, 100);
            string manufacturerText = "KUKA",
                code = $"\\b C_VEL;$APO.CVEL=Zonev{percentage}",
                declaration = $"DECL GLOBAL REAL Zonev{percentage} = {percentage}";

            var command = new Robots.Commands.Custom("SpeedApproximation");
            if (!Enum.TryParse<Manufacturers>(manufacturerText, out var manufacturer))
            {
                throw new ArgumentException($"Manufacturer {manufacturerText} not valid.");
            }
            command.AddCommand(manufacturer, code, declaration);
            DA.SetData(0, command);
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Speed_Approximation;
        public override Guid ComponentGuid => new Guid("79B3841F-6BCE-4D80-B665-B6DF637C1797");
    }
    public class KukaRotateEuler : GH_Component
    {
        public KukaRotateEuler() : base("Rotate Euler", "RotEuler", "Rotate an object with (KUKA) Euler notation", "Transform", "Euclidean") { }
        public override Guid ComponentGuid => new Guid("F838B4F6-42FA-4D77-9615-F6B2D142BA68");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Rotate_Euler;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "Geo", "Geometry or plane to rotate", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Rotation Plane", "Pln", "Plane to use for center or rotation", GH_ParamAccess.item);
            pManager.AddNumberParameter("Z-Axis", "A", "Degree of rotation on Z-Axis", GH_ParamAccess.item);
            pManager.AddNumberParameter("Y-Axis", "B", "Degree of rotation on Y-Axis", GH_ParamAccess.item);
            pManager.AddNumberParameter("X-Axis", "C", "Degree of rotation on X-Axis", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Rotated geometry", GH_ParamAccess.item);
            pManager.AddTransformParameter("Transform", "X", "Transformation data", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            IGH_GeometricGoo geo = null;
            Plane pln = Plane.Unset;
            double a = 0, b = 0, c = 0;
            DA.GetData(0, ref geo);
            DA.GetData(1, ref pln);
            DA.GetData(2, ref a);
            DA.GetData(3, ref b);
            DA.GetData(4, ref c);
            if (geo != null)
                geo = geo.DuplicateGeometry();
            if (pln == Plane.Unset && geo is GH_Plane p)
            {
                GH_Convert.ToPlane(p, ref pln, GH_Conversion.Both);
            }
            else if (geo == null && pln != Plane.Unset)
            {
                geo = GH_Convert.ToGeometricGoo(pln);
            }
            else if (pln == Plane.Unset || geo == null)
            {
                string e;
                if (pln == Plane.Unset)
                    e = Grasshopper.CentralSettings.CanvasFullNames ? Params.Input[1].Name : Params.Input[1].NickName;
                else
                    e = Grasshopper.CentralSettings.CanvasFullNames ? Params.Input[0].Name : Params.Input[0].NickName;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Input parameter {e} failed to collect data");
                return;
            }

            double r = Math.PI / 180;
            Transform x = Transform.Rotation(a * r, pln.ZAxis, pln.Origin);
            x *= Transform.Rotation(b * r, pln.YAxis, pln.Origin);
            x *= Transform.Rotation(c * r, pln.XAxis, pln.Origin);

            DA.SetData(0, geo.Transform(x));
            DA.SetData(1, x);
        }
    }
    public class KukaTCPsender : GH_Component
    {
        public KukaTCPsender() : base("Remote KUKA", "Remote", "Interop with KUKA controller through a network. [Requires KukaVarProxy]", "Robots", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.senary; //| GH_Exposure.obscure;
        public override Guid ComponentGuid => new Guid("D746BC1B-CA90-401D-ACDC-9320D2AD0844");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.KukaVarProxyConnect;
        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.ObjectsDeleted += ThisRemoved;
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            Menu_AppendItem(menu, "Timeout (ms)", (s, e) => { }, false);
            Menu_AppendCustomItem(menu, rTO);
            Menu_AppendItem(menu, "Keep Alive", (s, e) =>
            {
                bgKeepAlive = !bgKeepAlive;
                if (bgKeepAlive)
                    BackgroundTask();
                else
                {
                    try
                    {
                        if (worker != null)
                        {
                            worker.CancelAsync();
                            worker.Dispose();
                        }
                    }
                    catch { }
                    worker = null;
                }
            }, true, bgKeepAlive);
            //Menu_AppendCustomItem(menu, rKA);
            //Menu_AppendSeparator(menu);
            //Menu_AppendItem(menu, "Clear Log", (s, e) =>
            //{
            //    log = new List<string>();
            //    UpdateOut(null, null);
            //}, true);
            //Menu_AppendItem(menu, "Update Logs now", (s, e) => UpdateOut(null, null), true);
            Menu_AppendItem(menu, "Keep Logs", (s, e) => logNewestOnly = !logNewestOnly, true, !logNewestOnly);
#if (DEBUG)
            Menu_AppendItem(menu, "DEBUG", (s, e) => debug = !debug, true, debug);
#endif
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Command", "C", "Command to send, will query joint angle if nothing connected", GH_ParamAccess.list, "$AXIS_ACT");
            pManager.AddTextParameter("IP Address", "IP", "IP address of controller", GH_ParamAccess.item, "0.0.0.0"/*, "192.168.0.1" /* "172.31.1.147"*/);
            pManager.AddIntegerParameter("Port", "P", "Port to use for communications", GH_ParamAccess.item, 7000);
            pManager.AddBooleanParameter("Execute Program", "E", "Execute", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "L", "Log", GH_ParamAccess.list);
        }

        TcpClient client;
        NetworkStream stream = null;
        NumericUpDown rTO = new NumericUpDown
        {
            Minimum = 250,
            Maximum = 10000,
            Increment = 50,
            Value = 5000,
            DecimalPlaces = 0
        };
        BackgroundWorker worker;
        string ip = string.Empty;
        bool bgKeepAlive = true;
        List<string> log = new List<string>();
        bool logNewestOnly = true;
#if(DEBUG)
        bool debug = false;
#endif
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> usercmd = new List<string>();
            bool run = false;
            int port = 7000;
            DA.GetData("IP Address", ref ip);
            DA.GetData("Port", ref port);
            DA.GetDataList("Command", usercmd);
            DA.GetData("Execute Program", ref run);
#if (DEBUG)
            if (ip == "0.0.0.0")
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    ip = endPoint.Address.ToString();
                }
            }
#endif
            if (run)
            {
                if (rTO.Value > rTO.Maximum) rTO.Value = rTO.Maximum;
                else if (rTO.Value < rTO.Minimum) rTO.Value = rTO.Minimum;
                int timeout = (int)rTO.Value;
                if (client == null || client.Connected == false)
                {
                    try
                    {
                        client = new TcpClient
                        {
                            ReceiveTimeout = timeout,
                            SendTimeout = timeout,
                            ReceiveBufferSize = 2048 // Match KVP, too big creates GDI+ error in GH
                        };
                        if (client.ConnectAsync(ip, port).Wait(timeout))
                        {
                            stream = client.GetStream();
                            stream.WriteTimeout = timeout;
                            stream.ReadTimeout = timeout;
                        }
                        else
                            throw new TimeoutException($"A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond {ip}:{port} within {timeout}ms");
                    }
                    catch (Exception e)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Communication failed");
#if (DEBUG)
                        log.Add(e.ToString());
#else
                        log.Add(e.Message);
#endif
                        DA.SetDataList(0, log);
                        return;
                    }
                }
                if (bgKeepAlive && worker == null)
                {
                    BackgroundTask();
                }
#if (DEBUG)
                byte[] tcpmsg;
                if (!debug)
                {
                    tcpmsg = EncodeMsg(usercmd);
                }
                else
                {
                    try
                    { tcpmsg = Array.ConvertAll(usercmd[0].Split(' '), s => (byte)int.Parse(s)); }
                    catch
                    { tcpmsg = new byte[] { 0, 0, 0, 2, 0, 0 }; }
                }
#else
                byte[] tcpmsg = EncodeMsg(usercmd);
#endif
                SendMsg(tcpmsg);
            }
            else if (client != null)
            {
                CloseConnection();
            }
            DA.SetDataList(0, log);
        }
        void BackgroundTask()
        {
            worker = new BackgroundWorker();
            worker.DoWork += KeepAlive;
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerAsync();
        }
        void KeepAlive(object sender, DoWorkEventArgs e)
        {
            //(ASCII)"$AXIS_ACT"=="0004000C00000924415849535F414354"(HEX) == "0 0 0 12 0 0 9 36 65 88 73 83 95 65 67 84 "(byte)
            byte[] poke = new byte[] { 100, 100, 0, 2, 0, 0 };
            while (bgKeepAlive)
            {
                try
                {
                    /*if (rKA.Value > rKA.Maximum) rKA.Value = rKA.Maximum;
                    else if (rKA.Value < rKA.Minimum) rKA.Value = rKA.Minimum;*/
                    System.Threading.Thread.Sleep(10000);//(int)rKA.Value);
                    if (stream == null) break;
                    SendMsg(poke);
                }
                catch (Exception er)
                {
#if (DEBUG)
                    log.Add(er.ToString());
#else
                    log.Add(er.Message);
#endif
                    CloseConnection();
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Communication failed");
                }
            }
        }
        byte[] EncodeMsg(List<string> message)
        {
            List<byte> msg = new List<byte>();
            // LittleEndian - no need to shift list to add header (ID and Req Length)
            for (int i = message.Count - 1; i >= 0; i--)
            {
                /*if (!(message[i][0] == '$'))
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"[{i}] is not a variable command, skipped");
                    continue; // Skip if not a variable
                }*/
                List<byte> temp = new List<byte>();
                for (int c = message[i].Length - 1; c >= -1; c--)
                {
                    if (c == -1 || message[i][c] == '=')
                    {
                        int length = temp.Count;
                        msg.AddRange(temp);
                        msg.AddRange(new byte[] { (byte)(length % 256), (byte)(length / 256) });
                        temp = new List<byte>();
                        continue;
                    }
                    //if (message[i][c] == ' ') continue; // Spaces needed to formate msg correctly
                    temp.Add(BitConverter.GetBytes(message[i][c])[0]); // Convert to byte, ASCII fits in 0-255, only uses [0], [1] always 0
                }
            }
            if (msg.Count == 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid command detected");
                return null;
            }
            // Mode: 0=Read, 1=Write, (2=ReadArray, 3=WriteArray) Array not implemented in KVP
            msg.Add((byte)(message[0].Contains('=') ? 1 : 0));
            /*message.Count > 1 ? 3 : 1 :
            message.Count > 1 ? 2 : 0));*/
            // Request Length
            msg.AddRange(new byte[] { (byte)(msg.Count % 256), (byte)(msg.Count / 256) });
            // Request ID
            msg.AddRange(new byte[] { (byte)DateTime.Now.Second, (byte)DateTime.Now.Minute });

            msg.Reverse();
            return msg.ToArray();
        }
        void SendMsg(byte[] tcpmsg)
        {
            if (tcpmsg == null) return;
            if (logNewestOnly) log = new List<string>();
#if (DEBUG)
            log.Add("_Send (Byte):\r\n" + string.Join(" ", tcpmsg));
#endif
            stream.Write(tcpmsg, 0, tcpmsg.Length);
            var response = new byte[client.ReceiveBufferSize];
            string responseData = string.Empty;
            bool error = false;
            int rLength = 11;
            try
            {
                rLength = stream.Read(response, 0, client.ReceiveBufferSize);
                error = response[rLength - 3] == 0 &&
                        response[rLength - 2] == 0 &&
                        response[rLength - 1] == 0;
            }
            catch
            {
                //rLength = 11;
                error = true;
                response = new byte[] { 78, 111, 32, 82, 101, 115, 112, 111, 110, 115, 101 };
            }
            string respString = string.Join(" ", new ArraySegment<byte>(response, 0, rLength).ToArray());
            responseData = (error ? "_Error " : "") + respString;
            log.Add(responseData);
        }
        /*void UpdateOut(Object sender, ProgressChangedEventArgs e)//Need some otherway to update
        {
            var GhDoc = OnPingDocument();
            if (GhDoc != null)
            {
                this.Params.Output[0].ClearData();
                access.SetDataList(0, log);
                foreach (var recpt in this.Params.Output[0].Recipients)
                {
                    if (recpt.Phase == GH_SolutionPhase.Computed)
                        recpt.ExpireSolution(false);
                }
                GhDoc.ScheduleSolution(1);
            }
        }*/
        void ThisRemoved(object sender, GH_DocObjectEventArgs e)
        {
            if (e.Objects.Contains(this))
            {
                e.Document.ObjectsDeleted -= ThisRemoved;
                CloseConnection();
            }
        }
        void CloseConnection()
        {
            try
            {
                if (worker != null)
                {
                    worker.CancelAsync();
                    worker.Dispose();
                }
                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                    stream.Close();
                    stream.Dispose();
                }
            }
            catch { }
            worker = null;
            client = null;
            stream = null;
            log.Add("_Connection Terminated.");
        }
    }
    /*public class KukaTCPresponse : GH_Component
    {
        public KukaTCPresponse() : base("Deconstruct TCP (AXIS)", "DeAXIS", "Deconstruct the KeepAlive response of Remote KUKA to get axis angles", "Robots", "Utility") { }
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("7EC30372-DF94-4380-B713-4F0EA7AD8899");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "L", "Log from Remote Kuka to deconstruct", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Joints", "J", "Controller angle in degrees for A1 to A6", GH_ParamAccess.list);
            pManager.AddNumberParameter("Extneral", "E", "Controller angle in degrees for E1 and E2", GH_ParamAccess.list);
        }
        List<double> joints = new List<double>();
        List<double> external = new List<double>();

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> log = new List<string>();
            DA.GetDataList("Log", log);
            int last = -1;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                if (!(log[i][0] == '_') && log[i][0] == '0' && log[i][2] == '0')
                {
                    last = i;
                    break;
                }
            }
            if (last == -1)
            {
                DA.SetDataList("Joints", joints);
                DA.SetDataList("Extneral", external);
                return;
            }
            List<byte> ary = new List<byte>();
            Array.ForEach(log[last].Split(' '), b => ary.Add((byte)Convert.ToInt32(b)));
            string[] response = Encoding.ASCII.GetString(ary.ToArray()).Split(new string[] { "{E", ", E1 ", "}" }, StringSplitOptions.None);
            joints = Extract(response[1]);
            DA.SetDataList("Joints", joints);
            if (response.Length == 4)
            {
                external = Extract(response[2]);
                DA.SetDataList("Extneral", external);
            }
            List<double> Extract(string re)
            {
                List<double> outList = new List<double>();
                Array.ForEach(re.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries), e =>
                {
                    if (Double.TryParse(e, out double d))
                    {
                        outList.Add(d);
                    }
                });
                return outList;
            }
        }
        
    }
    */

    public class UpdateTool : GH_Component
    {
        public UpdateTool() : base("Update Tool", "newTCP", "Update the TCP of an exsisting tool", "Robots", "Utility") { }
        //public override GH_Exposure Exposure => GH_Exposure.hidden;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.UpdateTool;
        public override Guid ComponentGuid => new Guid("92915A29-8636-4670-B21C-756D681789E4");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("GH_Tool", "T", "Tool to update TCP location", GH_ParamAccess.item);
            pManager.AddPlaneParameter("TCP", "P", "New TCP to use", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tool", "T", "Tool", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Tool input = null;
            Plane tcp = new Plane();
            DA.GetData(0, ref input);
            DA.GetData(1, ref tcp);

            var o = input.Value;

            Tool tool = new Tool(tcp, o.Name, o.Weight, o.Centroid, o.Mesh);

            DA.SetData(0, tool);
        }
    }

    public class DeconstructTool : GH_Component
    {
        public DeconstructTool() : base("Deconstruct Tool", "DeTool", "Retrieves properties of an exsisting tool", "Robots", "Utility") { }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.DeTool;
        public override Guid ComponentGuid => new Guid("753CDC90-7278-45C4-91D4-B476BA34D396");
        readonly string tName = nameof(tName);
        readonly string TCP = nameof(TCP);
        readonly string Weight = nameof(Weight);
        readonly string Centroid = nameof(Centroid);
        readonly string Mesh = nameof(Mesh);

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("GH_Tool", "T", "Tool to update TCP location", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(tName, "N", "Tool name", GH_ParamAccess.item);
            pManager.AddPlaneParameter(TCP, "P", "TCP plane", GH_ParamAccess.item);
            pManager.AddNumberParameter(Weight, "W", "Tool weight in kg", GH_ParamAccess.item);
            pManager.AddPointParameter(Centroid, "C", "Optional tool center of mass", GH_ParamAccess.item);
            pManager.AddMeshParameter(Mesh, "M", "Tool geometry", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            dynamic input = null;
            DA.GetData(0, ref input);

            var t = input.Value;

            DA.SetData(tName, t.Name);
            DA.SetData(TCP, t.Tcp);
            DA.SetData(Weight, t.Weight);
            DA.SetData(Centroid, t.Centroid);
            DA.SetData(Mesh, t.Mesh);
        }
    }
}
