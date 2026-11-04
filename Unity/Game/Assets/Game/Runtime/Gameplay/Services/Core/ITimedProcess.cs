namespace Game
{
    /// Contract for a gameplay service that owns one or more timed activities GameScheduler must
    /// drive: how soon it next needs processing, and processing everything due at a given instant.
    /// Implementing this does not by itself register a service with the scheduler — GameScheduler
    /// still takes each dependency explicitly and lists them in its own fixed processing order; this
    /// only lets that list be driven through one interface instead of one repeated method pair per
    /// service.
    public interface ITimedProcess
    {
        /// Game-hour of the next due timer this process owns, or null if none is pending.
        float? NextDueHours();

        /// Processes every timer due at or before nowHours.
        void ProcessDue(float nowHours);
    }
}
