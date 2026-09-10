namespace PropEngine.Core.Signals;

public sealed class Setup
{
    public SetupKind Kind { get; init; }
    public SideKind Side { get; init; }
    public double Entry { get; init; }
    public double Stop { get; init; }
    public double Target { get; init; }
    public double StopPoints => Math.Abs(Entry - Stop);
    public double TargetPoints => Math.Abs(Target - Entry);
    public double Confidence { get; init; }
    public string Thesis { get; init; } = "";
    public bool IsValid => Kind != SetupKind.None && Side != SideKind.Flat && StopPoints > 0;

    public static Setup None(string reason)
        => new() { Kind = SetupKind.None, Thesis = reason };
}
