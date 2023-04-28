using GH_IO.Serialization;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobotsExtended.Util
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
            RecordUndoEvent("Toggle params");
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
            {-185,-35,-68,-185,-119,-350},
            { 185, 135, 210, 185, 119, 350},
        };
    }

}
