// The SteamManager is designed to work with Steamworks.NET
// This file is released into the public domain.
// Where that dedication is not recognized you are granted a perpetual,
// irrevokable license to copy and modify this file as you see fit.
//
// Version: 1.0.5

using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.UI.Menus;




#if UNITY_STANDALONE
using Steamworks;
#endif
//
// The SteamManager provides a base implementation of Steamworks.NET on which you can build upon.
// It handles the basics of starting up and shutting down the SteamAPI for use.
//
[DisallowMultipleComponent]
public class SteamManager : MonoBehaviour
{

  //
#if UNITY_STANDALONE

  //public bool _steamEnabled;
  protected Callback<GameOverlayActivated_t> _GameOverlayActivated;
  protected Callback<P2PSessionRequest_t> _P2PSessionRequest;
  protected Callback<P2PSessionConnectFail_t> _P2PSessionConnectFail;

  static AppId_t _AppID = new AppId_t(954010);

  public bool Enabled;
  public static bool _Enabled
  {
    get
    {
      return Instance.Enabled;
    }
  }

  private static SteamManager s_instance;
  private static SteamManager Instance
  {
    get
    {
      if (s_instance == null)
        s_instance = GameObject.Find("Steam").GetComponent<SteamManager>();
      return s_instance;
    }
  }

  private static bool s_EverInialized;

  private bool m_bInitialized;
  public static bool Initialized
  {
    get
    {
      return Instance.m_bInitialized;
    }
  }

