namespace Caribou.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Grasshopper.Kernel;
    using Caribou.Models;
    using Caribou.Forms;
    using Caribou.Forms.Models;
    using Caribou.Processing;
    using Caribou.Properties;
    using Eto.Forms;

    /// <summary>Discovers and selects OSM tags from a file interactively</summary>
    public class DiscoverTagsComponent : BasePickComponent
    {
        private string lastFilePath = "";
        private int lastElementTypeFilter = 0;

        public DiscoverTagsComponent()
            : base("Discover Tags", "OSM Discover",
                   "Discover and select tags from an OSM file", "Pick")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("OSM File", "F",
                "Path to OSM file to discover tags from", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Element Type", "ET",
                "Filter tags by element type: 0=All, 1=Nodes, 2=Ways, 3=Relations",
                GH_ParamAccess.item, 0);
        }

        protected override void CaribouRegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("OSM Features", "OF",
                "Selected OSM features and subfeatures", GH_ParamAccess.list);
        }

        protected override void CaribouSolveInstance(IGH_DataAccess da)
        {
            // Get file path input
            string filePath = "";
            if (!da.GetData(0, ref filePath)) return;

            filePath = filePath.Replace("\n", "").Replace("\r", "").Trim();

            // Get element type filter
            int elementTypeFilter = 0;
            da.GetData(1, ref elementTypeFilter);

            // Check if file exists
            if (!System.IO.File.Exists(filePath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Could not find file at '{filePath}'");
                return;
            }

            // If file changed or filter changed, rebuild selectableOSMs
            if (filePath != lastFilePath || elementTypeFilter != lastElementTypeFilter)
            {
                this.selectableOSMs = GetSelectableTagsFromFile(filePath, elementTypeFilter);
                this.lastFilePath = filePath;
                this.lastElementTypeFilter = elementTypeFilter;

                // Restore previous selections if available
                if (this.storedSelectionState != null)
                {
                    this.selectableOSMs = TreeGridUtilities.SetSelectionsFromStoredState(
                        this.selectableOSMs, this.storedSelectionState);
                }
            }

            // Output selected tags
            this.selectionStateSerialized = GetSelectedKeyValuesFromForm();
            this.OutputMessageBelowComponent();
            da.SetDataList(0, selectionStateSerialized);
        }

        private TreeGridItemCollection GetSelectableTagsFromFile(string filePath, int elementTypeFilter)
        {
            // Convert filter to OSMGeometryType
            OSMGeometryType? filterType = elementTypeFilter switch
            {
                1 => OSMGeometryType.Node,
                2 => OSMGeometryType.Way,
                3 => OSMGeometryType.Relation,
                _ => null // 0 or invalid = all types
            };

            // Extract all tags from file filtered by element type
            var tagsByKey = ParseViaXMLReader.ExtractAllUniqueTagsFiltered(filePath, filterType);

            // Convert to OSMTag objects with parent-child hierarchy
            var indexOfParents = new Dictionary<string, int>();
            var selectableTags = new TreeGridItemCollection();

            // Sort keys alphabetically
            var sortedKeys = tagsByKey.Keys.OrderBy(k => k).ToList();

            foreach (var key in sortedKeys)
            {
                // Create parent item (key=*)
                var parentTag = new OSMTag(key);
                var parentItem = new OSMTreeGridItem(parentTag, 0, 0, false, false, false);
                parentItem.IsParsed = true;  // Mark as discovered from file

                selectableTags.Add(parentItem);
                indexOfParents[key] = selectableTags.Count - 1;

                // Sort values alphabetically
                var sortedValues = tagsByKey[key].OrderBy(v => v).ToList();

                // Create child items (key=value)
                foreach (var value in sortedValues)
                {
                    var childTag = new OSMTag(key, value);
                    // Note: childTag.Key is automatically set in the constructor

                    var childItem = new OSMTreeGridItem(childTag, 0, 0, false, false, false);
                    childItem.IsParsed = true;

                    // Put "yes" values first (common pattern from OSMPrimaryTypes.cs:68-69)
                    if (value == "yes")
                        parentItem.Children.Insert(0, childItem);
                    else
                        parentItem.Children.Add(childItem);
                }
            }

            return selectableTags;
        }

        protected override BaseForm GetFormForComponent()
            => new DiscoverTagsForm(this.selectableOSMs, this.hideObscureFeatures);

        protected override string GetButtonTitle() => "Discover\nTags";

        protected override string GetNoSelectionMessage() => "No Tags Selected";

        public override Guid ComponentGuid => new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        protected override System.Drawing.Bitmap Icon => Resources.icons_select;
    }
}
