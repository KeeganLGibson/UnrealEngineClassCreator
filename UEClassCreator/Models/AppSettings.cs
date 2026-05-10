namespace UEClassCreator.Models;

public class AppSettings
{
    public string CompanyName { get; set; } = string.Empty;
    public string DefaultOutputPath { get; set; } = string.Empty;
    public double WindowLeft { get; set; }
    public double WindowTop { get; set; }
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
}
