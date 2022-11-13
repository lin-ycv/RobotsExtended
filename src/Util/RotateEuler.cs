using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotsExtended.Util
{
    public class RotateEuler : GH_Component
    {
        public RotateEuler() : base("Rotate Euler", "RotEuler", "Rotate an object with (KUKA) Euler notation", "Transform", "Euclidean") { }
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

}
