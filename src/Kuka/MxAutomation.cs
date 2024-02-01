using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotsExtended.Kuka
{
    //public class MxAutomation : GH_Component
    //{
    //    public MxAutomation() : base("mxAutomation","KUKA.mxA","KUKA mxAutomation. [Requires mxA package]", "Robots", "Components") { }
    //    public override GH_Exposure Exposure => GH_Exposure.senary; //| GH_Exposure.obscure;
    //    protected override Bitmap Icon => Properties.Resources.KukaVarProxyConnect;
    //    public override Guid ComponentGuid => new Guid("{30263A03-866C-4BBF-A90A-5C3F0DC62926}");

    //    protected override void RegisterInputParams(GH_InputParamManager pManager)
    //    {
    //        pManager.AddTextParameter("", "IP", "IP Address of the controller to communicate with", GH_ParamAccess.item);

    //    }

    //    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    //    {
    //    }

    //    protected override void SolveInstance(IGH_DataAccess DA)
    //    {
    //        // TO BE IMPLEMENTED
    //    }
    //}
}
