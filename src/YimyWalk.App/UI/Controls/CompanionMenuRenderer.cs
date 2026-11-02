namespace YimyWalk.App.UI.Controls;

internal sealed class CompanionMenuRenderer : ToolStripProfessionalRenderer
{
    internal CompanionMenuRenderer() : base(new Colors()) { RoundedEdges = true; }
    private sealed class Colors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => UiTheme.Cream;
        public override Color MenuBorder => UiTheme.Border;
        public override Color MenuItemSelected => UiTheme.Sidebar;
        public override Color MenuItemBorder => UiTheme.Border;
        public override Color SeparatorDark => UiTheme.Border;
        public override Color SeparatorLight => UiTheme.Cream;
    }
}
