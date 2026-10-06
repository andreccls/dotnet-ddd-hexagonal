namespace DddHexagonal.Application.Common;

/// <summary>Response of use cases that return nothing.</summary>
public readonly record struct Unit
{
    public static Unit Value => default;
}
