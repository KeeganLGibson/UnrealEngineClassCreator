namespace UEClassCreator.Models;

public class AppSettings
{
    public string CompanyName { get; set; } = string.Empty;

    // Window geometry — NaN means "use OS default position"
    public double WindowLeft   { get; set; } = double.NaN;
    public double WindowTop    { get; set; } = double.NaN;
    public double WindowWidth  { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;

    public bool FilterUObjectOnly          { get; set; } = false;
    public bool OpenInExplorerAfterCreate  { get; set; } = false;

    // Per-project last state, keyed by .uproject path
    public Dictionary<string, string> LastOutputPaths      { get; set; } = new();
    public Dictionary<string, string> LastSelectedClasses  { get; set; } = new();
}
