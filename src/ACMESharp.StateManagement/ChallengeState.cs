using System.Threading.Tasks;
using Stateless;

namespace ACMESharp.StateManagement
{
    /// <summary>
    /// Models the state machine workflow of an ACME Challenge object.
    /// </summary>
    /// <remarks>
    /// Based on state machine description for ACME Challenge object:
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.6
    /// </remarks>
    public class ChallengeState
    {
        private StateMachine<Status, Trigger> _machine;

        public void Init()
        {
            _machine = new StateMachine<Status, Trigger>(
                    Status.Pending, FiringMode.Queued);

            _machine.Configure(Status.Pending)
                .Permit(Trigger.Answer, Status.Processing);

            _machine.Configure(Status.Processing)
                .PermitReentry(Trigger.RetryByServer)
                .PermitReentry(Trigger.RetryByClient)
                .Permit(Trigger.SucceedValidation, Status.Valid)
                .Permit(Trigger.FailValidation, Status.Invalid);
        }

        public Task AnswerAsync() =>
                _machine.FireAsync(Trigger.Answer);

        public Task RetryByServerAsync() =>
                _machine.FireAsync(Trigger.RetryByServer);

        public Task RetryByClientAsync() =>
                _machine.FireAsync(Trigger.RetryByClient);

        public Task SucceedValidationAsync() =>
                _machine.FireAsync(Trigger.SucceedValidation);

        public Task FailValidationAsync() =>
                _machine.FireAsync(Trigger.FailValidation);

        public enum Status
        {
            Unknown = 0,

            Pending = 1,
            Processing = 2,
            Valid = 3,
            Invalid = 4,
        }

        public enum Trigger
        {
            Answer = 1,
            RetryByServer = 2,
            RetryByClient = 3,
            SucceedValidation = 4,
            FailValidation = int.MaxValue,
        }        
    }
}