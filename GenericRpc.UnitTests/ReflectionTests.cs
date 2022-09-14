using GenericRpc.Serialization;
using GenericRpc.SocketTransport;
using GenericRpc.Transport;
using GenericRpc.UnitTests.TestServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace GenericRpc.UnitTests
{
    [TestClass]
    public class ReflectionTests
    {
        [TestMethod]
        public void Test()
        {
            IMediator mediator = new TestMediator();
            var clientContext = new ClientContext(Guid.NewGuid());
            var proxyType = ProxyGenerator.GenerateProxyType(typeof(IExampleService));
            var generatedInstance = (IExampleService)ProxyGenerator.ActivateProxyInstance(proxyType, mediator, clientContext);

            Assert.ThrowsException<MediatorOkException>(() => generatedInstance.GetIndex());
            Assert.ThrowsException<MediatorOkException>(() => generatedInstance.ShowMessage("message text"));
            Assert.ThrowsException<MediatorOkException>(() => generatedInstance.Sum(1337, 4663));
            Assert.ThrowsException<MediatorOkException>(() => generatedInstance.Concat("test text 1", "test text 2"));
            Assert.ThrowsException<MediatorOkException>(() => generatedInstance.Apply());
        }

        private class TestMediator : IMediator
        {
            public object Execute(ClientContext clientContext, string serviceName, string methodName, params object[] arguments)
            {
                throw new MediatorOkException();
            }
        }

        private class MediatorOkException : Exception {}
    }
}