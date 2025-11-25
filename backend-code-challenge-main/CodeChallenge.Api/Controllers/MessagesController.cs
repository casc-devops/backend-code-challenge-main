using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CodeChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageRepository _repository;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageRepository repository, ILogger<MessagesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetAll(Guid organizationId)
    {
        var messages = await _repository.GetAllByOrganizationAsync(organizationId);
        return Ok(messages);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetById(Guid organizationId, Guid id)
    {
        var message = await _repository.GetByIdAsync(organizationId, id);
        if (message is null) return NotFound();
        return Ok(message);
    }

    [HttpPost]
    public async Task<ActionResult> Create(Guid organizationId, [FromBody] CreateMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length < 3)
            return BadRequest(new { Title = "Title is required and must be at least 3 characters." });

        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Trim().Length < 10)
            return BadRequest(new { Content = "Content is required and must be at least 10 characters." });

        var existing = await _repository.GetByTitleAsync(organizationId, request.Title.Trim());
        if (existing is not null)
            return Conflict(new { message = $"A message with title '{request.Title}' already exists." });

        var message = new Message
        {
            OrganizationId = organizationId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(message);

        return CreatedAtAction(nameof(GetById),
            new { organizationId = organizationId, id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid organizationId, Guid id, [FromBody] UpdateMessageRequest request)
    {
        var message = await _repository.GetByIdAsync(organizationId, id);
        if (message is null) return NotFound();

        if (!message.IsActive)
            return BadRequest(new { IsActive = "Cannot update an inactive message." });

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length < 3)
            return BadRequest(new { Title = "Title is required and must be at least 3 characters." });

        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Trim().Length < 10)
            return BadRequest(new { Content = "Content is required and must be at least 10 characters." });

         if (!string.Equals(message.Title, request.Title.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var byTitle = await _repository.GetByTitleAsync(organizationId, request.Title.Trim());
            if (byTitle is not null && byTitle.Id != id)
                return Conflict(new { message = $"A message with title '{request.Title}' already exists." });
        }

        message.Title = request.Title.Trim();
        message.Content = request.Content.Trim();
        message.IsActive = request.IsActive;
        message.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(message);
        if (updated is null) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid organizationId, Guid id)
    {
        var message = await _repository.GetByIdAsync(organizationId, id);
        if (message is null) return NotFound();

        if (!message.IsActive)
            return BadRequest(new { IsActive = "Cannot delete an inactive message." });

        var deleted = await _repository.DeleteAsync(organizationId, id);
        if (!deleted) return NotFound();

        return NoContent();
    }
}
