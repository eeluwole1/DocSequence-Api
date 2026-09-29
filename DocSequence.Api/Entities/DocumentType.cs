namespace DocSequence.Api.Entities;

public class DocumentType
{
    public int DocumentTypeId { get; set; }
    public string Prefix { get; set; } = "";
    public string Name { get; set; } = "";
    public long CurrentNumber { get; set; }   // last committed number for this type
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}