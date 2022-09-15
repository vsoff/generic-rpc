using GenericRpc.Mediators;
using System;

namespace GenericRpc
{
    public abstract class ProxyService
    {
        public static readonly string ExecuteMethodName = nameof(Execute);

        private readonly IMediator _mediator;
        private readonly ClientContext _clientContext;

        public ProxyService(IMediator mediator, ClientContext clientContext)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _clientContext = clientContext;
        }

        protected object Execute(string serviceName, string methodName, params object[] arguments)
            => _mediator.Execute(_clientContext, serviceName, methodName, arguments);
    }
}
