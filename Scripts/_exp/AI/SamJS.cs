using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Diagnostics;
using System.IO;

public class SamJS
{

  public SamJS()
  {
  }

  public struct VoiceOptions
  {
    public int _Speed;
    public int _Pitch;
    public int _Throat;
    public int _Mouth;
    public bool _Sing;

    public static VoiceOptions CreateBasic()
    {
      return new VoiceOptions()
      {
        _Speed = 72,
        _Pitch = 62,
        _Throat = 128,
        _Mouth = 128,

        _Sing = false
      };
    }
  }

  public struct VoiceQueueData
  {
    public string _TextToSpeech;
    public string _SaveLocation;
    public VoiceOptions _VoiceOptions;
  }

  int _generationId;
  public void ProcessQueueData(params VoiceQueueData[] voiceQueueData)
  {

    var waitForFiles = new List<string>();

    // Loop through queue data
    foreach (var data in voiceQueueData)
    {
      var saveDestination = data._SaveLocation;
      var textToSpeech = data._TextToSpeech;
      var voiceOptions = data._VoiceOptions;

      waitForFiles.Add(saveDestination);

      // Sanitize
      if (!saveDestination.EndsWith(".wav"))
      {
        UnityEngine.Debug.LogError($"You must save voice recordings as (*.wav)!");
        return;
      }
      textToSpeech = textToSpeech.Replace("\r", "");
      textToSpeech = textToSpeech.Replace("\n", "");
      textToSpeech = System.Text.RegularExpressions.Regex.Replace(textToSpeech, @"[^a-zA-Z0-9 -""'<>.,!?:;^]", string.Empty);
      textToSpeech = textToSpeech.Trim();

      // Save voice queue data to file for Node SamJs process to read
      var sing = voiceOptions._Sing ? 1 : 0;
      var metadata = $"{saveDestination}|{textToSpeech}|{voiceOptions._Speed}|{voiceOptions._Pitch}|{voiceOptions._Throat}|{voiceOptions._Mouth}|{sing}";
      WriteToFile(metadata, $"C:/Users/thoma/Desktop/Projects/Unity/AI/Resources/sam-js/queue/{_generationId++}.txt");
      UnityEngine.Debug.Log($"Queued sam-js voice request [{saveDestination}]: {textToSpeech}");
    }

    // Generate audio
    // Create Node process
    var process = new Process();
    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
    process.StartInfo.FileName = "cscript";
    process.StartInfo.Arguments = @$"/B /NOLOGO C:\Users\thoma\Desktop\Projects\Unity\AI\Resources\sam-js\powershell_quiet.vbs";

    UnityEngine.Debug.Log($"Stating sam-js node process: {process.StartInfo.Arguments}");
    process.Start();

    // Wait for all files to process
    var messageIter = 0;
    while (waitForFiles.Count > 0)
    {
      var waitForFile = waitForFiles[0];
      if (!File.Exists(waitForFile))
      {
        if (messageIter++ % 15 == 0)
        {
          var pname = Process.GetProcessesByName("node");
          //UnityEngine.Debug.LogWarning($"Waiting for samjs audio [{pname.Length}, {waitForFile}]");
        }
        System.Threading.Thread.Sleep(150);
      }
      else
      {
        waitForFiles.RemoveAt(0);
      }
    }
  }

  // Utility
  public static void WriteToFile(string text, string filePath)
  {
    // Create a StreamWriter object to write to the file
    using (StreamWriter writer = new StreamWriter(filePath, false))
    {
      // Write the text to the file
      writer.Write(text);
    }
  }
}
