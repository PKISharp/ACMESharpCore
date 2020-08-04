using System;
using System.Threading.Tasks;
using Stateless;

namespace ACMESharp.StateManagement
{
    /// <summary>
    /// Models the state machine workflow of an ACME Account object.
    /// </summary>
    /// <remarks>
    /// Based on state machine description for ACME Account object:
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.6
    /// </remarks>
    public class AccountState
    {
        private StateMachine<Status, Trigger> _machine;
        
        public Func<Task> OnValid { get; set; }
        public Func<Task> OnRevoked { get; set; }
        public Func<Task> OnDeactivated { get; set; }

        protected void Init(Status initialStatus = Status.Valid)
        {
            _machine = new StateMachine<Status, Trigger>(
                    initialStatus, FiringMode.Queued);
            _machine.Configure(Status.Valid)
                .OnEntryAsync(async () => await (OnValid?.Invoke() ?? Task.CompletedTask))
                .Permit(Trigger.Deactivate, Status.Deactivated)
                .Permit(Trigger.Revoke, Status.Revoked);

            _machine.Configure(Status.Revoked)
                .OnEntryAsync(async () => await (OnRevoked?.Invoke() ?? Task.CompletedTask));

            _machine.Configure(Status.Deactivated)
                .OnEntryAsync(async () => await (OnDeactivated?.Invoke() ?? Task.CompletedTask));
        }



        public Task Revoke() =>
                _machine.FireAsync(Trigger.Revoke);

        public Task Deactivate() =>
                _machine.FireAsync(Trigger.Deactivate);

        public enum Status
        {
            Unknown = 0,

            Valid = 1,
            Revoked = 2,
            Deactivated = 3,
        }

        public enum Trigger
        {
            Revoke = 1,
            Deactivate = 2,
        }
    }
}