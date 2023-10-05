using System.Diagnostics;

public class ChatGPT
{
  public enum Model
  {
    CHATGPT_3_5,
    CHAT_GPT_4
  }
  public ChatGPT()
  {

  }


  string GetModelRealName(Model model)
  {
    switch (model)
    {
      case Model.CHATGPT_3_5:
      default:
        return "gpt-3.5-turbo";
      case Model.CHAT_GPT_4:
        return "gpt-4";
    }
  }

  public string GetPromptResponse(string prompt, Model chatGptModel, string filePath)
  {
    prompt = prompt.Trim();

    var process = new Process();
    //process.StartInfo.UseShellExecute = false;
    //process.StartInfo.RedirectStandardOutput = true;
    //process.StartInfo.CreateNoWindow = true;
    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
    process.StartInfo.FileName = "cmd.exe";
    process.StartInfo.Arguments = @$"/c python C:\Users\thoma\Desktop\Projects\Unity\AI\Assets\Scripts\Python\_chatgpt.py ""{filePath}"" ""{prompt}"" ""{GetModelRealName(chatGptModel)}""";
    UnityEngine.Debug.Log($"{process.StartInfo.Arguments}");

    UnityEngine.Debug.Log($"Sending ChatGPT [{chatGptModel}] prompt: {prompt}");
    process.Start();

    // Wait for file
    while (!System.IO.File.Exists(filePath))
    {
      System.Threading.Thread.Sleep(250);
      //UnityEngine.Debug.LogWarning("Waiting for file: " + filePath);
    }
    System.Threading.Thread.Sleep(500);
    var output = System.IO.File.ReadAllText(filePath);

    // Return output
    return output.Trim();
  }
}
