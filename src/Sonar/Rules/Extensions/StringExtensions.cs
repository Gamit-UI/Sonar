using Sonar.Detections;
using Sonar.Rules.Correlations;

namespace Sonar.Rules.Extensions;

internal static class StringExtensions
{
    private const string Info = "info";
    extension(string level)
    {
        public DetectionSeverity FromLevel()
        {
            if (level.Equals(nameof(DetectionSeverity.Critical), StringComparison.OrdinalIgnoreCase))
            {
                return DetectionSeverity.Critical;
            }

            if (level.Equals(nameof(DetectionSeverity.High), StringComparison.OrdinalIgnoreCase))
            {
                return DetectionSeverity.High;
            }

            if (level.Equals(nameof(DetectionSeverity.Medium), StringComparison.OrdinalIgnoreCase))
            {
                return DetectionSeverity.Medium;
            }

            if (level.Equals(nameof(DetectionSeverity.Low), StringComparison.OrdinalIgnoreCase))
            {
                return DetectionSeverity.Low;
            }

            if (level.Equals(Info, StringComparison.OrdinalIgnoreCase))
            {
                return DetectionSeverity.Informational;
            }

            return DetectionSeverity.Informational;
        }

        public DetectionStatus FromStatus()
        {
            if (level.Equals(nameof(DetectionStatus.Unsupported), StringComparison.OrdinalIgnoreCase))
            {
                return DetectionStatus.Unsupported;
            }
        
            if (level.Equals(nameof(DetectionStatus.Deprecated), StringComparison.OrdinalIgnoreCase))
            {
                return DetectionStatus.Deprecated;
            }

            if (level.Equals(nameof(DetectionStatus.Experimental), StringComparison.OrdinalIgnoreCase))
            {
                return DetectionStatus.Experimental;
            }

            if (level.Equals(nameof(DetectionStatus.Test), StringComparison.OrdinalIgnoreCase))
            {
                return DetectionStatus.Test;
            }

            if (level.Equals(nameof(DetectionStatus.Stable), StringComparison.OrdinalIgnoreCase))
            {
                return DetectionStatus.Stable;
            }

            return DetectionStatus.Unsupported;
        }
    }

    extension(string time)
    {
        public TimeSpan ToTimeframe()
        {
            if (time.EndsWith('s'))
            {
                return TimeSpan.FromSeconds(int.Parse(time[..^1]));
            }
        
            if (time.EndsWith('m'))
            {
                return TimeSpan.FromMinutes(int.Parse(time[..^1]));
            }
        
            if (time.EndsWith('h'))
            {
                return TimeSpan.FromHours(int.Parse(time[..^1]));
            }
        
            if (time.EndsWith('d'))
            {
                return TimeSpan.FromDays(int.Parse(time[..^1]));
            }
        
            if (time.EndsWith('M'))
            {
                return TimeSpan.FromDays(int.Parse(time[..^1]) * 31 - 1);
            }

            throw new ArgumentException($"Unknown time format: {time}");
        }

        public Operator ToOperator()
        {
            return time switch
            {
                Constants.Equal => Operator.Equal,
                Constants.GreaterThanOrEqual or Constants.Gte => Operator.GreaterThanOrEqual,
                Constants.GreaterThan or Constants.Gt => Operator.GreaterThan,
                Constants.LessThanOrEqual or Constants.Lte => Operator.LessThanOrEqual,
                Constants.LessThan or Constants.Lt => Operator.LessThan,
                _ => throw new ArgumentException($"Unknown operator: {time}")
            };
        }

        public IEnumerable<string> FromAbnormalPattern()
        {
            return time.Split(Constants.AbnormalSeparator, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    internal readonly ref struct Split(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        public readonly ReadOnlySpan<char> Left = left;
        public readonly ReadOnlySpan<char> Right = right;
    }

    public static Split SplitOnce(this string input, ReadOnlySpan<char> separator)
    {
        return input.AsSpan().SplitOnce(separator);
    }
    
    public static Split SplitOnce(this ReadOnlySpan<char> span, ReadOnlySpan<char> separator)
    {
        var i = 0;
        ReadOnlySpan<char> left = string.Empty;
        ReadOnlySpan<char> right = string.Empty;
        foreach (var range in span.Split(separator))
        {
            var value = span[range];
            if (i == 0)
            {
                left = value;
            }
            else
            {
                var (offset, _) = range.GetOffsetAndLength(span.Length);
                right = span[offset..];
                break;
            }
            
            i++;
        }

        return new Split(left, right);
    }
    
    extension(string input)
    {
        public string TakeLast(char separator)
        {
            var span = input.AsSpan();
            foreach (var range in span.Split(separator))
            {
                var (offset, length) = range.GetOffsetAndLength(span.Length);
                var tail = offset + length == span.Length;
                if (!tail) continue;
                return new string(span[range]);
            }

            return string.Empty;
        }

        public string StripDomain() => input.Contains('.') ? input[..input.IndexOf('.')] : input;
        
        public string StripDollarSign() => input.Contains('$') ? input[..input.IndexOf('$')] : input;
    }
}