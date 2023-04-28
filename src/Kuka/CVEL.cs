using Grasshopper.Kernel;
using Robots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotsExtended.Kuka
{
    public class CVEL : GH_Component
    {
        public CVEL()
          : base("Speed Approximation", "CVEL",
              "Commands the robot to maintain defined speed percentage by zoning (Custom Command)\r\nShould be the first command if in a command group",
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

}
