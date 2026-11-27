using System.Collections.Concurrent;

public enum EnrollmentResult
{
    Enrolled,
    Waitlisted,
    AlreadyEnrolled,
    AlreadyWaitlisted,
    Ineligible,
    LearnerNotFound,
    Dropped,
    NotEnrolled
}

public sealed class Learner
{
    public Learner(string id, bool prerequisiteSatisfied)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
        PrerequisiteSatisfied = prerequisiteSatisfied;
    }

    public string Id { get; }
    public bool PrerequisiteSatisfied { get; set; }
}

public sealed class CourseEnrollmentService
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Learner> _learners;
    private readonly HashSet<string> _enrolledLearnerIds = new(StringComparer.Ordinal);
    private readonly Queue<string> _waitlist = new();

    public CourseEnrollmentService(IEnumerable<Learner> learners, int capacity)
    {
        ArgumentNullException.ThrowIfNull(learners);

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");
        }

        Capacity = capacity;
        _learners = learners.ToDictionary(
            learner => learner.Id,
            learner => learner,
            StringComparer.Ordinal);
    }

    public int Capacity { get; }

    public IReadOnlyCollection<string> EnrolledLearnerIds
    {
        get
        {
            lock (_sync)
            {
                return _enrolledLearnerIds.ToArray();
            }
        }
    }

    public IReadOnlyCollection<string> WaitlistedLearnerIds
    {
        get
        {
            lock (_sync)
            {
                return _waitlist.ToArray();
            }
        }
    }

    public EnrollmentResult Enroll(string learnerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(learnerId);

        lock (_sync)
        {
            if (!_learners.TryGetValue(learnerId, out Learner? learner))
            {
                return EnrollmentResult.LearnerNotFound;
            }

            if (_enrolledLearnerIds.Contains(learnerId))
            {
                return EnrollmentResult.AlreadyEnrolled;
            }

            if (!learner.PrerequisiteSatisfied)
            {
                return EnrollmentResult.Ineligible;
            }

            if (_enrolledLearnerIds.Count < Capacity)
            {
                _enrolledLearnerIds.Add(learnerId);
                return EnrollmentResult.Enrolled;
            }

            if (_waitlist.Contains(learnerId))
            {
                return EnrollmentResult.AlreadyWaitlisted;
            }

            _waitlist.Enqueue(learnerId);
            return EnrollmentResult.Waitlisted;
        }
    }

    public EnrollmentResult Drop(string learnerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(learnerId);

        lock (_sync)
        {
            if (!_enrolledLearnerIds.Remove(learnerId))
            {
                return EnrollmentResult.NotEnrolled;
            }

            PromoteWaitlist();
            return EnrollmentResult.Dropped;
        }
    }

    private void PromoteWaitlist()
    {
        while (_enrolledLearnerIds.Count < Capacity && _waitlist.Count > 0)
        {
            string learnerId = _waitlist.Dequeue();

            if (!_learners.TryGetValue(learnerId, out Learner? learner) ||
                !learner.PrerequisiteSatisfied)
            {
                continue;
            }

            if (_enrolledLearnerIds.Add(learnerId))
            {
                break;
            }
        }
    }
}

var learners = new[]
{
    new Learner("L001", prerequisiteSatisfied: true),
    new Learner("L002", prerequisiteSatisfied: true),
    new Learner("L003", prerequisiteSatisfied: false),
    new Learner("L004", prerequisiteSatisfied: true)
};

var enrollment = new CourseEnrollmentService(learners, capacity: 2);

Console.WriteLine($"L001: {enrollment.Enroll("L001")}");
Console.WriteLine($"L002: {enrollment.Enroll("L002")}");
Console.WriteLine($"L003: {enrollment.Enroll("L003")}");
Console.WriteLine($"L004: {enrollment.Enroll("L004")}");
Console.WriteLine($"L004 again: {enrollment.Enroll("L004")}");

Console.WriteLine($"Enrolled: {string.Join(", ", enrollment.EnrolledLearnerIds)}");
Console.WriteLine($"Waitlist: {string.Join(", ", enrollment.WaitlistedLearnerIds)}");

Console.WriteLine($"Drop L001: {enrollment.Drop("L001")}");
Console.WriteLine($"Drop L001 again: {enrollment.Drop("L001")}");
Console.WriteLine($"Enrolled: {string.Join(", ", enrollment.EnrolledLearnerIds)}");
Console.WriteLine($"Waitlist: {string.Join(", ", enrollment.WaitlistedLearnerIds)}");
