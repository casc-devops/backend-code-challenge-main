using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;

namespace CodeChallenge.Api.Logic;

public class MessageLogic : IMessageLogic
{
    private readonly IMessageRepository _repository;

    public MessageLogic(IMessageRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> CreateMessageAsync(Guid organizationId, CreateMessageRequest request)
    {
        var errors = ValidateCreateRequest(request);
        if (errors.Any())
            return new ValidationError(errors.ToDictionary(k => k.Key, v => v.Value.ToArray()));

        // Title unique per organization
        var existing = await _repository.GetByTitleAsync(organizationId, request.Title.Trim());
        if (existing is not null)
            return new Conflict($"A message with title '{request.Title}' already exists.");

        var message = new Message
        {
            OrganizationId = organizationId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(message);
        return new Created<Message>(created);
    }

    public async Task<Result> UpdateMessageAsync(Guid organizationId, Guid id, UpdateMessageRequest request)
    {
        var message = await _repository.GetByIdAsync(organizationId, id);
        if (message is null)
            return new NotFound("Message not found.");

        // Can only update active messages
        if (!message.IsActive)
            return new ValidationError(new Dictionary<string, string[]>
            {
                { "IsActive", new[] { "Cannot update an inactive message." } }
            });

        var errors = ValidateUpdateRequest(request);
        if (errors.Any())
            return new ValidationError(errors.ToDictionary(k => k.Key, v => v.Value.ToArray()));

        // Title uniqueness check (if title changed)
        if (!string.Equals(message.Title, request.Title.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var byTitle = await _repository.GetByTitleAsync(organizationId, request.Title.Trim());
            if (byTitle is not null && byTitle.Id != id)
                return new Conflict($"A message with title '{request.Title}' already exists.");
        }

        message.Title = request.Title.Trim();
        message.Content = request.Content.Trim();
        message.IsActive = request.IsActive;
        message.UpdatedAt = DateTime.UtcNow; // UpdatedAt set automatically

        var updated = await _repository.UpdateAsync(message);
        if (updated is null)
            return new NotFound("Message not found during update.");

        return new Updated();
    }

    public async Task<Result> DeleteMessageAsync(Guid organizationId, Guid id)
    {
        var message = await _repository.GetByIdAsync(organizationId, id);
        if (message is null)
            return new NotFound("Message not found.");

        if (!message.IsActive)
            return new ValidationError(new Dictionary<string, string[]>
            {
                { "IsActive", new[] { "Cannot delete an inactive message." } }
            });

        var deleted = await _repository.DeleteAsync(organizationId, id);
        if (!deleted)
            return new NotFound("Message not found or could not be deleted.");

        return new Deleted();
    }

    public Task<Message?> GetMessageAsync(Guid organizationId, Guid id) =>
        _repository.GetByIdAsync(organizationId, id);

    public Task<IEnumerable<Message>> GetAllMessagesAsync(Guid organizationId) =>
        _repository.GetAllByOrganizationAsync(organizationId);

    // --- Validation helpers ---
    private static Dictionary<string, List<string>> ValidateCreateRequest(CreateMessageRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Title))
            AddError(errors, "Title", "Title is required.");
        else if (request.Title.Trim().Length < 3 || request.Title.Trim().Length > 200)
            AddError(errors, "Title", "Title must be between 3 and 200 characters.");

        if (string.IsNullOrWhiteSpace(request.Content))
            AddError(errors, "Content", "Content is required.");
        else if (request.Content.Trim().Length < 10 || request.Content.Trim().Length > 1000)
            AddError(errors, "Content", "Content must be between 10 and 1000 characters.");

        return errors;
    }

    private static Dictionary<string, List<string>> ValidateUpdateRequest(UpdateMessageRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Title))
            AddError(errors, "Title", "Title is required.");
        else if (request.Title.Trim().Length < 3 || request.Title.Trim().Length > 200)
            AddError(errors, "Title", "Title must be between 3 and 200 characters.");

        if (string.IsNullOrWhiteSpace(request.Content))
            AddError(errors, "Content", "Content is required.");
        else if (request.Content.Trim().Length < 10 || request.Content.Trim().Length > 1000)
            AddError(errors, "Content", "Content must be between 10 and 1000 characters.");

        return errors;
    }

    private static void AddError(Dictionary<string, List<string>> d, string key, string message)
    {
        if (!d.TryGetValue(key, out var list))
        {
            list = new List<string>();
            d[key] = list;
        }
        list.Add(message);
    }
}
