Task 1:
Question 1:** Describe your implementation approach and the key decisions you made.
:>
I implemented a RESTful MessagesController using the provided IMessageRepository and models. My approach was to keep the controller focused on HTTP responsibilities while ensuring each endpoint followed clean REST conventions. I added all required CRUD endpoints and mapped them to appropriate status codes:
200 OK for successful GET requests
201 Created for a successful POST, including a Location header
204 No Content for successful updates and deletes
400 Bad Request for invalid input
404 Not Found when a message does not exist
409 Conflict when a title already exists
I added light validation for title and content, ensured titles remain unique per organization, and set timestamps (CreatedAt / UpdatedAt) consistently. The controller directly uses the repository to keep the Task-1 implementation simple, readable, and aligned with the instructions.

Question 2:** What would you improve or change if you had more time?
:> 
If I had more time, I would improve the design and move closer to production-level architecture. Specifically:
Introduce a business logic layer (IMessageLogic) so the controller stays thin and reusable.
Add FluentValidation for cleaner, reusable validation rules.
Enforce unique titles at the database level to avoid concurrency issues.
Add optimistic concurrency support (RowVersion) to prevent overwriting changes.
Add unit tests and integration tests to fully validate create/update/delete flows.
Improve error response consistency, logging, and observability.
Add authentication & authorization to ensure proper organization-level access control.
