using UnityEngine;
using System;

namespace MagicFarm.TimeSystem
{
    #region Data Structures

    /// <summary>
    /// Data structure for storing game time state.
    /// </summary>
    [Serializable]
    public struct GameTimeData
    {
        public int currentDay;
        public int currentHour;
        public int currentMinute;
        public Season currentSeason;
        public int currentYear;
    }

    /// <summary>
    /// Time segments representing different periods of the day.
    /// </summary>
    public enum TimeSegment
    {
        Night,      // 22:00 - 04:00: Late night
        Dawn,       // 04:00 - 06:00: Early morning/Dawn
        Morning,    // 06:00 - 11:00: Morning
        Noon,       // 11:00 - 13:00: Midday
        Afternoon,  // 13:00 - 17:00: Afternoon
        Dusk,       // 17:00 - 19:00: Evening/Dusk
        Evening     // 19:00 - 22:00: Night
    }

    /// <summary>
    /// Seasons of the year.
    /// </summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    #endregion

    /// <summary>
    /// Manages game time progression including days, seasons, and years.
    /// Provides events for time-based game systems to subscribe to.
    /// </summary>
    public class GameTimeManager : MonoBehaviour
    {
        #region Singleton

        private static GameTimeManager _instance;

        /// <summary>
        /// Gets the singleton instance of the GameTimeManager.
        /// </summary>
        public static GameTimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameTimeManager>();
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Time Configuration")]
        [Tooltip("Real-time seconds required for a complete 24-hour game day.")]
        [SerializeField] private float secondsPerFullDay = 1200f;
        
        [Tooltip("Hour at which a new day begins (typically early morning).")]
        [Range(0, 23)]
        [SerializeField] private int dayStartHour = 6;

        [Header("Calendar Configuration")]
        [Tooltip("Number of days in each season.")]
        [SerializeField] private int daysPerSeason = 30;

        [Header("Debug - Current State")]
        [SerializeField][Range(0, 1)] private float _timeOfDay01;
        [SerializeField] private int _currentMinute;
        [SerializeField] private int _currentHour;
        [SerializeField] private int _currentDay = 1;
        [SerializeField] private Season _currentSeason = Season.Spring;
        [SerializeField] private int _currentYear = 1;
        [SerializeField] private float _currentTimeInSeconds;
        [SerializeField] private TimeSegment _currentTimeSegment;

        #endregion

        #region Constants

        private const int HOURS_PER_DAY = 24;
        private const int MINUTES_PER_HOUR = 60;
        private const int TOTAL_MINUTES_PER_DAY = HOURS_PER_DAY * MINUTES_PER_HOUR;

        #endregion

        #region Private Fields

        private int _lastHourFired = -1;
        private int _lastMinuteFired = -1;
        private bool _isInitialized;

        #endregion

        #region Events

        /// <summary>
        /// Invoked every frame as time progresses.
        /// </summary>
        public event Action OnTimeOfDayChanged;

        /// <summary>
        /// Invoked when the minute value changes.
        /// </summary>
        public event Action<int> OnMinuteChanged;

        /// <summary>
        /// Invoked when the hour value changes.
        /// </summary>
        public event Action<int> OnHourChanged;

        /// <summary>
        /// Invoked when a new day begins.
        /// </summary>
        public event Action<int> OnDayChanged;

        /// <summary>
        /// Invoked when the season changes.
        /// </summary>
        public event Action<Season> OnSeasonChanged;

        /// <summary>
        /// Invoked when a new year begins.
        /// </summary>
        public event Action<int> OnYearChanged;

        /// <summary>
        /// Invoked when the time segment changes (e.g., Morning to Noon).
        /// </summary>
        public event Action<TimeSegment> OnTimeSegmentChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current time of day as a normalized value (0-1).
        /// </summary>
        public float TimeOfDay01 => _timeOfDay01;

        /// <summary>
        /// Gets the current minute (0-59).
        /// </summary>
        public int CurrentMinute => _currentMinute;

        /// <summary>
        /// Gets the current hour (0-23).
        /// </summary>
        public int CurrentHour => _currentHour;

        /// <summary>
        /// Gets the current day of the season.
        /// </summary>
        public int CurrentDay => _currentDay;

        /// <summary>
        /// Gets the current season.
        /// </summary>
        public Season CurrentSeason => _currentSeason;

        /// <summary>
        /// Gets the current year.
        /// </summary>
        public int CurrentYear => _currentYear;

        /// <summary>
        /// Gets the current time segment.
        /// </summary>
        public TimeSegment CurrentTimeSegment => _currentTimeSegment;

        /// <summary>
        /// Gets the current game time as a formatted string (HH:MM).
        /// </summary>
        public string FormattedTime => $"{_currentHour:D2}:{_currentMinute:D2}";

        /// <summary>
        /// Gets the current game time data as a struct.
        /// </summary>
        public GameTimeData GetTimeData()
        {
            return new GameTimeData
            {
                currentDay = _currentDay,
                currentHour = _currentHour,
                currentMinute = _currentMinute,
                currentSeason = _currentSeason,
                currentYear = _currentYear
            };
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSingleton();
        }

        private void Start()
        {
            InitializeTime();
        }

