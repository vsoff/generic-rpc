using GenericRpc.Serialization;
using GenericRpc.SocketTransport;
using GenericRpc.Transport;
using GenericRpc.UnitTests.TestServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace GenericRpc.UnitTests
{
    [TestClass]
    public class ReflectionTests
    {
        [TestMethod]
        public void Test()
        {
            var mediator = new Mediator();

            var generatedInstance = (IExampleService)ClassGenerator.GenerateSpeakerInstance(typeof(IExampleService), mediator);

            try { generatedInstance.GetIndex(); }
            catch { }

            try { generatedInstance.ShowMessage("sda", "gg wp"); }
            catch { }

            try { var cc32 = generatedInstance.Sum(1, 2); }
            catch { }

            try { generatedInstance.Concat("sda", "gg wp"); }
            catch { }

            try { generatedInstance.Apply(); }
            catch { }

        }

        private static object InvokeMethod<T>(T obj, string methodName, List<object> args)
        {
            return typeof(T).GetMethod(methodName).Invoke(obj, args.ToArray());
        }

        private class ClientService : SpeakerService, IExampleService
        {
            public ClientService(IMediator mediator) : base(mediator)
            {
            }

            public void ShowMessage(string message, string message2)
            {
                Execute(nameof(IExampleService), nameof(ShowMessage), message, message2);
            }

            public int Sum(int number1, int number2)
            {
                return (int)Execute(nameof(IExampleService), nameof(ShowMessage), number1, number2);
            }

            public string Concat(string text1, string text2)
            {
                return (string)Execute(nameof(IExampleService), nameof(Concat), text1, text2);
            }

            public int GetIndex()
            {
                return (int)Execute(nameof(IExampleService), nameof(GetIndex));
            }

            public void Apply()
            {
                Execute(nameof(IExampleService), nameof(GetIndex));
            }
        }

        public interface IA
        {

        }
    }
}