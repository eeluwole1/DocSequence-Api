namespace DocSequence.Api.Entities;

public class GeneratedDocument
{
    public long GeneratedDocumentId { get; set; }  // internal row key, not the business number
    public int DocumentTypeId { get; set; }
    public long Number { get; set; }
    public string Identifier { get; set; } = "";   // e.g. CXY-10429, written once at insert
    public string DocumentName { get; set; } = "";
    public string EngineerName { get; set; } = "";
    public Guid RequestKey { get; set; }
    public DateTime CreatedAt { get; set; }

    public DocumentType DocumentType { get; set; } = null!;
}