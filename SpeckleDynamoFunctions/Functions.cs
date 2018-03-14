using Autodesk.DesignScript.Runtime;

namespace SpeckleDynamo.Functions
{

    [IsVisibleInDynamoLibrary(false)]
    public static class Functions
    {
        public static double MultiplyTwoNumbers(double a, double b)
        {
            return a * b;
        }
    }

}
