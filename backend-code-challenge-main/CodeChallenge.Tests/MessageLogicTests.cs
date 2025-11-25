using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Repositories;
using CodeChallenge.Api.Models;

namespace CodeChallenge.Tests
{
    public class MessageLogicTests
    {
        private readonly Mock<IMessageRepository> _repoMock;

        public MessageLogicTests()
        {
            _repoMock = new Mock<IMessageRepository>();
        }

        private MessageLogic CreateSut() => new MessageLogic(_repoMock.Object);

        [Fact]
        public async Task CreateMessage_Success_ReturnsCreated()
        {
            var orgId = Guid.NewGuid();
            var req = new CreateMessageRequest
            {
                Title = "Valid Title",
                Content = new string('x', 20)
            };

            _repoMock.Setup(r => r.GetByTitleAsync(orgId, It.IsAny<string>()))
                     .ReturnsAsync((Message?)null);

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Message>()))
                     .ReturnsAsync((Message m) => { m.Id = Guid.NewGuid(); return m; });

            var sut = CreateSut();

            var result = await sut.CreateMessageAsync(orgId, req);

            result.Should().BeOfType<Created<Message>>();
            var created = (Created<Message>)result;
            created.Value.Title.Should().Be(req.Title);
            created.Value.Content.Should().Be(req.Content);
            created.Value.OrganizationId.Should().Be(orgId);
        }

        [Fact]
        public async Task CreateMessage_DuplicateTitle_ReturnsConflict()
        {
            var orgId = Guid.NewGuid();
            var req = new CreateMessageRequest
            {
                Title = "Duplicate",
                Content = new string('x', 20)
            };

            var existing = new Message { Id = Guid.NewGuid(), OrganizationId = orgId, Title = req.Title };

            _repoMock.Setup(r => r.GetByTitleAsync(orgId, req.Title.Trim()))
                     .ReturnsAsync(existing);

            var sut = CreateSut();

            var result = await sut.CreateMessageAsync(orgId, req);

            result.Should().BeOfType<Conflict>();
        }

        [Fact]
        public async Task CreateMessage_InvalidContentLength_ReturnsValidationError()
        {
            var orgId = Guid.NewGuid();
            var req = new CreateMessageRequest
            {
                Title = "OK Title",
                Content = "short" // < 10 chars
            };

            var sut = CreateSut();

            var result = await sut.CreateMessageAsync(orgId, req);

            result.Should().BeOfType<ValidationError>();
            var ve = (ValidationError)result;
            ve.Errors.Should().ContainKey("Content");
        }

        [Fact]
        public async Task UpdateMessage_NonExistent_ReturnsNotFound()
        {
            var orgId = Guid.NewGuid();
            var id = Guid.NewGuid();
            var req = new UpdateMessageRequest
            {
                Title = "New Title",
                Content = new string('x', 20),
                IsActive = true
            };

            _repoMock.Setup(r => r.GetByIdAsync(orgId, id)).ReturnsAsync((Message?)null);

            var sut = CreateSut();

            var result = await sut.UpdateMessageAsync(orgId, id, req);

            result.Should().BeOfType<NotFound>();
        }

        [Fact]
        public async Task UpdateMessage_Inactive_ReturnsValidationError()
        {
            var orgId = Guid.NewGuid();
            var id = Guid.NewGuid();
            var existing = new Message
            {
                Id = id,
                OrganizationId = orgId,
                Title = "Existing",
                Content = new string('x', 20),
                IsActive = false
            };

            _repoMock.Setup(r => r.GetByIdAsync(orgId, id)).ReturnsAsync(existing);

            var req = new UpdateMessageRequest
            {
                Title = "New Title",
                Content = new string('y', 20),
                IsActive = true
            };

            var sut = CreateSut();

            var result = await sut.UpdateMessageAsync(orgId, id, req);

            result.Should().BeOfType<ValidationError>();
            var ve = (ValidationError)result;
            ve.Errors.Should().ContainKey("IsActive");
        }

        [Fact]
        public async Task DeleteMessage_NonExistent_ReturnsNotFound()
        {
            var orgId = Guid.NewGuid();
            var id = Guid.NewGuid();

            _repoMock.Setup(r => r.GetByIdAsync(orgId, id)).ReturnsAsync((Message?)null);

            var sut = CreateSut();

            var result = await sut.DeleteMessageAsync(orgId, id);

            result.Should().BeOfType<NotFound>();
        }
    }
}
