using System;

namespace GenericRpc.Transport
{
    public abstract class SpeakerService
    {
        public static readonly string ExecuteMethodName = nameof(Execute);

        private readonly IMediator _mediator;

        public SpeakerService(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        protected object Execute(string serviceName, string methodName, params object[] arguments)
            =>  _mediator.Execute(serviceName, methodName, arguments);
    }
}
