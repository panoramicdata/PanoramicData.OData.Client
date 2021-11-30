﻿using System;

namespace PanoramicData.OData.Client;

/// <summary>
/// Specifies expansion levels.
/// </summary>
public enum ODataExpandLevels
{
	/// <summary>
	/// Specifies maximum expansion levels.
	/// </summary>
	Max,
}

/// <summary>
/// Specifies expansion mode (by value or by reference).
/// </summary>
public enum ODataExpandMode
{
	/// <summary>
	/// Associations should be expanded by value.
	/// </summary>
	ByValue,

	/// <summary>
	/// Associations should be expanded by reference.
	/// </summary>
	ByReference,
}

/// <summary>
/// Specifies how to expand entity associations.
/// </summary>
public class ODataExpandOptions : IEquatable<ODataExpandOptions>
{
	/// <summary>
	/// The number of levels to expand.
	/// </summary>
	public int Levels { get; private set; }

	/// <summary>
	/// The expansion mode (by value or by reference).
	/// </summary>
	public ODataExpandMode ExpandMode { get; private set; }

	private ODataExpandOptions(int levels = 1, ODataExpandMode expandMode = ODataExpandMode.ByValue)
	{
		Levels = levels;
		ExpandMode = expandMode;
	}

	private ODataExpandOptions(ODataExpandLevels levels, ODataExpandMode expandMode = ODataExpandMode.ByValue)
		: this(0, expandMode)
	{
	}

	/// <summary>
	/// Expansion by value.
	/// </summary>
	/// <param name="levels">The number of levels to expand.</param>
	public static ODataExpandOptions ByValue(int levels = 1) => new(levels, ODataExpandMode.ByValue);

	/// <summary>
	/// Expansion by value.
	/// </summary>
	/// <param name="levels">The number of levels to expand.</param>
	public static ODataExpandOptions ByValue(ODataExpandLevels levels) => new(levels, ODataExpandMode.ByValue);

	/// <summary>
	/// Expansion by reference.
	/// </summary>
	/// <param name="levels">The number of levels to expand.</param>
	public static ODataExpandOptions ByReference(int levels = 1) => new(levels, ODataExpandMode.ByReference);

	/// <summary>
	/// Expansion by reference.
	/// </summary>
	/// <param name="levels">The number of levels to expand.</param>
	public static ODataExpandOptions ByReference(ODataExpandLevels levels) => new(levels, ODataExpandMode.ByReference);

	public bool Equals(ODataExpandOptions other)
	{
		if (other == null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return ExpandMode == other.ExpandMode && Levels == other.Levels;
	}

	public override int GetHashCode() => (ExpandMode, Levels).GetHashCode();
}
