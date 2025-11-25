## Task 1:

## Question 1: Describe your implementation approach and the key decisions you made.
:>
I implemented a RESTful MessagesController using the provided IMessageRepository and models. My approach was to keep the controller focused on HTTP responsibilities while ensuring each endpoint followed clean REST conventions. I added all required CRUD endpoints and mapped them to appropriate status codes:
200 OK for successful GET requests
201 Created for a successful POST, including a Location header
204 No Content for successful updates and deletes
400 Bad Request for invalid input
404 Not Found when a message does not exist
409 Conflict when a title already exists
I added light validation for title and content, ensured titles remain unique per organization, and set timestamps (CreatedAt / UpdatedAt) consistently. The controller directly uses the repository to keep the Task-1 implementation simple, readable, and aligned with the instructions.

## Question 2: What would you improve or change if you had more time?
:> 
If I had more time, I would improve the design and move closer to production-level architecture. Specifically:
Introduce a business logic layer (IMessageLogic) so the controller stays thin and reusable.
Add FluentValidation for cleaner, reusable validation rules.
Enforce unique titles at the database level to avoid concurrency issues.
Add optimistic concurrency support (RowVersion) to prevent overwriting changes.
Add unit tests and integration tests to fully validate create/update/delete flows.
Improve error response consistency, logging, and observability.
Add authentication & authorization to ensure proper organization-level access control.




Task 2:

## Question 3: How did you approach the validation requirements and why?
:>
I centralized all validation inside the MessageLogic class to ensure that rules are consistently applied regardless of where the API is called from. Instead of keeping validation inside the controller, I moved it into logic-level helper methods (ValidateCreateRequest and ValidateUpdateRequest).
These methods enforce all required business rules:

Title must be present and between 3–200 characters
Content must be present and between 10–1000 characters
Titles must be unique per organization
Messages can only be updated/deleted when IsActive = true
I structured the validation output using the ValidationError result type defined in Results.cs, which returns errors as a dictionary of field → list of messages. This keeps validation failures consistent, testable, and easy for the controller to map to HTTP responses.
By placing validation inside MessageLogic, I ensured:
Single source of truth for business rules
Reusable rules for create/update paths
Cleaner controllers that focus only on HTTP concerns
Easy unit testing, since logic is isolated from the framework


## Question 4: What changes would you make to this implementation for a production environment?
:> 
If this were a production system, I would enhance the architecture and reliability of the MessageLogic layer with the following improvements:
1. Use FluentValidation for cleaner, declarative validation
Instead of manually building dictionaries, FluentValidation would give expressive, reusable validators with consistent error formatting.
2. Enforce uniqueness at the database level
I would add a unique index on (OrganizationId, Title) to guarantee title uniqueness even under high concurrency. Application-layer checks alone are not enough to avoid race conditions.
3. Add optimistic concurrency (RowVersion)
To prevent overwriting changes made by another user, I would add a RowVersion/concurrency token to the Message entity and enforce it on updates.
4. Use a persistent database instead of in-memory storage
Replace InMemoryMessageRepository with EF Core or another persistent store. This allows migrations, indexing, transactions, and reliability.
5. Introduce a testable time provider (IClock)
Replace DateTime.UtcNow with IClock.UtcNow so timestamps become predictable and easy to verify in unit tests.
6. Add structured logging and monitoring
Integrate Serilog/Elastic/OpenTelemetry to track validation errors, conflicts, and performance metrics.
7. Apply authentication and multi-tenant authorization
Ensure only authorized users can access or modify messages in a specific organization.
8. Improve response shaping
Return standardized API error formats (e.g., { code, message, details }) to provide a consistent client experience.





## Task 3

## Question 5: Explain your testing strategy and the tools you chose

For this task, I followed a layered unit testing strategy focused on isolating business logic from infrastructure.
The goal was to validate all rules inside MessageLogic without involving controllers, HTTP pipeline, or database code.

Testing Strategy
Unit Tests for Business Logic:
I isolated the MessageLogic class and verified each rule independently, including validation, uniqueness checks, and active/inactive state rules.

Mocking the Repository Layer:
By mocking IMessageRepository, I simulated different data scenarios (existing title, missing message, inactive record) without needing a real database.

Behavior Verification:
Ensured that each method returns the correct Result type (Created, Conflict, ValidationError, NotFound) for expected inputs.

Tools Used

xUnit → Chosen as the primary test framework for .NET because it's simple, modern, and widely used.

Moq → Used to mock IMessageRepository and fully isolate business logic from external dependencies.

FluentAssertions → Provides clear, human-readable assertions (e.g., result.Should().BeOfType<Conflict>()), improving readability and debugging.



## Question 6: What other scenarios would you test in a real-world application?

In a real production environment, I would expand the testing to cover more complex and edge-case behaviors:

Additional Real-World Scenarios

Boundary Value Tests:
Validate title/content at exact limits (3, 200, 10, 1000 characters).

Concurrency & Race Conditions:
Test simultaneous message creation with identical titles to ensure DB-level unique constraints hold under load.

Optimistic Concurrency Tests:
Use UpdatedAt/RowVersion fields to ensure updates do not overwrite changes made by another user.

Integration Tests with In-Memory or Test Database:
Validate full repository behavior, database constraints, migrations, and error handling.

Authorization / Multi-Tenant Tests:
Ensure cross-organization isolation so one org cannot access another org’s messages.

Negative Tests for Infrastructure Failures:
Simulate repository exceptions (DB down, timeout) to ensure proper error handling and logging.

Controller-level Tests:
Verify correct mapping from Result types to HTTP status codes (200, 201, 204, 400, 404, 409).







