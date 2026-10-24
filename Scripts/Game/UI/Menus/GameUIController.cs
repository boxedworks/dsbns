using System;
using UnityEngine;
using UnityEngine.UIElements;

public class GameUIController : MonoBehaviour
{
  static GameUIController s_Instance;
  static int? s_PendingIncrementalHealth;
  static int? s_PendingIncrementalRespawnCountdown;

  [SerializeField] PanelRenderer _uiDocument;
  [SerializeField] Transform _playerHead;
  [SerializeField] Camera _worldCamera;
  [SerializeField] float _verticalOffset = 0.25f;
  [SerializeField, Min(1f)] float _maximumXp = 100f;
  [SerializeField, Min(0f)] float _currentXp;

  IPanel _panel;
  VisualElement _xpBar;
  VisualElement _xpFill;
  Label _xpLabel;
  Label _incrementalHealthLabel;
  Label _incrementalRespawnCountdownLabel;
  int _incrementalHealth;
  int _incrementalRespawnCountdown;
  bool _showIncrementalHealth;
  bool _showIncrementalRespawnCountdown;

  public float CurrentXp => _currentXp;
  public float MaximumXp => _maximumXp;

  void Awake()
  {
    s_Instance = this;
    _uiDocument.RegisterUIReloadCallback(OnUIReload);

    SetExperience(_currentXp, _maximumXp);

    if (s_PendingIncrementalHealth.HasValue)
      SetIncrementalHealthDisplay(s_PendingIncrementalHealth.Value);
    if (s_PendingIncrementalRespawnCountdown.HasValue)
      SetIncrementalRespawnCountdownDisplay(s_PendingIncrementalRespawnCountdown.Value);
  }

  int m_Version;
  void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
  {
    // Skip redundant execution if the UI has not actually changed
    if (version == m_Version)
      return;

    m_Version = version;

    // Wire up your UI Toolkit elements here (e.g., rootElement.Q<Button>("my-button"))
    _panel = rootElement.panel;
    _xpBar = rootElement.Q<VisualElement>("player-xp-bar");
    _xpFill = rootElement.Q<VisualElement>("xp-fill");
    _xpLabel = rootElement.Q<Label>("xp-label");
    _incrementalHealthLabel = rootElement.Q<Label>("incremental-health");
    _incrementalRespawnCountdownLabel = rootElement.Q<Label>("incremental-respawn-countdown");
    UpdateIncrementalHealthLabel();
    UpdateIncrementalRespawnCountdownLabel();
  }

  void LateUpdate()
  {
    if (_playerHead == null || _xpBar == null)
    {
      return;
    }

    var cameraToUse = _worldCamera != null ? _worldCamera : Camera.main;
    var panel = _panel;
    if (cameraToUse == null || panel == null)
    {
      _xpBar.style.display = DisplayStyle.None;
      return;
    }

    _xpBar.style.display = DisplayStyle.Flex;
    var position = RuntimePanelUtils.CameraTransformWorldToPanel(
      panel,
      _playerHead.position + Vector3.up * _verticalOffset,
      cameraToUse
    );
    _xpBar.style.left = position.x;
    _xpBar.style.top = position.y;
  }

  public void SetExperience(float currentXp, float maximumXp)
  {
    if (maximumXp <= 0f)
    {
      throw new ArgumentOutOfRangeException(nameof(maximumXp), maximumXp, "Maximum XP must be greater than zero.");
    }

    _maximumXp = maximumXp;
    _currentXp = Mathf.Clamp(currentXp, 0f, _maximumXp);

    var progress = _currentXp / _maximumXp;
    if (_xpFill != null)
    {
      _xpFill.style.width = Length.Percent(progress * 100f);
      _xpLabel.text = $"XP {Mathf.RoundToInt(_currentXp)} / {Mathf.RoundToInt(_maximumXp)}";
    }
  }

  public void SetNormalizedExperience(float normalizedXp)
  {
    SetExperience(Mathf.Clamp01(normalizedXp) * _maximumXp, _maximumXp);
  }

  public static void SetIncrementalHealth(int health)
  {
    s_PendingIncrementalHealth = health;
    s_Instance?.SetIncrementalHealthDisplay(health);
  }

  public static void SetIncrementalRespawnCountdown(int secondsRemaining)
  {
    s_PendingIncrementalRespawnCountdown = secondsRemaining;
    s_Instance?.SetIncrementalRespawnCountdownDisplay(secondsRemaining);
  }

  public static void HideIncrementalRespawnCountdown()
  {
    s_PendingIncrementalRespawnCountdown = null;
    s_Instance?.HideIncrementalRespawnCountdownDisplay();
  }

  void SetIncrementalHealthDisplay(int health)
  {
    _incrementalHealth = health;
    _showIncrementalHealth = true;
    UpdateIncrementalHealthLabel();
  }

  void SetIncrementalRespawnCountdownDisplay(int secondsRemaining)
  {
    _incrementalRespawnCountdown = secondsRemaining;
    _showIncrementalRespawnCountdown = true;
    UpdateIncrementalRespawnCountdownLabel();
  }

  void HideIncrementalRespawnCountdownDisplay()
  {
    _showIncrementalRespawnCountdown = false;
    UpdateIncrementalRespawnCountdownLabel();
  }

  void UpdateIncrementalHealthLabel()
  {
    if (_incrementalHealthLabel == null)
      return;

    _incrementalHealthLabel.style.display = _showIncrementalHealth ? DisplayStyle.Flex : DisplayStyle.None;
    _incrementalHealthLabel.text = $"Health: {_incrementalHealth}";
  }

  void UpdateIncrementalRespawnCountdownLabel()
  {
    if (_incrementalRespawnCountdownLabel == null)
      return;

    _incrementalRespawnCountdownLabel.style.display = _showIncrementalRespawnCountdown ? DisplayStyle.Flex : DisplayStyle.None;
    _incrementalRespawnCountdownLabel.text = $"Respawning in {_incrementalRespawnCountdown}";
  }
}
