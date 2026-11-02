namespace YimyWalk.App.UI.Controls;

/// <summary>A per-pet numeric setting with explicit inheritance and quiet host refresh.</summary>
internal sealed class NumberOverrideEditor : FlowLayoutPanel
{
    private readonly AntdUI.Switch _inherit = UiTheme.CreateSwitch(isChecked: true);
    private readonly AntdUI.InputNumber _number;
    private readonly decimal _displayFactor;
    private Guid? _target;
    private bool _binding;

    internal NumberOverrideEditor(decimal minimum, decimal maximum, decimal increment, int decimals, decimal displayFactor = 1)
    {
        if (displayFactor <= 0) throw new ArgumentOutOfRangeException(nameof(displayFactor));
        _displayFactor = displayFactor;
        AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
        WrapContents = false; Margin = Padding.Empty;
        var label = UiTheme.CreateMutedLabel("跟随全局");
        label.Margin = new Padding(0, 10, 4, 0);
        _inherit.AccessibleName = "跟随全局";
        _inherit.Margin = new Padding(0, 6, 10, 0);
        _number = UiTheme.CreateInputNumber(minimum, maximum, increment, decimals, 100);
        _number.Enabled = false;
        Controls.Add(label); Controls.Add(_inherit); Controls.Add(_number);
        _inherit.CheckedChanged += (_, _) =>
        {
            _number.Enabled = !_inherit.Checked;
            if (!_binding) ValueChanged?.Invoke(this, EventArgs.Empty);
        };
        _number.ValueChanged += (_, _) =>
        {
            if (!_binding && !_inherit.Checked) ValueChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    internal event EventHandler? ValueChanged;
    internal float? OverrideValue => _inherit.Checked ? null : (float)(_number.Value / _displayFactor);

    internal void SetAccessibleLabel(string title)
    {
        AccessibleName = _number.AccessibleName = title;
        _inherit.AccessibleName = $"{title}跟随全局";
    }

    internal void Bind(Guid target, float? custom, float inherited)
    {
        _binding = true;
        try
        {
            _inherit.Checked = custom is null;
            // A new selection must replace a focused editor. An ordinary
            // periodic refresh must leave an in-progress edit alone.
            if (_target != target || !_number.ContainsFocus || custom is null)
                _number.Value = (decimal)(custom ?? inherited) * _displayFactor;
            _number.Enabled = custom is not null;
            _target = target;
        }
        finally { _binding = false; }
    }
}
