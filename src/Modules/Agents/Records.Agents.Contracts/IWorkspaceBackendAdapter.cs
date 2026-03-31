using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Contracts;

public interface IWorkspaceBackendAdapter
{
    string Id { get; }

    Task<WorkspaceGridDto?> SearchAsync(
        WorkspaceSearchRequestDto request,
        CancellationToken cancellationToken
    );

    Task<WorkspaceEntityDto?> GetEntityAsync(
        WorkspaceEntityRequestDto request,
        CancellationToken cancellationToken
    );

    Task<WorkspaceHistoryDto?> GetHistoryAsync(
        WorkspaceEntityRequestDto request,
        CancellationToken cancellationToken
    );

    Task<WorkspaceEditorSchemaDto?> ResolveSchemaAsync(
        WorkspaceResolveSchemaRequestDto request,
        CancellationToken cancellationToken
    );

    Task<WorkspaceProposalDto?> ProposeUpdateAsync(
        WorkspaceProposeUpdateRequestDto request,
        CancellationToken cancellationToken
    );

    Task<WorkspaceProposalDto?> ProposeCreateAsync(
        WorkspaceProposeCreateRequestDto request,
        CancellationToken cancellationToken
    );

    Task<WorkspaceEntityDto?> ApplyConfirmedProposalAsync(
        WorkspaceApplyProposalRequestDto request,
        CancellationToken cancellationToken
    );

    Task<WorkspaceNotificationsDto?> GetContextNotificationsAsync(
        WorkspaceEntityRequestDto request,
        CancellationToken cancellationToken
    );
}
