using Dynamo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleDynamoExtension
{
  /// <summary>
  /// From: https://github.com/DynamoDS/DeveloperWorkshop/blob/Unfancify_dev/CBW227909%20-%20View%20Extensions%20for%20C%23%20Beginners/Source/Utilities.cs
  /// </summary>
  public static class Utilities
  {
    /// Dynamo's public API does not offer a method to clear the current selection.
    /// However, Dynamo does contain an internal DynamoSelection class that includes such a method:
    /// https://github.com/DynamoDS/Dynamo/blob/RC2.0.1_master/src/DynamoCore/Core/DynamoSelection.cs#L94
    /// This method is exposed here via reflection.
    /// More on reflection here: https://stackify.com/what-is-c-reflection/
    /// </summary>
    public static void ClearSelection()
    {
      // Find the DynamoSelection class
      var dynamoSelection = typeof(DynamoModel).Assembly.GetType("Dynamo.Selection.DynamoSelection");
      // Instantiate the class
      var selectionInstance = dynamoSelection.GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
      // Find the ClearSelection method
      var clearMethod = dynamoSelection.GetMethod("ClearSelection", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
      // Call the ClearSelection method
      clearMethod.Invoke(selectionInstance.GetValue(null), null);
    }
  }
}
