using UnityEngine;
using System;
using System.Reflection;

public class ClipboardHelper
{
  public static string clipBoard
  {
    get
    {
      return GUIUtility.systemCopyBuffer;
    }
    set
    {
      GUIUtility.systemCopyBuffer = value;
    }
  }
}
