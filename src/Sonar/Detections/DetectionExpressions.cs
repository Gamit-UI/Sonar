using System.Linq.Expressions;
using Sonar.Rules.Serialization;

namespace Sonar.Detections;

internal sealed record DetectionExpressions(Expression<Func<WinEvent, bool>> ReducedExpression);
