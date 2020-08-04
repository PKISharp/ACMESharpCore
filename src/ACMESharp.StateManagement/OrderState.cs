using System.Threading.Tasks;
using Stateless;

namespace ACMESharp.StateManagement
{
    /// <summary>
    /// Models the state machine workflow of an ACME Order object.
    /// </summary>
    /// <remarks>
    /// Based on state machine description for ACME Order object:
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.6
    /// </remarks>
    public class OrderState
    {
        private StateMachine<Status, TriggerX> _machine;

        public void Init()
        {
            _machine = new StateMachine<Status, TriggerX>(
                    Status.Pending, FiringMode.Queued);

            _machine.Configure(Status.Pending)
                .Permit(TriggerX.ValidateAllAuthorizations, Status.Ready)
                .Permit(TriggerX.FailAuthorization, Status.Invalid)
                .Permit(TriggerX.ErrorOut, Status.Invalid);

            _machine.Configure(Status.Ready)
                .Permit(TriggerX.Finalize, Status.Processing)
                .Permit(TriggerX.ErrorOut, Status.Invalid);

            _machine.Configure(Status.Processing)
                .Permit(TriggerX.IssueCertificate, Status.Valid)
                .Permit(TriggerX.ErrorOut, Status.Invalid);
        }

        public Task ValidateAllAuthorizationsAsync() =>
                _machine.FireAsync(TriggerX.ValidateAllAuthorizations);

        public Task FinalizeAsync() =>
                _machine.FireAsync(TriggerX.Finalize);
        
        public Task IssueCertificateAsync() =>
                _machine.FireAsync(TriggerX.Finalize);
        
        public Task ErrorOutAsync() =>
                _machine.FireAsync(TriggerX.ErrorOut);

        public Task FailAuthorizationAsync() =>
                _machine.FireAsync(TriggerX.FailAuthorization);

        public enum Status
        {
            Unknown = 0,

            Pending = 1,
            Ready =2,
            Processing = 3,
            Valid = 4,
            Invalid = 5,
        }

        public enum TriggerX
        {
            ValidateAllAuthorizations = 1,
            Finalize = 2,
            IssueCertificate = 2,

            ErrorOut = int.MaxValue,
            FailAuthorization = int.MaxValue,
        }        
    }
}