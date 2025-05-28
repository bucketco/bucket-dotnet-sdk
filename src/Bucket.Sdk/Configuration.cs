namespace Bucket.Sdk;

using Microsoft.Extensions.Configuration;

/// <summary>
///     The <see cref="Configuration" /> class is used to configure the behavior of the Bucket SDK.
/// </summary>
[PublicAPI]
public sealed class Configuration
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string _secretKey = string.Empty;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Uri _apiBaseUri = new("https://front.bucket.co");

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private OperationMode _mode = OperationMode.LocalEvaluation;

    /// <summary>
    ///     The secret key used to authenticate with the Bucket API.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid key.</exception>
    public required string SecretKey
    {
        [DebuggerStepThrough]
        get => _secretKey;
        [DebuggerStepThrough]
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _secretKey = value;
        }
    }

    /// <summary>
    ///     The base URI of the Bucket API.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null"/>.</exception>
    public Uri ApiBaseUri
    {
        [DebuggerStepThrough]
        get => _apiBaseUri;
        [DebuggerStepThrough]
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _apiBaseUri = value;
        }
    }

    /// <summary>
    ///     The operation mode of the SDK.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is invalid.</exception>
    public OperationMode Mode
    {
        [DebuggerStepThrough]
        get => _mode;
        [DebuggerStepThrough]
        set
        {
            if (value is < OperationMode.Offline or > OperationMode.RemoteEvaluation)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _mode = value;
        }
    }

    /// <summary>
    ///     The output configuration.
    /// </summary>
    public OutputConfiguration Output { get; } = new();

    /// <summary>
    ///     The features configuration.
    /// </summary>
    public FeaturesConfiguration Features { get; } = new();

    /// <summary>
    ///     Creates a new instance of the <see cref="Configuration" /> by loading it from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    /// <param name="section">The name of the configuration section to load settings from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration" /> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="section" /> is <see langword="null"/> or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="section" /> is not found in the configuration.</exception>
    public static Configuration FromConfiguration(
        IConfiguration configuration,
        string section = "Bucket")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(section);

        return configuration.GetSection(section).Get<Configuration>()
               ?? throw new ArgumentException("Configuration section not found", nameof(section));
    }

    /// <summary>
    ///     The configuration related to the output.
    /// </summary>
    public sealed class OutputConfiguration
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan _flushInterval = TimeSpan.FromSeconds(10);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _maxMessages = 100;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan _rollingWindow = TimeSpan.FromSeconds(60);

        /// <summary>
        ///     The maximum number of messages to buffer before flushing.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than <c>1</c>.</exception>
        public int MaxMessages
        {
            [DebuggerStepThrough]
            get => _maxMessages;
            [DebuggerStepThrough]
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
                _maxMessages = value;
            }
        }

        /// <summary>
        ///     The interval at which the buffer is flushed.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is invalid.</exception>
        public TimeSpan FlushInterval
        {
            [DebuggerStepThrough]
            get => _flushInterval;
            [DebuggerStepThrough]
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
                _flushInterval = value;
            }
        }

        /// <summary>
        ///     The rolling window used to rate limit the output.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is invalid.</exception>
        public TimeSpan RollingWindow
        {
            [DebuggerStepThrough]
            get => _rollingWindow;
            [DebuggerStepThrough]
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
                _rollingWindow = value;
            }
        }
    }

    /// <summary>
    ///     The configuration related to the features.
    /// </summary>
    public sealed class FeaturesConfiguration
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan _refreshInterval = TimeSpan.FromSeconds(60);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan _staleAge = TimeSpan.FromMinutes(10);

        /// <summary>
        ///     The interval at which the features are refreshed (if running in <see cref="OperationMode.LocalEvaluation" /> mode).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is invalid.</exception>
        public TimeSpan RefreshInterval
        {
            [DebuggerStepThrough]
            get => _refreshInterval;
            [DebuggerStepThrough]
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
                _refreshInterval = value;
            }
        }

        /// <summary>
        ///     The age at which the cached features are considered stale (if running in
        ///     <see cref="OperationMode.LocalEvaluation" /> mode).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is invalid.</exception>
        public TimeSpan StaleAge
        {
            [DebuggerStepThrough]
            get => _staleAge;
            [DebuggerStepThrough]
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
                _staleAge = value;
            }
        }
    }
}
