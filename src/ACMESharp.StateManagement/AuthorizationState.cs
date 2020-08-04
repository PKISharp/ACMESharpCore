using System.Threading.Tasks;
using Stateless;

namespace ACMESharp.StateManagement
{
    /// <summary>
    /// Models the state machine workflow of an ACME AuthorizationS object.
    /// </summary>
    /// <remarks>
    /// Based on state machine description for ACME AuthorizationS object:
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.6
    /// </remarks>
    public class AuthorizationState
    {
        private StateMachine<Status, Trigger> _machine;

        public void Init()
        {
            _machine = new StateMachine<Status, Trigger>(
                    Status.Pending, FiringMode.Queued);

            _machine.Configure(Status.Pending)
                .Permit(Trigger.ValidateChallenge, Status.Valid)
                .Permit(Trigger.ErrorOut, Status.Invalid);

            _machine.Configure(Status.Valid)
                .Permit(Trigger.Revoke, Status.Revoked)
                .Permit(Trigger.Deactivate, Status.Deactivated)
                .Permit(Trigger.Expire, Status.Expired);
        }

        public Task ValidateChallengeAsync() =>
                _machine.FireAsync(Trigger.ValidateChallenge);
                
        public Task RevokeAsync() =>
                _machine.FireAsync(Trigger.Revoke);
                
        public Task DeactivateAsync() =>
                _machine.FireAsync(Trigger.Deactivate);
                
        public Task ExpireAsync() =>
                _machine.FireAsync(Trigger.Expire);
                
        public Task ErrorOutAsync() =>
                _machine.FireAsync(Trigger.ErrorOut);
                
        public enum Status
        {
            Unknown = 0,

            Pending = 1,
            Valid = 2,
            Invalid = 3,
            Revoked = 4,
            Deactivated = 5,
            Expired = 6,
        }

        public enum Trigger
        {
            ValidateChallenge = 1,
            Revoke = 2,
            Deactivate = 3,
            Expire = 4,
            ErrorOut = int.MaxValue,
        }        
    }
}