You are an expert .NET Solution Architect and Technical Hiring Lead.

### Objective
I have provided an attached PDF containing a technical assessment specification for an associate developer. Originally, this test was designed for low-code implementation in OutSystems. 

Your task is to adapt and rewrite this complete specification into an assessment tailored for an **Associate .NET Developer** learning **Modular Monolith Architecture** in **ASP.NET Core**.

The output must be formatted as a complete, production-ready specification document (suitable for exporting to `.docx` / Word).

---

### Key Requirements & Constraints

1. **Retain 100% of the Business Logic:**
   - Preserve all original domain rules, entities, booking flows, validations, and edge cases from the PDF.
   - Keep the scope appropriate for an **Associate Developer** (approachable, clear, and focused—avoid overcomplicating with unnecessary distributed system overhead).

2. **Complete Low-Code / OutSystems Purge:**
   - Remove every reference to OutSystems concepts (e.g., Service Studio, Aggregate queries, Server Actions, Client Actions, Screen flows, OutSystems entities, BPT).
   - Replace them with standard C# and ASP.NET Core idioms.

3. **Technical Stack & Architecture:**
   - **Framework:** ASP.NET Core ( .NET 10).
   - **Architecture:** Modular Monolith.
     - Define clean module boundaries (e.g., `Modules.Bookings`, `Modules.Rooms`, or relevant business modules matching the PDF).
     - Each module should follow a clean separation of concerns (e.g., Domain, Application, Infrastructure/Persistence).
     - Clear guidelines on inter-module communication (e.g., in-process MediatR/events, public module APIs/contracts, or direct service interfaces—no tight direct coupling between internal module DbContexts/tables).
   - **Data Access:** Entity Framework Core (code-first migrations, separate DbContexts/schemas per module where appropriate).
   - **API / UI:** Minimal APIs or ASP.NET Core Controllers (specify the expected exposure layer).

4. **Document Structure to Generate:**
   Please generate the full assessment document with the following sections:
   - **Title & Overview:** Role level (Associate .NET Developer), purpose, and estimated completion time.
   - **Architecture & Technical Guidelines:** Module breakdown, dependency rules, project layout, and coding standards.
   - **Functional Specifications & Business Logic:** Detailed user stories, acceptance criteria, and domain rules translated from the PDF.
   - **Technical Deliverables:** What the candidate must submit (solution structure, tests, database migrations, README, sample seed data).
   - **Evaluation Criteria / Rubric:** Clear grading rubric (Module isolation, Clean Code & SOLID, EF Core design, error handling, validation, basic unit/integration tests).
   - **Bonus / Nice-to-Have (Optional):** Light enhancements suitable for candidates wanting to stand out (e.g., FluentValidation, Outbox pattern basics,  or integration tests with Testcontainers).

---

### Output Format
- Provide the final document in clean, professional Markdown with clear headings, bullet points, and code/folder tree blocks so it can be directly converted into a polished `.docx` file.