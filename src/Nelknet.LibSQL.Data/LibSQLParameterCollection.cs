using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Nelknet.LibSQL.Data;

/// <summary>
/// Represents a collection of parameters associated with a <see cref="LibSQLCommand"/>.
/// </summary>
public sealed class LibSQLParameterCollection : DbParameterCollection
{
    private readonly List<LibSQLParameter> _parameters = new();

    /// <summary>
    /// Gets the number of parameters in the collection.
    /// </summary>
    public override int Count => _parameters.Count;

    /// <summary>
    /// Gets a value indicating whether the collection has a fixed size.
    /// </summary>
    public override bool IsFixedSize => false;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public override bool IsReadOnly => false;

    /// <summary>
    /// Gets a value indicating whether access to the collection is synchronized.
    /// </summary>
    public override bool IsSynchronized => false;

    /// <summary>
    /// Gets an object that can be used to synchronize access to the collection.
    /// </summary>
    public override object SyncRoot => ((ICollection)_parameters).SyncRoot;

    /// <summary>
    /// Gets or sets the <see cref="LibSQLParameter"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the parameter.</param>
    /// <returns>The <see cref="LibSQLParameter"/> at the specified index.</returns>
    public new LibSQLParameter this[int index]
    {
        get => _parameters[index];
        set
        {
            _parameters[index] = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="LibSQLParameter"/> with the specified name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The <see cref="LibSQLParameter"/> with the specified name.</returns>
    public new LibSQLParameter this[string parameterName]
    {
        get => (LibSQLParameter)GetParameter(parameterName);
        set => SetParameter(parameterName, value);
    }

    /// <summary>
    /// Adds a parameter to the collection.
    /// </summary>
    /// <param name="value">The parameter to add.</param>
    /// <returns>The index of the new parameter.</returns>
    public override int Add(object value)
    {
        if (value is not LibSQLParameter parameter)
            throw new ArgumentException("Value must be a LibSQLParameter.", nameof(value));

        _parameters.Add(parameter);
        return _parameters.Count - 1;
    }

    /// <summary>
    /// Adds a <see cref="LibSQLParameter"/> to the collection.
    /// </summary>
    /// <param name="parameter">The parameter to add.</param>
    /// <returns>The index of the new parameter.</returns>
    public LibSQLParameter Add(LibSQLParameter parameter)
    {
        if (parameter is null)
            throw new ArgumentNullException(nameof(parameter));

        _parameters.Add(parameter);
        return parameter;
    }

    /// <summary>
    /// Adds a parameter with the specified name and value.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns>The newly created parameter.</returns>
    public LibSQLParameter AddWithValue(string parameterName, object? value)
    {
        var parameter = new LibSQLParameter(parameterName, value);
        Add(parameter);
        return parameter;
    }

    /// <summary>
    /// Adds a parameter with the specified name, data type, and value.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="dbType">The data type of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <returns>The newly created parameter.</returns>
    public LibSQLParameter AddWithValue(string parameterName, DbType dbType, object? value)
    {
        var parameter = new LibSQLParameter(parameterName, dbType);
        parameter.Value = value;
        // Explicitly set the DbType again since setting Value might override it
        parameter.DbType = dbType;
        Add(parameter);
        return parameter;
    }

    /// <summary>
    /// Adds a range of LibSQLParameter objects to the collection.
    /// </summary>
    /// <param name="parameters">The parameters to add.</param>
    public void AddRange(IEnumerable<LibSQLParameter> parameters)
    {
        if (parameters is null)
            throw new ArgumentNullException(nameof(parameters));

        foreach (var parameter in parameters)
        {
            Add(parameter);
        }
    }

    /// <summary>
    /// Adds an array of parameters to the collection.
    /// </summary>
    /// <param name="values">The parameters to add.</param>
    public override void AddRange(Array values)
    {
        if (values is null)
            throw new ArgumentNullException(nameof(values));

        foreach (LibSQLParameter parameter in values)
        {
            Add(parameter);
        }
    }

    /// <summary>
    /// Removes all parameters from the collection.
    /// </summary>
    public override void Clear()
    {
        _parameters.Clear();
    }

    /// <summary>
    /// Determines whether the collection contains the specified parameter.
    /// </summary>
    /// <param name="value">The parameter to locate.</param>
    /// <returns>true if the parameter is found; otherwise, false.</returns>
    public override bool Contains(object value)
    {
        return value is LibSQLParameter parameter && _parameters.Contains(parameter);
    }

    /// <summary>
    /// Determines whether the collection contains a parameter with the specified name.
    /// </summary>
    /// <param name="value">The name of the parameter.</param>
    /// <returns>true if a parameter with the specified name is found; otherwise, false.</returns>
    public override bool Contains(string value)
    {
        return IndexOf(value) >= 0;
    }

    /// <summary>
    /// Copies the collection elements to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based index in array at which copying begins.</param>
    public override void CopyTo(Array array, int index)
    {
        ((ICollection)_parameters).CopyTo(array, index);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public override IEnumerator GetEnumerator()
    {
        return _parameters.GetEnumerator();
    }

    /// <summary>
    /// Gets the index of the specified parameter in the collection.
    /// </summary>
    /// <param name="value">The parameter to locate.</param>
    /// <returns>The index of the parameter; -1 if not found.</returns>
    public override int IndexOf(object value)
    {
        return value is LibSQLParameter parameter ? _parameters.IndexOf(parameter) : -1;
    }

    /// <summary>
    /// Gets the index of the parameter with the specified name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The index of the parameter; -1 if not found.</returns>
    public override int IndexOf(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return -1;

        for (int i = 0; i < _parameters.Count; i++)
        {
            if (string.Equals(_parameters[i].ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Validates all parameters in the collection.
    /// </summary>
    public void ValidateParameters()
    {
        var parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var parameter in _parameters)
        {
            parameter.Validate();
            
            if (!parameterNames.Add(parameter.ParameterName))
            {
                throw new InvalidOperationException($"Duplicate parameter name: {parameter.ParameterName}");
            }
        }
    }

    /// <summary>
    /// Inserts a parameter at the specified index.
    /// </summary>
    /// <param name="index">The index at which to insert the parameter.</param>
    /// <param name="value">The parameter to insert.</param>
    public override void Insert(int index, object value)
    {
        if (value is not LibSQLParameter parameter)
            throw new ArgumentException("Value must be a LibSQLParameter.", nameof(value));

        _parameters.Insert(index, parameter);
    }

    /// <summary>
    /// Removes the specified parameter from the collection.
    /// </summary>
    /// <param name="value">The parameter to remove.</param>
    public override void Remove(object value)
    {
        if (value is LibSQLParameter parameter)
        {
            _parameters.Remove(parameter);
        }
    }

    /// <summary>
    /// Removes the parameter at the specified index.
    /// </summary>
    /// <param name="index">The index of the parameter to remove.</param>
    public override void RemoveAt(int index)
    {
        _parameters.RemoveAt(index);
    }

    /// <summary>
    /// Removes the parameter with the specified name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to remove.</param>
    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            RemoveAt(index);
        }
    }

    /// <summary>
    /// Gets the parameter at the specified index.
    /// </summary>
    /// <param name="index">The index of the parameter.</param>
    /// <returns>The parameter at the specified index.</returns>
    protected override DbParameter GetParameter(int index)
    {
        return _parameters[index];
    }

    /// <summary>
    /// Gets the parameter with the specified name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The parameter with the specified name.</returns>
    protected override DbParameter GetParameter(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index < 0)
        {
            throw new ArgumentException($"Parameter '{parameterName}' not found.", nameof(parameterName));
        }
        return _parameters[index];
    }

    /// <summary>
    /// Sets the parameter at the specified index.
    /// </summary>
    /// <param name="index">The index of the parameter.</param>
    /// <param name="value">The parameter to set.</param>
    protected override void SetParameter(int index, DbParameter value)
    {
        _parameters[index] = (LibSQLParameter)value;
    }

    /// <summary>
    /// Sets the parameter with the specified name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The parameter to set.</param>
    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index < 0)
        {
            throw new ArgumentException($"Parameter '{parameterName}' not found.", nameof(parameterName));
        }
        _parameters[index] = (LibSQLParameter)value;
    }

    /// <summary>
    /// Gets a safe index for the parameter name, throwing an exception if not found.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The index of the parameter.</returns>
    private int GetSafeIndex(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index < 0)
        {
            throw new ArgumentException($"Parameter '{parameterName}' not found.", nameof(parameterName));
        }
        return index;
    }
}