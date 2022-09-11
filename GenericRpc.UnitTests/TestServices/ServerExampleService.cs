using System.Diagnostics;

namespace GenericRpc.UnitTests.TestServices
{
    public class ServerExampleService : IExampleService
    {
        public void Apply()
        {
            throw new System.NotImplementedException();
        }

        public string Concat(string text1, string text2)
        {
            throw new System.NotImplementedException();
        }

        public int GetIndex()
        {
            throw new System.NotImplementedException();
        }

        public void ShowMessage(string message, string message2) => Debug.WriteLine(message + message2);

        public int Sum(int number1, int number2) => number1 + number2;
    }
}