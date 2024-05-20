﻿using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Zooyard.Utils;

/// <summary>
/// Class to initialize types using reflection
/// </summary>
internal class FastProperty
{
    private static readonly ConcurrentDictionary<PropertyInfo, FastProperty> FastPropertyCache = new();

    /// <summary>
    /// Get or create a <see cref="FastProperty"/> instance for getting/setting the given property.
    /// </summary>
    /// <param name="property">The property to obtain a <see cref="FastProperty"/> instance for.</param>
    /// <returns>
    /// A new or already existing and cached <see cref="FastProperty"/> instance.
    /// </returns>
    public static FastProperty GetOrCreate(PropertyInfo property)
    {
        return FastPropertyCache.GetOrAdd(property, p => new FastProperty(p));
    }

    private Func<object, object>? _getDelegate;
    private Action<object, object>? _setDelegate;

    /// <summary>
    /// Constructor for FastPropery
    /// </summary>
    /// <param name="property"></param>
    private FastProperty(PropertyInfo property)
    {
        Property = property;
        InitializeGet();
        InitializeSet();
    }

    private void InitializeSet()
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");

        if (Property.DeclaringType is null)
        {
            throw new ArgumentException("Unable to determine DeclaringType from Property");
        }

        UnaryExpression instanceCast = !Property.DeclaringType?.IsValueType ?? false
            ? Expression.TypeAs(instance, Property.DeclaringType!)
            : Expression.Convert(instance, Property.DeclaringType!);

        UnaryExpression valueCast = !Property.PropertyType.IsValueType
            ? Expression.TypeAs(value, Property.PropertyType)
            : Expression.Convert(value, Property.PropertyType);

        var setter = Property.GetSetMethod(true) ?? Property.DeclaringType?.GetProperty(Property.Name)?.GetSetMethod(true); // when Prop from parent it requires DeclaringType

        if (setter != null)
            _setDelegate = Expression.Lambda<Action<object, object>>(Expression.Call(instanceCast, setter, valueCast), new ParameterExpression[] { instance, value }).Compile();
    }

    private void InitializeGet()
    {
        var instance = Expression.Parameter(typeof(object), "instance");

        if (Property.DeclaringType is null)
        {
            throw new ArgumentException("Unable to determine DeclaringType from Property");
        }

        UnaryExpression instanceCast = !Property.DeclaringType.IsValueType
            ? Expression.TypeAs(instance, Property.DeclaringType)
            : Expression.Convert(instance, Property.DeclaringType);

        var getter = Property.GetGetMethod(true) ?? Property.DeclaringType.GetProperty(Property.Name)?.GetGetMethod(true);

        if (getter != null)
            _getDelegate = Expression.Lambda<Func<object, object>>(Expression.TypeAs(Expression.Call(instanceCast, getter), typeof(object)), instance).Compile();
    }

#pragma warning disable CS1591 // No XML comment required here
    public PropertyInfo Property { get; set; }


    /// <summary>
    /// Returns the object
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    public object? Get(object instance) => instance == default || _getDelegate is null ? default : _getDelegate(instance);

    /// <summary>
    /// Sets the delegate
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="value"></param>
    public void Set(object instance, object? value)
    {
        if (value != default && _setDelegate is not null)
        {
            _setDelegate(instance, value);
        }
    }
}
