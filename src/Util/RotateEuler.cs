namespace RobotsExtended.Util
{
    public class RotateEuler : GH_Component
    {
        public RotateEuler() : base("Rotate Euler", "RotEuler", "Rotate an object with (KUKA) Euler notation", "Transform", "Euclidean") { }
        public override Guid ComponentGuid => new("F838B4F6-42FA-4D77-9615-F6B2D142BA68");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Rotate_Euler;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            // Switched to list input for better performance
            pManager.AddGeometryParameter("Geometry", "Geo", "Geometry or plane to rotate", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Rotation Plane", "Pln", "Plane to use for center or rotation", GH_ParamAccess.list);
            pManager.AddNumberParameter("Z-Axis", "A", "Degree of rotation on Z-Axis", GH_ParamAccess.list);
            pManager.AddNumberParameter("Y-Axis", "B", "Degree of rotation on Y-Axis", GH_ParamAccess.list);
            pManager.AddNumberParameter("X-Axis", "C", "Degree of rotation on X-Axis", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Rotated geometry", GH_ParamAccess.list);
            pManager.AddTransformParameter("Transform", "X", "Transformation data", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<IGH_GeometricGoo> geo = [];
            List<GH_Plane> pln = [];
            List<GH_Number>[] ro = [[], [], []];

            DA.GetDataList(0, geo);
            DA.GetDataList(1, pln);
            DA.GetDataList(2, ro[0]);
            DA.GetDataList(3, ro[1]);
            DA.GetDataList(4, ro[2]);

            for (int i = 0; i < geo.Count; i++) 
                geo[i] = geo[i].DuplicateGeometry();
            if (geo.Count == 0 && pln.Count > 0)
                geo.AddRange(pln.Select(p=> GH_Convert.ToGeometricGoo(p).DuplicateGeometry()));
            for (int i = 0; i < ro.Length; i++)
                if (!ro[i].Any())
                    ro[i].Add(new GH_Number(0));

            List<Transform> transforms = [];
            double r = Math.PI / 180;
            for (int i = 0; i < pln.Count; i++)
            {
                if (i > geo.Count) break;
                Transform x = Transform.Rotation(ro[0][Math.Min(i, ro[0].Count-1)].Value * r, pln[i].Value.ZAxis, pln[i].Value.Origin);
                x *= Transform.Rotation(ro[1][Math.Min(i, ro[1].Count - 1)].Value * r, pln[i].Value.YAxis, pln[i].Value.Origin);
                x *= Transform.Rotation(ro[2][Math.Min(i, ro[2].Count - 1)].Value * r, pln[i].Value.XAxis, pln[i].Value.Origin);
                transforms.Add(x);
                geo[i].Transform(x);
            }
            

            DA.SetDataList(0, geo);
            DA.SetDataList(1,transforms);
        }
    }

}
