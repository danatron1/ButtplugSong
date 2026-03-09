using ButtplugSong.GUI.VibeSettings.Presets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace ButtplugSong.Helper;

public static class ExtHelper
{
    public static async void FireAndForget(this Task t, Action<string> logging)
    {
        try
        {
            await t;
        }
        catch (Exception e)
        {
            logging?.Invoke($"FAF Exception: {e.GetType()}; {e.Message}");
            if (e.InnerException != null) logging?.Invoke($"    Inner: {e.InnerException.GetType()}; {e.InnerException.Message}");
        }
    }

    public static Random rng = new Random();
    public static T ChooseRandom<T>() where T : Enum
    {
        Array values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(rng.Next(values.Length));
    }
    public static T ChooseRandom<T>(this IEnumerable<T> collection) => collection.ElementAt(rng.Next(collection.Count()));
    public static void SetClassListIf<T>(this T field, string classList, Func<T, bool> predicate) where T : VisualElement
    {
        if (predicate(field))
        {
            if (!field.ClassListContains(classList)) field.AddToClassList(classList);
        }
        else if (field.ClassListContains(classList)) field.RemoveFromClassList(classList);
    }
    public static bool OutsideRange<T>(this T value, T lowerBound, T upperBound, out T inRangeValue) where T : IComparable<T>
    {
        if (value.CompareTo(lowerBound) < 0)
        {
            inRangeValue = lowerBound;
            return true;
        }
        if (value.CompareTo(upperBound) > 0)
        {
            inRangeValue = upperBound;
            return true;
        }
        inRangeValue = value;
        return false;
    }

