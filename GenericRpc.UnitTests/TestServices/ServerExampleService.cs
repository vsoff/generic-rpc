using System.Diagnostics;

namespace GenericRpc.UnitTests.TestServices
{
    public class ServerExampleService : IExampleService
    {
        public void ShowMessage(string message) => Debug.WriteLine(message);

        public int Sum(int number1, int number2) => number1 + number2;
    }
}