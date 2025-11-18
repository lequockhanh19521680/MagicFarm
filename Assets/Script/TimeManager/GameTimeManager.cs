using UnityEngine;
using System;

[System.Serializable]
public struct GameTimeData
{
    public int currentDay;
    public int currentHour;
    public int currentMinute;
    public Season currentSeason;
    public int currentYear;
}

/// <summary>
/// Các phân đoạn thời gian trong ngày.
/// </summary>
public enum TimeSegment
{
    Night,      // 22h - 04h: Đêm khuya
    Dawn,       // 04h - 06h: Rạng đông (mờ sáng)
    Morning,    // 06h - 11h: Buổi sáng
    Noon,       // 11h - 13h: Buổi trưa (đứng bóng)
    Afternoon,  // 13h - 17h: Buổi chiều
    Dusk,       // 17h - 19h: Hoàng hôn (chạng vạng)
    Evening     // 19h - 22h: Buổi tối
}

public enum Season
{
    Spring, Summer, Autumn, Winter
}

public class GameTimeManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    private static GameTimeManager _instance;
    public static GameTimeManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<GameTimeManager>();
            return _instance;
        }
    }

    [Header("Time Settings")]
    [Tooltip("Số giây thực tế cho 1 ngày 24h trong game.")]
    [SerializeField] private float secondsPerFullDay = 1200f;
    
    [Tooltip("Giờ bắt đầu ngày mới (thường là sáng sớm).")]
    [Range(0, 23)]
    [SerializeField] private int dayStartHour = 6;

    [Header("Seasons & Years")]
    [SerializeField] private int daysPerSeason = 30;

    // --- Debug State ---
    [Header("Debug - Current Time State")]
    [SerializeField][Range(0, 1)] private float _timeOfDay01;
    [SerializeField] private int _currentMinute;
    [SerializeField] private int _currentHour;
    [SerializeField] private int _currentDay = 1;
    [SerializeField] private Season _currentSeason = Season.Spring;
    [SerializeField] private int _currentYear = 1;
    
    [Tooltip("Thời gian hiện tại (giây) trong ngày.")]
    [SerializeField] private float _currentTimeInSeconds;

    // --- Logic State ---
    [Header("Debug - Time Segment")]
    [SerializeField] private TimeSegment _currentTimeSegment;

    private int _lastHourFired = -1;
    private int _lastMinuteFired = -1;
    private bool _isInitialized = false;

    // --- Events ---
    public event Action OnTimeOfDayChanged;
    public event Action<int> OnMinuteChanged;
    public event Action<int> OnHourChanged;
    public event Action<int> OnDayChanged;
    public event Action<Season> OnSeasonChanged;
    public event Action<int> OnYearChanged;
    
    /// <summary>
    /// Sự kiện bắn ra khi chuyển vùng thời gian (Sáng -> Trưa, v.v...)
    /// Payload: (TimeSegment) vùng thời gian mới
    /// </summary>
    public event Action<TimeSegment> OnTimeSegmentChanged;

    // --- Getters ---
    public float TimeOfDay01 => _timeOfDay01;
    public int CurrentMinute => _currentMinute;
    public int CurrentHour => _currentHour;
    public int CurrentDay => _currentDay;
    public Season CurrentSeason => _currentSeason;
    public int CurrentYear => _currentYear;
    public TimeSegment CurrentTimeSegment => _currentTimeSegment;

    // --- Initialization ---
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        SetTime(dayStartHour, 0); 
        _isInitialized = true;
        
        // Tính toán segment ban đầu
        UpdateClock(true);
    }

    private void Update()
    {
        _currentTimeInSeconds += Time.deltaTime;

        if (_currentTimeInSeconds >= secondsPerFullDay)
        {
            _currentTimeInSeconds -= secondsPerFullDay;
        }
        
        UpdateClock(false);
    }

    // --- Core Logic ---
    private void UpdateClock(bool forceUpdate)
    {
        _timeOfDay01 = _currentTimeInSeconds / secondsPerFullDay;
        
        float totalMinutes = _timeOfDay01 * 24 * 60;
        _currentHour = Mathf.FloorToInt(totalMinutes / 60) % 24;
        _currentMinute = Mathf.FloorToInt(totalMinutes % 60);

        OnTimeOfDayChanged?.Invoke(); 

        if (_currentMinute != _lastMinuteFired || forceUpdate)
        {
            OnMinuteChanged?.Invoke(_currentMinute);
            _lastMinuteFired = _currentMinute;
        }

        if (_currentHour != _lastHourFired || forceUpdate)
        {
            OnHourChanged?.Invoke(_currentHour);
            
            // Check Day Change
            if (_isInitialized && _currentHour == dayStartHour && _lastHourFired != dayStartHour)
            {
                IncrementDay();
            }

            // Check Time Segment Change
            CheckTimeSegment();

            _lastHourFired = _currentHour;
        }
    }

    /// <summary>
    /// Xác định vùng thời gian dựa trên giờ hiện tại.
    /// </summary>
    private void CheckTimeSegment()
    {
        TimeSegment newSegment;

        // Logic phân chia giờ
        if (_currentHour >= 4 && _currentHour < 6) newSegment = TimeSegment.Dawn;        // 4h-6h
        else if (_currentHour >= 6 && _currentHour < 11) newSegment = TimeSegment.Morning; // 6h-11h
        else if (_currentHour >= 11 && _currentHour < 13) newSegment = TimeSegment.Noon;   // 11h-13h
        else if (_currentHour >= 13 && _currentHour < 17) newSegment = TimeSegment.Afternoon;// 13h-17h
        else if (_currentHour >= 17 && _currentHour < 19) newSegment = TimeSegment.Dusk;     // 17h-19h
        else if (_currentHour >= 19 && _currentHour < 22) newSegment = TimeSegment.Evening;  // 19h-22h
        else newSegment = TimeSegment.Night;                                                 // 22h-4h

        // Nếu thay đổi so với trước đó thì bắn event
        if (newSegment != _currentTimeSegment)
        {
            _currentTimeSegment = newSegment;
            OnTimeSegmentChanged?.Invoke(_currentTimeSegment);
            // Debug.Log($"Time Segment Changed: {newSegment}");
        }
    }

    private void IncrementDay()
    {
        _currentDay++;
        OnDayChanged?.Invoke(_currentDay);

        if (_currentDay > daysPerSeason)
        {
            _currentDay = 1;
            IncrementSeason();
        }
    }

    private void IncrementSeason()
    {
        if (_currentSeason == Season.Winter)
        {
            _currentSeason = Season.Spring;
            IncrementYear();
        }
        else
        {
            _currentSeason++;
        }
        OnSeasonChanged?.Invoke(_currentSeason);
    }

    private void IncrementYear()
    {
        _currentYear++;
        OnYearChanged?.Invoke(_currentYear);
    }

    // --- Public Setter ---
    public void SetTime(int hour, int minute)
    {
        float totalMinutes = (hour * 60) + minute;
        _timeOfDay01 = totalMinutes / (24 * 60);
        _currentTimeInSeconds = _timeOfDay01 * secondsPerFullDay;
        UpdateClock(true);
    }
}