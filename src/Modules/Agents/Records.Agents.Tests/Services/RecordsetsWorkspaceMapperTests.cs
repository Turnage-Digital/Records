using Records.Agents.Infrastructure.OpenAI;

namespace Records.Agents.Tests.Services;

public sealed class RecordsetsWorkspaceMapperTests
{
    [Test]
    public void SummarizeToolResult_ShouldUnwrapTextContentEnvelope_WhenListRecordsetsReturnsContentBlocks()
    {
        var outputJson =
            """
            [
              {
                "type": "text",
                "text": "[{\"id\":\"01KNJ1H9XMXJAAAWCZKCB3Y0AF\",\"name\":\"Orders\"}]"
              }
            ]
            """;

        var summary = RecordsetsWorkspaceMapper.SummarizeToolResult("list_recordsets", outputJson);

        Assert.That(summary, Is.EqualTo("Found 1 recordset(s)."));
    }
}
