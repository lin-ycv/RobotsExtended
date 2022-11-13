using Grasshopper.Kernel;
using Rhino.Geometry;
using Robots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotsExtended.Util
{
    public class UpdateFrame : GH_Component
    {
        public UpdateFrame() : base("Update Frame", "newBase", "Update frame info", "Robots", "Utility") { }
        public override Guid ComponentGuid => new Guid("{3BAA462A-7D1B-401A-B789-2E12B878B6AD}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.UpdateFrame;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Frame", "F", "Frame to configure", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "New frame", GH_ParamAccess.item, Plane.Unset);
            pManager.AddIntegerParameter("Base", "B", "Base number to read from, -1 to hard-code", GH_ParamAccess.item, -1);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Frame", "F", "Frame", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Frame frame = null;
            Plane plane = Plane.Unset;
            int? number = null;
            DA.GetData(0, ref frame);
            DA.GetData(1, ref plane);
            DA.GetData(2, ref number);
            if (number < 0) number = null;
            Frame uFrame = new Frame(plane == Plane.Unset ? frame.Plane : plane, frame.CoupledMechanism, frame.CoupledMechanicalGroup, frame.HasName ? frame.Name : null, number.HasValue, number);
            DA.SetData(0, uFrame);
        }
    }
}
