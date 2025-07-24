namespace Scry.Models;

public class ListEntry
{
    public string Value { get; set; }
    public string? Description { get; set; }

    public ListEntry(string value, string? description)
    {
        Value = value;
        Description = description;
    }
}
