# C# Course Enrollment Prototype

## Prerequisites and policy

- A learner must be registered and have `PrerequisiteSatisfied = true`.
- When capacity remains, an eligible learner enrolls directly.
- When the course is full, an eligible learner is added once to the waitlist.
- Learner ids are unique and enrollment is single-instance.
- **Ineligible learner policy:** ineligible learners may join the waitlist? No. In this implementation, an
  enrollment request that is already ineligible fails immediately. If a learner becomes represented as
  ineligible while on the waitlist, it is skipped and removed when promotion is attempted.
- Promotion is FIFO: the first eligible learner is enrolled. Earlier ineligible entries are removed.
- A repeated drop is a no-op with `NotEnrolled`.
- The service is thread-safe: operations are locked as one atomic transaction.

## Run

```bash
cd ConsoleApp
dotnet run
```

## Test

A test project can be added with xUnit/MSTest/NUnit. No starter test framework was present.
