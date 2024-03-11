namespace RobotsExtended.Util
{
    public class DeconstructTool : GH_Component
    {
        public DeconstructTool() : base("Deconstruct tool", "DeTool", "Retrieves properties of an exsisting tool", "Robots", "Utility") { }
        protected override Bitmap Icon => Properties.Resources.DeTool;
        public override Guid ComponentGuid => new("753CDC90-7278-45C4-91D4-B476BA34D396");
        readonly string tName = nameof(tName);
        readonly string TCP = nameof(TCP);
        readonly string Weight = nameof(Weight);
        readonly string Centroid = nameof(Centroid);
        readonly string Mesh = nameof(Mesh);
        readonly string Number = nameof(Number);

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
            pManager.AddIntegerParameter(Number, "#", "Tool # in controller", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Tool input = null;
            DA.GetData(0, ref input);

            DA.SetData(tName, input.Value.Name);
            DA.SetData(TCP, input.Value.Tcp);
            DA.SetData(Weight, input.Value.Weight);
            DA.SetData(Centroid, input.Value.Centroid);
            DA.SetData(Mesh, input.Value.Mesh);
            DA.SetData(Number, input.Value.Number);
        }

    }

}
