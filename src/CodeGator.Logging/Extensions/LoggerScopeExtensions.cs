namespace Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

/// <summary>
/// This class contains logging scope extension methods.
/// </summary>
public static class LoggerScopeExtensions
{
    /// <summary>
    /// This method begins a logging scope that sanitizes values using logging attributes.
    /// </summary>
    /// <param name="logger">Logger that receives the created scope.</param>
    /// <param name="redactorProvider">Provider used to redact classified values.</param>
    /// <param name="state">State object whose public members are flattened into scope keys.</param>
    /// <param name="maxDepth">Maximum recursion depth when traversing member values.</param>
    /// <returns>A disposable scope token.</returns>
    public static IDisposable? BeginSanitizedScope(
        this ILogger logger,
        IRedactorProvider redactorProvider,
        object? state,
        int maxDepth = 2
        )
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(redactorProvider);

        var scopeState = ScopeSanitizer.CreateScopeState(
            redactorProvider,
            state,
            maxDepth
            );

        return logger.BeginScope(scopeState);
    }

    /// <summary>
    /// This method begins a logging scope that sanitizes values using logging attributes.
    /// </summary>
    /// <param name="logger">Logger that receives the created scope.</param>
    /// <param name="serviceProvider">Provider used to resolve an <see cref="IRedactorProvider"/>.</param>
    /// <param name="state">State object whose public members are flattened into scope keys.</param>
    /// <param name="maxDepth">Maximum recursion depth when traversing member values.</param>
    /// <returns>A disposable scope token.</returns>
    public static IDisposable? BeginSanitizedScope(
        this ILogger logger,
        IServiceProvider serviceProvider,
        object? state,
        int maxDepth = 2
        )
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var redactorProvider = serviceProvider.GetService(typeof(IRedactorProvider)) as IRedactorProvider;
        if (redactorProvider is null)
        {
            throw new InvalidOperationException(
                $"No {nameof(IRedactorProvider)} is registered. Call EnableRedaction and AddRedaction."
                );
        }

        return logger.BeginSanitizedScope(
            redactorProvider,
            state,
            maxDepth
            );
    }

    private static class ScopeSanitizer
    {
        private static readonly ConcurrentDictionary<Type, TypePlan> _typePlans = new();

        public static IReadOnlyList<KeyValuePair<string, object?>> CreateScopeState(
            IRedactorProvider redactorProvider,
            object? state,
            int maxDepth
            )
        {
            ArgumentNullException.ThrowIfNull(redactorProvider);

            if (maxDepth < 0)
            {
                maxDepth = 0;
            }

            var list = new List<KeyValuePair<string, object?>>();

            if (state is null)
            {
                return list;
            }

            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            AddObject(
                list,
                visited,
                redactorProvider,
                prefix: string.Empty,
                value: state,
                maxDepth: maxDepth
                );

            return list;
        }

        private static void AddObject(
            List<KeyValuePair<string, object?>> list,
            HashSet<object> visited,
            IRedactorProvider redactorProvider,
            string prefix,
            object? value,
            int maxDepth
            )
        {
            if (value is null)
            {
                AddLeaf(list, prefix, null);
                return;
            }

            if (maxDepth == 0 || IsSimple(value))
            {
                AddLeaf(list, prefix, value);
                return;
            }

            if (!visited.Add(value))
            {
                AddLeaf(list, prefix, "[cyclic]");
                return;
            }

            if (TryAddDictionary(list, visited, redactorProvider, prefix, value, maxDepth))
            {
                return;
            }

            if (TryAddKeyValueEnumerable(list, visited, redactorProvider, prefix, value, maxDepth))
            {
                return;
            }

            var type = value.GetType();
            var plan = _typePlans.GetOrAdd(type, CreateTypePlan);

            foreach (var member in plan.Members)
            {
                object? propValue;
                try
                {
                    propValue = member.Getter(value);
                }
                catch
                {
                    continue;
                }

                var memberName = Join(prefix, member.Name);

                if (member.Classification is DataClassification dc)
                {
                    AddLeaf(
                        list,
                        memberName,
                        Redact(propValue, redactorProvider, dc)
                        );

                    continue;
                }

                if (propValue is byte[] rawBytes)
                {
                    AddLeaf(list, memberName, $"byte[{rawBytes.Length}]");
                    continue;
                }

                AddObject(
                    list,
                    visited,
                    redactorProvider,
                    memberName,
                    propValue,
                    maxDepth - 1
                    );
            }
        }

        private static bool TryAddDictionary(
            List<KeyValuePair<string, object?>> list,
            HashSet<object> visited,
            IRedactorProvider redactorProvider,
            string prefix,
            object value,
            int maxDepth
            )
        {
            if (value is not System.Collections.IDictionary dictionary)
            {
                return false;
            }

            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                AddObject(
                    list,
                    visited,
                    redactorProvider,
                    Join(prefix, key),
                    entry.Value,
                    maxDepth - 1
                    );
            }

            return true;
        }

        private static bool TryAddKeyValueEnumerable(
            List<KeyValuePair<string, object?>> list,
            HashSet<object> visited,
            IRedactorProvider redactorProvider,
            string prefix,
            object value,
            int maxDepth
            )
        {
            if (value is not IEnumerable<KeyValuePair<string, object?>> kvps)
            {
                return false;
            }

            foreach (var kvp in kvps)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    continue;
                }

                AddObject(
                    list,
                    visited,
                    redactorProvider,
                    Join(prefix, kvp.Key),
                    kvp.Value,
                    maxDepth - 1
                    );
            }

            return true;
        }

        private static void AddLeaf(
            List<KeyValuePair<string, object?>> list,
            string name,
            object? value
            )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "scope";
            }

            list.Add(new KeyValuePair<string, object?>(name, value));
        }

        private static bool IsSimple(
            object value
            )
        {
            return value is string ||
                   value is Guid ||
                   value is DateTime ||
                   value is DateTimeOffset ||
                   value is TimeSpan ||
                   value is Enum ||
                   value.GetType().IsPrimitive ||
                   value is decimal;
        }

        private static string Join(
            string prefix,
            string name
            )
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return name;
            }

            return $"{prefix}.{name}";
        }

        private static DataClassification? GetClassification(
            System.Reflection.PropertyInfo propertyInfo
            )
        {
            var attribute = Attribute.GetCustomAttribute(
                propertyInfo,
                typeof(DataClassificationAttribute),
                inherit: true
                ) as DataClassificationAttribute;

            return attribute?.Classification;
        }

        private static TypePlan CreateTypePlan(
            Type type
            )
        {
            var props = type.GetProperties(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public
                );

            var members = new List<MemberPlan>();

            foreach (var prop in props)
            {
                if (prop.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                if (prop.GetMethod is null)
                {
                    continue;
                }

                var getter = CreateGetter(prop);
                if (getter is null)
                {
                    continue;
                }

                members.Add(new MemberPlan(
                    Name: prop.Name,
                    Getter: getter,
                    Classification: GetClassification(prop)
                    ));
            }

            return new TypePlan(
                Members: members
                );
        }

        private static Func<object, object?>? CreateGetter(
            System.Reflection.PropertyInfo propertyInfo
            )
        {
            try
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var cast = Expression.Convert(instance, propertyInfo.DeclaringType!);
                var access = Expression.Property(cast, propertyInfo);
                var box = Expression.Convert(access, typeof(object));
                var lambda = Expression.Lambda<Func<object, object?>>(box, instance);
                return lambda.Compile();
            }
            catch
            {
                return null;
            }
        }

        private sealed record TypePlan(
            IReadOnlyList<MemberPlan> Members
            );

        private sealed record MemberPlan(
            string Name,
            Func<object, object?> Getter,
            DataClassification? Classification
            );

        private static string? Redact(
            object? value,
            IRedactorProvider redactorProvider,
            DataClassification classification
            )
        {
            if (value is null)
            {
                return null;
            }

            var text = value switch
            {
                byte[] bytes => Convert.ToBase64String(bytes),
                _ => value.ToString() ?? string.Empty
            };

            var redactor = redactorProvider.GetRedactor(classification);
            return redactor.Redact(text);
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();

            public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}

