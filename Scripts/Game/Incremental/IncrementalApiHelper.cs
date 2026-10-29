using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace SneakyGame.Game.Incremental
{
  public static class IncrementalApiHelper
  {
    static HttpClient s_httpClient;
    static bool s_isSending;

    public static event Action<InteractionCounts> OnDataReceived;

    class InteractionCountsWrapper
    {
      public int _New;
      private int _amountLast;

      public InteractionCountsWrapper()
      {
        _amountLast = -1;
      }

      public void UpdateCount(int total)
      {
        if (_amountLast != -1)
        {
          var delta = total - _amountLast;
          _New += delta;
        }
        _amountLast = total;
      }
    }
    static InteractionCountsWrapper s_likesWrapper, s_followsWrapper, s_sharesWrapper;

    public static int GetNewLikes() => s_likesWrapper._New;
    public static int GetNewFollows() => s_followsWrapper._New;
    public static int GetNewShares() => s_sharesWrapper._New;

    static Dictionary<string, int> s_giftsWrapper = new();
    public static int GetNewGifts(string giftType) => s_giftsWrapper.TryGetValue(giftType, out var count) ? count : 0;

    public static void Init()
    {
      s_httpClient = new HttpClient();

      s_likesWrapper = new();
      s_followsWrapper = new();
      s_sharesWrapper = new();

      OnDataReceived += interactionCounts =>
      {

        s_likesWrapper.UpdateCount(interactionCounts.likes);
        Debug.Log($"Total likes received: {interactionCounts.likes}, new likes: {s_likesWrapper._New}");

        s_followsWrapper.UpdateCount(interactionCounts.follows);
        Debug.Log($"Total follows received: {interactionCounts.follows}, new follows: {s_followsWrapper._New}");

        s_sharesWrapper.UpdateCount(interactionCounts.shares);
        Debug.Log($"Total shares received: {interactionCounts.shares}, new shares: {s_sharesWrapper._New}");

        s_giftsWrapper = interactionCounts.gifts;
        foreach (var kvp in s_giftsWrapper)
          Debug.Log($"Total gifts received: {kvp.Key} = {kvp.Value}");
      };
      RequestInteractionData();
    }

    // Fire-and-forget entry point safe to call from any MonoBehaviour on the main thread.
    public static async void RequestInteractionData()
    {
      if (s_isSending)
        return;

      s_isSending = true;
      try
      {
        var data = await PostInteractionDataAsync();
        OnDataReceived?.Invoke(data);
      }
      catch (Exception exception)
      {
        Debug.LogError("Error occurred while requesting interaction data.");
        Debug.LogException(exception);
      }
      finally
      {
        s_isSending = false;
      }
    }

    public static async Task<InteractionCounts> PostInteractionDataAsync()
    {
      using var response = await s_httpClient.PostAsync("http://localhost:3000/api/data", null);
      response.EnsureSuccessStatusCode();
      var json = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<InteractionCounts>(json);
    }

    [Serializable]
    public class InteractionCounts
    {
      public int likes;
      public int follows;
      public int shares;

      [SerializeField]
      public Dictionary<string, int> gifts;
    }
  }
}
