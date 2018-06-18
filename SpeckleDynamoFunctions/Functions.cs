using Autodesk.DesignScript.Runtime;
using System.Collections;
using System;
using System.Collections.Generic;

namespace SpeckleDynamo.Functions
{

  [IsVisibleInDynamoLibrary(false)]
  public static class Functions
  {
    public static object SpeckleOutput(string layer)
    {
        return SpeckleTempData.GetLayerObjects(layer);
    }

    public static string Test(string test)
    {
      return "Hello "+test;
    }

    public static string HelloWorld()
    {
      return "Hello World";
    }
  }

}
