using GenericRpc.Mediators;
using GenericRpc.ServicesGeneration;
using GenericRpc.UnitTests.TestServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GenericRpc.UnitTests
{
    [TestClass]
    public class ProxyGenerationTests
    {
        [TestMethod]
        public void Test()
        {
            IMediator mediator = new MediatorWithException();
            var clientContext = new ClientContext(Guid.NewGuid());
            var proxyType = ProxyGenerator.GenerateProxyType(typeof(IExampleService));
            var generatedInstance = (IExampleService)ProxyGenerator.ActivateProxyInstance(proxyType, mediator, clientContext);

            Assert.ThrowsException<MediatorOkException>(() => generatedInstance.GetIndex());
            Assert.ThrowsException<MediatorOkException>(() => generatedInstance.ShowMessage("message text"));
            Assert.ThrowsException<MediatorOkException>(() => generatedInstance.Sum(1337, 4663));
            Assert.ThrowsException<MediatorOkException>(() => generatedInstance.Concat("test text 1", "test text 2"));
            Assert.ThrowsException<MediatorOkException>(() => generatedInstance.Apply());
        }

        private class MediatorWithException : IMediator
        {
            public object Execute(ClientContext clientContext, string serviceName, string methodName, params object[] arguments)
            {
                throw new MediatorOkException();
            }

            public void Dispose()
            {
            }
        }

        private class MediatorOkException : Exception {}
    }
}