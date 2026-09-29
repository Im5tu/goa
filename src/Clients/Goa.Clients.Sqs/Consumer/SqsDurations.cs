namespace Goa.Clients.Sqs.Consumer;

/// <summary>
/// Converts <see cref="TimeSpan"/> values to the whole-second integers the SQS API expects.
/// </summary>
internal static class SqsDurations
{
    /// <summary>
    /// The maximum visibility timeout SQS accepts (12 hours).
    /// </summary>
    public static readonly TimeSpan MaxVisibilityTimeout = TimeSpan.FromSeconds(SqsServiceClient.MaxVisibilityTimeoutSeconds);

    /// <summary>
    /// The maximum long-poll wait SQS accepts (20 seconds).
    /// </summary>
    public static readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Converts a duration to whole seconds, rounding up, when it lies within [0, <paramref name="max"/>].
    /// </summary>
    /// <param name="value">The duration to convert.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <param name="seconds">The duration in whole seconds, rounded up.</param>
    /// <returns><c>true</c> when the duration is in range; otherwise <c>false</c>.</returns>
    public static bool TryToSeconds(TimeSpan value, TimeSpan max, out int seconds)
    {
        if (value < TimeSpan.Zero || value > max)
        {
            seconds = 0;
            return false;
        }

        seconds = (int)Math.Ceiling(value.TotalSeconds);
        return seconds <= (int)max.TotalSeconds;
    }
}