  private SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;
  [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
  private static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
  {
    Debug.LogWarning(pchDebugText);
  }

  private void Awake()
  {
    if (!_Enabled) return;
    // Only one instance of SteamManager at a time!
    /*if (s_instance != null)
    {
      Destroy(gameObject);
      return;
    }*/
    s_instance = this;

    if (s_EverInialized)
    {
      // This is almost always an error.
      // The most common case where this happens is when SteamManager gets destroyed because of Application.Quit(),
      // and then some Steamworks code in some other OnDestroy gets called afterwards, creating a new SteamManager.
      // You should never call Steamworks functions in OnDestroy, always prefer OnDisable if possible.
      throw new System.Exception("Tried to Initialize the SteamAPI twice in one session!");
    }

    // We want our SteamManager Instance to persist across scenes.
    DontDestroyOnLoad(gameObject);

    if (!Packsize.Test())
    {
      Debug.LogError("[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.", this);
    }

    if (!DllCheck.Test())
    {
      Debug.LogError("[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.", this);
    }

    try
    {
      // If Steam is not running or the game wasn't started through Steam, SteamAPI_RestartAppIfNecessary starts the
      // Steam client and also launches this game again if the User owns it. This can act as a rudimentary form of DRM.

      // Once you get a Steam AppID assigned by Valve, you need to replace AppId_t.Invalid with it and
      // remove steam_appid.txt from the game depot. eg: "(AppId_t)480" or "new AppId_t(480)".
      // See the Valve documentation for more information: https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
      if (SteamAPI.RestartAppIfNecessary((AppId_t)954010))
      {
        Application.Quit();
        return;
      }
    }
    catch (System.DllNotFoundException e)
    { // We catch this exception here, as it will be the first occurence of it.
      Debug.LogError("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e, this);

      Application.Quit();
      return;
    }

    // Initializes the Steamworks API.
    // If this returns false then this indicates one of the following conditions:
    // [*] The Steam client isn't running. A running Steam client is required to provide implementations of the various Steamworks interfaces.
    // [*] The Steam client couldn't determine the App ID of game. If you're running your application from the executable or debugger directly then you must have a [code-inline]steam_appid.txt[/code-inline] in your game directory next to the executable, with your app ID in it and nothing else. Steam will look for this file in the current working directory. If you are running your executable from a different directory you may need to relocate the [code-inline]steam_appid.txt[/code-inline] file.
    // [*] Your application is not running under the same OS user context as the Steam client, such as a different user or administration access level.
    // [*] Ensure that you own a license for the App ID on the currently active Steam account. Your game must show up in your Steam library.
    // [*] Your App ID is not completely set up, i.e. in [code-inline]Release State: Unavailable[/code-inline], or it's missing default packages.
    // Valve's documentation for this is located here:
    // https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
    m_bInitialized = SteamAPI.Init();
    if (!m_bInitialized)
    {
      Debug.LogError("[Steamworks.NET] SteamAPI_Init() failed. Refer to Valve's documentation or the comment above this line for more information.", this);

      return;
    }

    s_EverInialized = true;
  }

  // This should only ever get called on first load and after an Assembly reload, You should never Disable the Steamworks Manager yourself.
  private void OnEnable()
  {
    if (!_Enabled) return;
    if (s_instance == null)
      s_instance = this;

    if (!m_bInitialized)
      return;

    if (m_SteamAPIWarningMessageHook == null)
    {
      // Set up our callback to recieve warning messages from Steam.
      // You must launch with "-debug_steamapi" in the launch args to recieve warnings.
      m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
      SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);
    }

    if (!Initialized)
      return;

    void CheckOverlay(GameOverlayActivated_t pCallback)
    {
      if (!_Enabled) return;
      // Overlay deactivated
      if (pCallback.m_bActive == 0)
      {
        return;
      }
      // Overlay activated
      if (Menu.s_InMenus)
        return;
      GameScript.TogglePause();
    }
    _GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(CheckOverlay);
  }

  // OnApplicationQuit gets called too early to shutdown the SteamAPI.
  // Because the SteamManager should be persistent and never disabled or destroyed we can shutdown the SteamAPI here.
  // Thus it is not recommended to perform any Steamworks work in other OnDestroy functions as the order of execution can not be garenteed upon Shutdown. Prefer OnDisable().
  private void OnDestroy()
  {
    if (!_Enabled) return;
    if (s_instance != this)
    {
      return;
    }

    s_instance = null;

    if (!m_bInitialized)
    {
      return;
    }

    SteamAPI.Shutdown();
  }

  private void Update()
  {
    if (!_Enabled) return;
    if (!m_bInitialized)
      return;
    // Run Steam client callbacks
    SteamAPI.RunCallbacks();
  }

  public static CSteamID TEST_GetOtherSteamID()
  {
    CSteamID self = SteamUser.GetSteamID();
    if (self.m_SteamID == 76561198054603834)
      return new CSteamID(76561198958049374);
    return new CSteamID(76561198054603834);
  }

  public static void SendPacket(string message, CSteamID receiver, EP2PSend sendType)
  {
    // allocate new bytes array and copy string characters as bytes
    byte[] bytes = new byte[message.Length * sizeof(char)];
    System.Buffer.BlockCopy(message.ToCharArray(), 0, bytes, 0, bytes.Length);

    SteamNetworking.SendP2PPacket(receiver, bytes, (uint)bytes.Length, sendType);
  }



  //
  static CallResult<SteamUGCQueryCompleted_t> _SteamAPICallResult_Query;
  public static Dictionary<PublishedFileId_t, SteamUGCDetails_t> _PublishedItems;
  public static void Workshop_GetPublishedItems()
  {

    if (_SteamAPICallResult_Query != null)
      return;

    // Get published items
    var steamQueryHandle = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(), EUserUGCList.k_EUserUGCList_Published, EUGCMatchingUGCType.k_EUGCMatchingUGCType_All, EUserUGCListSortOrder.k_EUserUGCListSortOrder_CreationOrderDesc, AppId_t.Invalid, SteamUtils.GetAppID(), 1);
    var steamAPICall = SteamUGC.SendQueryUGCRequest(steamQueryHandle);
    _SteamAPICallResult_Query = CallResult<SteamUGCQueryCompleted_t>.Create();
    _SteamAPICallResult_Query.Set(steamAPICall, CallResult_Query);
  }

  static void CallResult_Query(SteamUGCQueryCompleted_t param, bool bIOFailure)
  {

    _SteamAPICallResult_Query = null;

    if (param.m_eResult == EResult.k_EResultOK)
    {

      Debug.Log("Got num query results: " + param.m_unNumResultsReturned);
      _PublishedItems = new Dictionary<PublishedFileId_t, SteamUGCDetails_t>();
      for (var i = 0u; i < param.m_unNumResultsReturned; i++)
      {

        var get = new SteamUGCDetails_t();
        if (SteamUGC.GetQueryUGCResult(param.m_handle, i, out get))
          if (get.m_nConsumerAppID.m_AppId == _AppID.m_AppId && get.m_eFileType == EWorkshopFileType.k_EWorkshopFileTypeCommunity)
          {
            _PublishedItems.Add(get.m_nPublishedFileId, get);

            Debug.Log($"Got query result item: {get.m_nPublishedFileId}, title: {get.m_rgchTitle}, desc: {get.m_rgchDescription}, filesize: {get.m_nFileSize}");
          }
      }

    }

  }

