using ButtplugSong.Helper;
using ButtplugSong.Network;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.Network;

internal class FeatureUI : GUISection
{
    protected readonly Toggle featureToggle;
    protected DeviceUI parent;
    protected bool FeatureHidden;

    public FeatureUI(DeviceUI deviceUI, VisualElement localRoot, FeatureType featureType, DeviceFeature feature) : base(featureType.ToString())
    {
        parent = deviceUI;

        featureToggle = localRoot.Q<Toggle>($"{Identifier}Feature");
        FeatureHidden = !feature.IsSupported || !DeviceFeature.Implemented.Contains(featureType);

        if (FeatureHidden)
        {
            feature.IsEnabled = false;
            featureToggle.AddToClassList("hide");
            return;
        }

        Label stepCount = localRoot.Q<Label>($"{Identifier}StepCount");
        if (!feature.StepCount.HasValue) stepCount.AddToClassList("hide");
        else if (feature.StepCount < 1_000) stepCount.text = feature.StepCount.Value.ToString();
        else stepCount.text = $"{feature.StepCount.Value / 1000:f1}K";

        feature.IsEnabled = featureToggle.value;
        featureToggle.DependsOn(parent._enabled).RegisterValueChangedCallback(evt => feature.IsEnabled = evt.newValue);
    }
}
internal class RotateFeatureUI : FeatureUI
{
    protected readonly DropdownField dropdown;
    public RotateFeatureUI(DeviceUI deviceUI, VisualElement localRoot, DeviceFeature feature) : base(deviceUI, localRoot, FeatureType.Rotate, feature)
    {
        dropdown = localRoot.Q<DropdownField>("RotateFeatureDirection");
        if (FeatureHidden)
        {
            dropdown.AddToClassList("hide");
            return;
        }
        dropdown.DependsOn(parent._enabled, featureToggle).RegisterValueChangedCallback(DropdownChanged);
    }
    private void DropdownChanged(ChangeEvent<string> evt)
    {
        parent.DeviceInfo.AlternateRotation = evt.newValue == "Alternate";
        parent.DeviceInfo.RotateClockwise = evt.newValue == "Clockwise";
    }
}
internal class TemperatureFeatureUI : FeatureUI
{
    protected readonly FloatField neutralTemp;
    public TemperatureFeatureUI(DeviceUI deviceUI, VisualElement localRoot, DeviceFeature feature) : base(deviceUI, localRoot, FeatureType.Temperature, feature)
    {
        neutralTemp = localRoot.Q<FloatField>("TemperatureFeatureNeutral");
        if (FeatureHidden)
        {
            neutralTemp.AddToClassList("hide");
            return;
        }
        neutralTemp.DependsOn(parent._enabled, featureToggle).SetupValueClamping(0, 100).RegisterValueChangedCallback(NeutralTempChanged);
    }

    private void NeutralTempChanged(ChangeEvent<float> evt)
    {
        parent.DeviceInfo.NeutralTemperature = evt.newValue / 100;
    }
}
internal class PositionFeatureUI : FeatureUI
{
    protected readonly FloatField moveDuration;
    public PositionFeatureUI(DeviceUI deviceUI, VisualElement localRoot, DeviceFeature feature) : base(deviceUI, localRoot, FeatureType.Position, feature)
    {
        moveDuration = localRoot.Q<FloatField>("PositionFeatureDuration");
        if (FeatureHidden)
        {
            moveDuration.AddToClassList("hide");
            return;
        }
        moveDuration.DependsOn(parent._enabled, featureToggle).SetupValueClamping(0.05f, 60).RegisterValueChangedCallback(PositionMoveDurationChanged);
    }
    private void PositionMoveDurationChanged(ChangeEvent<float> evt)
    {
        parent.DeviceInfo.MoveDuration = evt.newValue;
    }
}