        private void Update()
        {
            ProgressTime();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the singleton pattern.
        /// </summary>
        private void InitializeSingleton()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Initializes the time system to the starting time.
        /// </summary>
        private void InitializeTime()
        {
            SetTime(dayStartHour, 0);
            _isInitialized = true;
            UpdateClock(forceUpdate: true);
        }

        #endregion

        #region Time Progression

        /// <summary>
        /// Progresses time based on real-time delta.
        /// </summary>
        private void ProgressTime()
        {
            _currentTimeInSeconds += Time.deltaTime;

            if (_currentTimeInSeconds >= secondsPerFullDay)
            {
                _currentTimeInSeconds -= secondsPerFullDay;
            }

            UpdateClock(forceUpdate: false);
        }

        /// <summary>
        /// Updates the clock and fires appropriate events.
        /// </summary>
        private void UpdateClock(bool forceUpdate)
        {
            CalculateTimeValues();
            
            OnTimeOfDayChanged?.Invoke();

            CheckMinuteChange(forceUpdate);
            CheckHourChange(forceUpdate);
        }

        /// <summary>
        /// Calculates current hour and minute from elapsed time.
        /// </summary>
        private void CalculateTimeValues()
        {
            _timeOfDay01 = _currentTimeInSeconds / secondsPerFullDay;
            float totalMinutes = _timeOfDay01 * TOTAL_MINUTES_PER_DAY;
            
            _currentHour = Mathf.FloorToInt(totalMinutes / MINUTES_PER_HOUR) % HOURS_PER_DAY;
            _currentMinute = Mathf.FloorToInt(totalMinutes % MINUTES_PER_HOUR);
        }

        /// <summary>
        /// Checks and fires minute change event if needed.
        /// </summary>
        private void CheckMinuteChange(bool forceUpdate)
        {
            if (_currentMinute != _lastMinuteFired || forceUpdate)
            {
                OnMinuteChanged?.Invoke(_currentMinute);
                _lastMinuteFired = _currentMinute;
            }
        }

        /// <summary>
        /// Checks and fires hour change event if needed.
        /// </summary>
        private void CheckHourChange(bool forceUpdate)
        {
            if (_currentHour != _lastHourFired || forceUpdate)
            {
                OnHourChanged?.Invoke(_currentHour);

                if (_isInitialized && _currentHour == dayStartHour && _lastHourFired != dayStartHour)
                {
                    IncrementDay();
                }

                UpdateTimeSegment();
                _lastHourFired = _currentHour;
            }
        }

        #endregion

        #region Time Segment Management

        /// <summary>
        /// Updates the current time segment based on the hour.
        /// </summary>
        private void UpdateTimeSegment()
        {
            TimeSegment newSegment = DetermineTimeSegment(_currentHour);

            if (newSegment != _currentTimeSegment)
            {
                _currentTimeSegment = newSegment;
                OnTimeSegmentChanged?.Invoke(_currentTimeSegment);
            }
        }

        /// <summary>
        /// Determines the time segment for a given hour.
        /// </summary>
        private TimeSegment DetermineTimeSegment(int hour)
        {
            if (hour >= 4 && hour < 6) return TimeSegment.Dawn;
            if (hour >= 6 && hour < 11) return TimeSegment.Morning;
            if (hour >= 11 && hour < 13) return TimeSegment.Noon;
            if (hour >= 13 && hour < 17) return TimeSegment.Afternoon;
            if (hour >= 17 && hour < 19) return TimeSegment.Dusk;
            if (hour >= 19 && hour < 22) return TimeSegment.Evening;
            return TimeSegment.Night;
        }

        #endregion

        #region Calendar Management

        /// <summary>
        /// Increments the day counter and checks for season change.
        /// </summary>
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

        /// <summary>
        /// Increments the season and checks for year change.
        /// </summary>
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

        /// <summary>
        /// Increments the year counter.
        /// </summary>
        private void IncrementYear()
        {
            _currentYear++;
            OnYearChanged?.Invoke(_currentYear);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the time to a specific hour and minute.
        /// </summary>
        /// <param name="hour">Hour to set (0-23).</param>
        /// <param name="minute">Minute to set (0-59).</param>
        public void SetTime(int hour, int minute)
        {
            hour = Mathf.Clamp(hour, 0, HOURS_PER_DAY - 1);
            minute = Mathf.Clamp(minute, 0, MINUTES_PER_HOUR - 1);

            float totalMinutes = (hour * MINUTES_PER_HOUR) + minute;
            _timeOfDay01 = totalMinutes / TOTAL_MINUTES_PER_DAY;
            _currentTimeInSeconds = _timeOfDay01 * secondsPerFullDay;
            
            UpdateClock(forceUpdate: true);
        }

        /// <summary>
        /// Sets the current season.
        /// </summary>
        public void SetSeason(Season season)
        {
            _currentSeason = season;
            OnSeasonChanged?.Invoke(_currentSeason);
        }

        /// <summary>
        /// Pauses time progression.
        /// </summary>
        public void PauseTime()
        {
            enabled = false;
        }

        /// <summary>
        /// Resumes time progression.
        /// </summary>
        public void ResumeTime()
        {
            enabled = true;
        }

        /// <summary>
        /// Gets whether time is currently paused.
        /// </summary>
        public bool IsTimePaused()
        {
            return !enabled;
        }

        #endregion
    }
}