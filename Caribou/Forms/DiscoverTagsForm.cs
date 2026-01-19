namespace Caribou.Forms
{
    using Caribou.Forms.Models;
    using Eto.Forms;

    public class DiscoverTagsForm : BaseForm
    {
        public DiscoverTagsForm(TreeGridItemCollection selectionState, bool hideObscure)
            : base(selectionState, "Discover Tags From File", hideObscure)
        {
        }

        protected override string GetLabelForHideObscure()
            => " Hide unique/rare tags (name, ref, id, etc.)";
    }
}
