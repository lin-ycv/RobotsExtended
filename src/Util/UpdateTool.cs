namespace RobotsExtended.Util
{
    public class UpdateTool : GH_Component, IGH_VariableParameterComponent
    {
        public UpdateTool() : base("Update tool", "newTCP", "Update the TCP of an exsisting tool", "Robots", "Utility") { }
        //public override GH_Exposure Exposure => GH_Exposure.hidden;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.UpdateTool;
        public override Guid ComponentGuid => new("92915A29-8636-4670-B21C-756D681789E4");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("GH_Tool", "T", "Tool to update TCP location", GH_ParamAccess.item);
            pManager.AddPlaneParameter("TCP", "P", "New TCP to use", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Use Controller Values", ChangeInput, true, controller);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tool", "T", "Tool", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Tool input = null;
            DA.GetData(0, ref input);
            Plane tcp = new();
            DA.GetData(1, ref tcp);
            int? no = null;
            if (controller)
                DA.GetData(2, ref no);

            GH_Tool tool = new(
                new Tool(
                    tcp,
                    input.Value.Name,
                    input.Value.Weight,
                    input.Value.Centroid,
                    input.Value.Mesh,
                    null,
                    controller,
                    no
                    )
                );

            DA.SetData(0, tool);
        }
        private void ChangeInput(object sender, EventArgs e)
        {
            controller = !controller;
            if (controller)
            {
                IGH_Param p = new Param_Integer { Name = "Tool Number", NickName = "N", Description = "Tool number in controller", Optional = false };
                Params.RegisterInputParam(p);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input[2], true);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("IsCtrl", controller);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            controller = reader.GetBoolean("IsCtrl");
            return base.Read(reader);
        }

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        bool controller = false;
    }

}
