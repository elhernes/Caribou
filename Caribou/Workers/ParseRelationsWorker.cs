namespace Caribou.Workers
{
    using System.Collections.Generic;
    using Caribou.Components;
    using Caribou.Models;
    using Caribou.Processing;
    using Grasshopper.Kernel;
    using Grasshopper.Kernel.Data;
    using Grasshopper.Kernel.Types;
    using Rhino.Geometry;

    /// <summary>Asynchronous task to identify and output all OSM relations as Breps or Curves for a given request.</summary>
    public class ParseRelationsWorker : BaseLoadAndParseWorker
    {
        private IGH_Structure relationOutputs;
        private Dictionary<OSMTag, List<Brep>> foundRelationsBreps;
        private Dictionary<OSMTag, List<PolylineCurve>> foundRelationsCurves;
        private bool outputMerged;

        protected override OSMGeometryType WorkerType()
        {
            return OSMGeometryType.Relation;
        }

        public ParseRelationsWorker(GH_Component parent)
            : base(parent)
        {
        }

        protected override void GetExtraData(IGH_DataAccess da)
        {
            base.GetExtraData(da);
            da.GetData(2, ref this.outputMerged);
        }

        public override WorkerInstance Duplicate() => new ParseRelationsWorker(this.Parent);

        public override void MakeGeometryForComponentType()
        {
            if (this.outputMerged)
            {
                // Translate OSM relations to merged Rhino Breps with holes
                this.foundRelationsBreps = TranslateToXYManually.RelationBrepsFromCoords(this.result);
            }
            else
            {
                // Translate OSM relations to separate polylines for outer/inner rings
                this.foundRelationsCurves = TranslateToXYManually.RelationPolylinesFromCoords(this.result);
            }
        }

        public override void GetTreeForComponentType()
        {
            if (this.outputMerged)
            {
                this.relationOutputs = TreeFormatters.MakeTreeForRelations(this.foundRelationsBreps);
            }
            else
            {
                this.relationOutputs = TreeFormatters.MakeTreeForRelationPolylines(this.foundRelationsCurves, this.result);
            }
        }

        public override void OutputTreeForComponentType(IGH_DataAccess da)
        {
            if (this.relationOutputs != null)
            {
                da.SetDataTree(0, this.relationOutputs);
                if (this.relationOutputs.DataCount == 0)
                    this.RuntimeMessages.Add(new Message(
                        "No relations were found with the specified features or tags.",
                        Message.Level.Warning));
            }
        }
    }
}