  static CallResult<SteamUGCQueryCompleted_t> _SteamAPICallResult_GetDetails;
  public static Dictionary<PublishedFileId_t, SteamUGCDetails_t> _SubscribedItems;
  public static void Workshop_GetSubscribed()
  {

    if (_SteamAPICallResult_GetDetails != null)
      return;

    // Get subbed items
    var numsubbed_items = SteamUGC.GetNumSubscribedItems();
    if (numsubbed_items == 0)
    {
      _SubscribedItems = null;
      return;
    }

    var subbed_items = new PublishedFileId_t[numsubbed_items];
    if (SteamUGC.GetSubscribedItems(subbed_items, numsubbed_items) > 0)
    {

      var steamQueryHandle = SteamUGC.CreateQueryUGCDetailsRequest(subbed_items, numsubbed_items);
      var steamAPICall = SteamUGC.SendQueryUGCRequest(steamQueryHandle);
      _SteamAPICallResult_GetDetails = CallResult<SteamUGCQueryCompleted_t>.Create();
      _SteamAPICallResult_GetDetails.Set(steamAPICall, CallResult_GetDetails);

      Debug.Log($"Got {subbed_items.Length} subscribed items");
      return;
    }

    Debug.Log($"Got no subscribed items");
  }

  static void CallResult_GetDetails(SteamUGCQueryCompleted_t param, bool bIOFailure)
  {

    _SteamAPICallResult_GetDetails = null;

    if (param.m_eResult == EResult.k_EResultOK)
    {

      Debug.Log("Got num query results: " + param.m_unNumResultsReturned);
      _SubscribedItems = new Dictionary<PublishedFileId_t, SteamUGCDetails_t>();
      for (var i = 0u; i < param.m_unNumResultsReturned; i++)
      {

        var get = new SteamUGCDetails_t();
        if (SteamUGC.GetQueryUGCResult(param.m_handle, i, out get))
          if (get.m_nConsumerAppID.m_AppId == _AppID.m_AppId && get.m_eFileType == EWorkshopFileType.k_EWorkshopFileTypeCommunity &&
            _PublishedItems != null && !_PublishedItems.ContainsKey(get.m_nPublishedFileId))
          {
            _SubscribedItems.Add(get.m_nPublishedFileId, get);

            Debug.Log($"Got query result item: {get.m_nPublishedFileId}, title: {get.m_rgchTitle}, desc: {get.m_rgchDescription}, filesize: {get.m_nFileSize}");
          }
      }

    }

  }

  public struct SteamWorkshopItem
  {

    public string _title, _description, _filelocation;

  }

  // Create new Steam Workshop item
  static CallResult<CreateItemResult_t> _SteamAPICallResult_Create;
  public static void Workshop_CreateNew(SteamWorkshopItem workshopItem)
  {

    Debug.Log("Creating item");

    if (_SteamAPICallResult_Create != null)
      return;

    try
    {
      var steamAPICall = SteamUGC.CreateItem(_AppID, EWorkshopFileType.k_EWorkshopFileTypeCommunity);
      _SteamAPICallResult_Create = CallResult<CreateItemResult_t>.Create((CreateItemResult_t param, bool bIOFailure) =>
        {
          _SteamAPICallResult_Create = null;

          if (param.m_eResult == EResult.k_EResultOK)
          {
            SteamUGC.SubscribeItem(param.m_nPublishedFileId);
            Workshop_Update(param.m_nPublishedFileId, workshopItem);
          }
          else
          {
            Debug.Log("Couldn't create a new item");
            SteamMenus.ShowInformationDialogue($"Error creating Workshop item: {param.m_eResult}");
          }
        });
      _SteamAPICallResult_Create.Set(steamAPICall);
    }
    catch (System.Exception e)
    {
      Debug.LogError(e.ToString());
      SteamMenus.ShowInformationDialogue($"Error creating Workshop item");
    }
  }

