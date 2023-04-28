using GH_IO.Serialization;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobotsExtended.Kuka
{
    public class MergeKRL : GH_Component, IGH_VariableParameterComponent
    {
        public MergeKRL()
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
            Menu_AppendItem(menu, "Fold", (s, e) => { fold = !fold; ExpireSolution(true); }, true, fold); ;
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
            List<string> declOrg = string.Join(Environment.NewLine, code.Branches[1].Select(x => x.Value)).Split(new string[] { "\r\n", "\r", "\n", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList(); ;
            for (int i = 1; i < declOrg.Count - 1; i++)// Skip RVP+REL and ENDDAT
            {
                if (!declOrg[i].Contains("DECL")) continue;
                string[] de = declOrg[i].Split(new string[] { " = ", " =", "= ", "=" }, StringSplitOptions.RemoveEmptyEntries);
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
            if (fold)
                main.Insert(2, "\r\n;FOLD");
            else
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
            if (fold)
                main.Insert(main.Count - 1, ";ENDFOLD");

            List<string> all = header.GetRange(0, 3);
            string name = all[2].Substring(4).Split(new string[] { "_T_ROB" }, StringSplitOptions.RemoveEmptyEntries)[0];

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
        void IGH_VariableParameterComponent.VariableParameterMaintenance() {}
        bool save = false;
        bool fold = false;
        readonly Param_String param = new Param_String { Name = "Directory", NickName = "P", Description = "Specify Path where file will be saved\nIf not specified, will try to save to Desktop", Optional = true };
        readonly Param_Boolean param2 = new Param_Boolean { Name = "Save", NickName = "S", Description = "Button or toggle to specify saving", Optional = false };
        private void SaveInputs(object sender, EventArgs e)
        {
            RecordUndoEvent("Enable/disable Save param");
            save = !save;
            if (save)
            {
                Params.RegisterInputParam(param);
                Params.RegisterInputParam(param2);
            }
            else
            {
                for (int i = 2; i > 0; i--)
                    Params.UnregisterInputParameter(Params.Input[i], true);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("ShowSave", save);
            writer.SetBoolean("FoldKRL", fold);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            save = reader.GetBoolean("ShowSave");
            fold = reader.GetBoolean("FoldKRL");
            return base.Read(reader);
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.KRL;
        public override Guid ComponentGuid => new Guid("309454cf-ea5e-470f-80a8-fc19e3729dfc");
    }

}