    public static BaseField<T> SetupValueClamping<T>(this BaseField<T> notifier, T lowerBound, T upperBound, bool delay = true) where T : IComparable<T>
    {
        if (delay && notifier is TextInputBaseField<T> text) text.isDelayed = true;
        notifier.RegisterValueChangedCallback((ChangeEvent<T> evt) =>
        {
            if (evt.newValue.OutsideRange(lowerBound, upperBound, out T inRangeValue)) notifier.value = inRangeValue;
        });
        return notifier;
    }
    public static BaseField<T> SetupGreyout<T>(this BaseField<T> notifier, Func<T, bool> predicate) where T : IComparable<T>
    {
        notifier.RegisterValueChangedCallback((ChangeEvent<T> evt) => notifier.SetClassListIf("grey-value", n => predicate(n.value)));
        return notifier;
    }
    public static BaseField<string> SetupSaving(this BaseField<string> element, string? defaultValue = null, string? settingName = null)
    {
        settingName ??= element.name;
        Preset.BindElementToSetting(element, settingName);
        element.RegisterValueChangedCallback(evt => Preset.Custom.SaveSettingChange(settingName, evt.newValue.Replace(" ", "")));
        if (defaultValue != null) PresetDefault.SaveDefaultSetting(settingName, defaultValue.Replace(" ", ""));
        return element;
    }
    public static BaseField<T> SetupSaving<T>(this BaseField<T> element, T? defaultValue = null, string? settingName = null) where T : struct
    {
        settingName ??= element.name;
        Preset.BindElementToSetting(element, settingName);
        element.RegisterValueChangedCallback(evt => Preset.Custom.SaveSettingChange(settingName, evt.newValue));
        if (defaultValue != null) PresetDefault.SaveDefaultSetting(settingName, defaultValue);
        return element;
    }
    public static void Load<T>(this BaseField<T> element, Preset preset) where T : struct, IComparable<T>
    {
        if (Preset.TryGetBoundSetting(element, out string settingName))
        {
            T newValue = preset.Get<T>(settingName) ?? element.value;
            ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(element.value, newValue);
            evt.target = element;
            element.SetValueWithoutNotify(newValue);
            element.SendEvent(evt);
        }
        else throw new ArgumentException($"Saving not setup! Couldn't load setting from preset {preset.Identifier} for element {element.name} ({element.typeName})");
    }
    //for dropdowns, save the default as a STRING. For cyclingButton, save the default as an ENUM (i.e. no .ToString())
    public static void Load(this BaseField<string> element, Preset preset, bool friendlyName = true)
    {
        if (Preset.TryGetBoundSetting(element, out string settingName))
        {
            string newValue = preset.GetString(settingName) ?? element.value;
            if (friendlyName) newValue = newValue.FriendlyName();
            ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(element.value, newValue);
            evt.target = element;
            element.SetValueWithoutNotify(newValue);
            element.SendEvent(evt);
        }
        else throw new ArgumentException($"Saving not setup! Couldn't load setting from preset {preset.Identifier} for element {element.name} ({element.typeName})");
    }
    public static BaseField<T> DependsOn<T>(this BaseField<T> element, Toggle other, Func<Toggle, bool> predicate)
    {
        other.RegisterValueChangedCallback((ChangeEvent<bool> evt) => element.SetEnabled(evt.newValue && predicate(other)));
        return element;
    }
    public static BaseField<TSelf> DependsOn<TSelf, TOther>(this BaseField<TSelf> element, BaseField<TOther> other, Func<BaseField<TOther>, bool> predicate)
    {
        other.RegisterValueChangedCallback((ChangeEvent<TOther> evt) => element.SetEnabled(predicate(other)));
        return element;
    }
    public static BaseField<T> DependsOn<T>(this BaseField<T> element, params Toggle[] others)
    {
        if (others.Length == 0) return element;
        if (others.Length == 1) others[0].RegisterValueChangedCallback(evt => element.SetEnabled(evt.newValue));
        else
        {
            foreach (Toggle other in others)
            {
                other.RegisterValueChangedCallback((ChangeEvent<bool> evt) => element.SetEnabled(evt.newValue && others.All(x => x.value)));
            }
        }
        return element;
    }
    public static string FriendlyName(this string s) //adds spaces where needed
    {
        string result = $"{char.ToUpper(s[0])}";
        bool lastWasUpper = true;
        for (int i = 1; i < s.Length; i++)
        {
            if (char.IsUpper(s[i]))
            {
                if (!char.IsWhiteSpace(s[i - 1]) && !(lastWasUpper && i + 1 < s.Length && !char.IsLower(s[i + 1]))) result += ' ';
                lastWasUpper = true;
            }
            else lastWasUpper = false;
            result += s[i];
        }
        return result;
    }
    public static string[] DisplayFriendlyEnumNames<T>() where T : Enum => [.. Enum.GetNames(typeof(T)).Select(FriendlyName)];
    public static DropdownField PopulateDropdown<T>(this DropdownField dropdown, params string[] additionalSettings) where T : Enum
    {
        return dropdown.PopulateDropdown([.. DisplayFriendlyEnumNames<T>(), .. additionalSettings]);
    }
    public static DropdownField PopulateDropdown(this DropdownField dropdown, params string[] options)
    {
        dropdown.choices = [.. options];
        return dropdown;
    }
    public delegate void DropdownChanged<T>(string newValue, bool isEnum, T type) where T : Enum;
    public static DropdownField RegisterDropdownChangedCallback<T>(this DropdownField dropdown, DropdownChanged<T> callWhenChanged) where T : struct, Enum
    {
        dropdown.RegisterValueChangedCallback((ChangeEvent<string> evt) =>
        {
            if (Enum.TryParse(evt.newValue.Replace(" ", ""), true, out T result)) callWhenChanged(evt.newValue, true, result);
            else callWhenChanged(evt.newValue, false, default);
        });
        return dropdown;
    }
    public static T Next<T>(this T current) where T : Enum
    {
        T[] arr = (T[])Enum.GetValues(typeof(T));
        int i = Array.IndexOf(arr, current) + 1;
        return (arr.Length == i) ? arr[0] : arr[i];
    }
}
