using DocSequence.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace DocSequence.Tests;

// Proves the database enforces the rules even when the API is bypassed.
// Each statement runs in a transaction that is rolled back, so no data is left behind.
[Collection(ApiCollection.Name)]
public sealed class DatabaseConstraintTests(DocSequenceApiFactory factory) : ApiTestBase(factory)
{
    [Theory] // AC-019
    [InlineData("abc")]   // lowercase: only rejected thanks to the BIN2 collation
    [InlineData("C1Y")]
    [InlineData("CX-")]
    [InlineData("C")]
    public async Task Prefix_check_rejects_invalid_prefixes(string prefix)
    {
        var ex = await Should.ThrowAsync<SqlException>(() => ExecuteRolledBackAsync(
            "INSERT INTO DocumentTypes (Prefix, Name, CurrentNumber, IsActive) VALUES (@p, 'Test', 0, 1)", prefix));

        ex.Number.ShouldBe(547);
        ex.Message.ShouldContain("CK_DocumentTypes_Prefix");
    }

    [Fact] // AC-019 control case
    public async Task Prefix_check_accepts_valid_prefix() =>
        await ExecuteRolledBackAsync(
            "INSERT INTO DocumentTypes (Prefix, Name, CurrentNumber, IsActive) VALUES (@p, 'Test', 0, 1)", "ZZT");

    [Fact] // AC-003
    public async Task Duplicate_type_and_number_is_rejected()
    {
        const string sql = """
            INSERT INTO GeneratedDocuments (DocumentTypeId, Number, Identifier, DocumentName, EngineerName, RequestKey)
            VALUES (1, 1, 'TEST-A', 'Test', 'Test', NEWID());
            INSERT INTO GeneratedDocuments (DocumentTypeId, Number, Identifier, DocumentName, EngineerName, RequestKey)
            VALUES (1, 1, 'TEST-B', 'Test', 'Test', NEWID());
            """;

        var ex = await Should.ThrowAsync<SqlException>(() => ExecuteRolledBackAsync(sql));

        ex.Message.ShouldContain("UX_GeneratedDocuments_DocumentTypeId_Number");
    }

    private async Task ExecuteRolledBackAsync(string sql, string? prefix = null)
    {
        await using var connection = new SqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        await using var command = new SqlCommand(sql, connection, transaction);
        if (prefix is not null)
            command.Parameters.AddWithValue("@p", prefix);

        await command.ExecuteNonQueryAsync();
        await transaction.RollbackAsync();
    }
}