  static CallResult<SubmitItemUpdateResult_t> _SteamAPICallResult_Update;
  static SteamWorkshopItem _Current_Workshopitem;
  public static void Workshop_Update(PublishedFileId_t fileID, SteamWorkshopItem workshopItem)
  {
    if (_SteamAPICallResult_Update != null)
      return;

    Debug.Log($"Updating item: {workshopItem._title ?? "null"}, {workshopItem._description ?? "null"}, {workshopItem._filelocation}");

    try
    {
      var updateHandle = SteamUGC.StartItemUpdate(_AppID, fileID);

      // Set title
      if (workshopItem._title != null)
        SteamUGC.SetItemTitle(updateHandle, workshopItem._title);

      // Set description
      if (workshopItem._description != null)
        SteamUGC.SetItemDescription(updateHandle, workshopItem._description);

      // Set file location
      SteamUGC.SetItemContent(updateHandle, workshopItem._filelocation);

      //SteamUGC.SetItemTags(updateHandle, currentSteamWorkshopItem.Tags);
      //SteamUGC.SetItemPreview(updateHandle, currentSteamWorkshopItem.PreviewImagePath);
      SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);

      _Current_Workshopitem = workshopItem;

      var steamAPICall = SteamUGC.SubmitItemUpdate(updateHandle, "");
      _SteamAPICallResult_Update = CallResult<SubmitItemUpdateResult_t>.Create();
      _SteamAPICallResult_Update.Set(steamAPICall, CallResult_Update);
    }
    catch (System.Exception e)
    {
      Debug.LogError(e.ToString());
      SteamMenus.ShowInformationDialogue($"Error updating Workshop item");
    }
  }

  static void CallResult_Update(SubmitItemUpdateResult_t param, bool bIOFailure)
  {
    _SteamAPICallResult_Update = null;

    if (param.m_eResult == EResult.k_EResultOK)
    {
      Debug.Log("Sucessfully updated Steam Workshop item");
      SteamMenus.ShowInformationDialogue($"Sucessfully updated Steam Workshop item");
    }
    else
    {
      Debug.Log("Couldn't submit the item to Steam: " + param.m_eResult);
      SteamMenus.ShowInformationDialogue($"Error updating Workshop item: {param.m_eResult}");
    }

    // Delete folder
    System.IO.Directory.Delete(_Current_Workshopitem._filelocation, true);
  }

  public static void Workshop_GetUserItems(bool nodialogue = false)
  {
    if (!_Enabled) return;

    try
    {
      Workshop_GetPublishedItems();
      Workshop_GetSubscribed();
    }
    catch (System.Exception e)
    {
      Debug.LogError(e.ToString());

      if (!nodialogue)
        SteamMenus.ShowInformationDialogue($"Error getting items from Steam Workshop");
    }
  }

  public static string Workshop_GetInstalledLocation(PublishedFileId_t file)
  {
    try
    {
      ulong file_size = 0;
      var file_loc = "";
      uint timestamp = 0;
      SteamUGC.GetItemInstallInfo(file, out file_size, out file_loc, 1024, out timestamp);

      if (file_size > 0)
        return file_loc;
    }
    catch (System.Exception e)
    {
      Debug.LogError(e.ToString());
      SteamMenus.ShowInformationDialogue($"Error gathering install location");
    }
    return null;
  }

#endif

  public static class SteamMenus
  {

    static RectTransform _SteamworksMenus, _DialogueInfo;
    public static bool _DialogueMenuShown { get { return _DialogueInfo.transform.localPosition == Vector3.zero; } }
    public static void Init()
    {

      _SteamworksMenus = GameObject.Find("SteamWorkshop").transform as RectTransform;
      _DialogueInfo = _SteamworksMenus.GetChild(0) as RectTransform;

      _DialogueInfo.transform.localPosition = new Vector3(-1000f, 0f, 0f);
      _DialogueInfo.GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
      {

        _DialogueInfo.transform.localPosition = new Vector3(-1000f, 0f, 0f);

      });
    }

    public static void ShowInformationDialogue(string dialogue)
    {
      _DialogueInfo.GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = dialogue;
      _DialogueInfo.transform.localPosition = Vector3.zero;
    }

    public static void HideInformationDialogue()
    {
      _DialogueInfo.GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    }

  }

}