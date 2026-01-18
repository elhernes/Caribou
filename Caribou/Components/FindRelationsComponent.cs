namespace Caribou.Components
{
    using System;
    using Caribou.Properties;
    using Caribou.Workers;
    using Grasshopper.Kernel;

    /// <summary>Identifies and outputs all OSM relation-type items that contain any of the requested metadata. Logic in worker.</summary>
    public class FindRelationsComponent : BaseFindComponent
    {
        public FindRelationsComponent()
            : base("Extract Relations", "Relations",
                   "Load and parse relation data (e.g. multipolygons, boundaries) from an OSM file based on its metadata")
        {
            this.BaseWorker = new ParseRelationsWorker(this);
        }

        protected override void RegisterExtraInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Output Merged", "OM?",
                "If true, outputs merged Breps with holes. If false, outputs separate polylines for outer/inner rings.",
                GH_ParamAccess.item, true);
        }

        protected override void CaribouRegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Relations", "R",
                "Relations as Breps with holes (merged) or Curves (outer/inner polylines)",
                GH_ParamAccess.tree);
            AddCommonOutputParams(pManager);
        }

        public override Guid ComponentGuid => new Guid("e7c2d132-3c6e-4fb7-acf3-fc479de743b2");

        protected override System.Drawing.Bitmap Icon => Resources.icons_buildings;
    }
}
