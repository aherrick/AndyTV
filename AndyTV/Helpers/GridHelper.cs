namespace AndyTV.Helpers;

public static class GridHelper
{
    public static void ApplyStandardRowHeight(DataGridView grid)
    {
        if (grid is null)
            throw new ArgumentNullException(nameof(grid));

        var baseHeight = TextRenderer.MeasureText("Xg", grid.Font).Height;
        var rowHeight = (int)(baseHeight * 1.6);
        grid.RowTemplate.Height = rowHeight;
    }
}
